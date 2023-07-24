using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Inspiring.Contracts;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Inspiring.Json.NSwag {
    /// <summary>
    /// Adds support for System.Text.Json polymorphism to NSwag/NJsonSchema.
    /// </summary>
    public class PolymorphicJsonSchemaProcessor : ISchemaProcessor {
        private readonly JsonSerializerOptions _options;

        /// <summary>
        /// A filter predicate that can be used to ignore certain subtypes.
        /// </summary>
        public Func<Type, bool> DerivedTypeFilter { get; set; } = _ => true;

        /// <param name="options">The .NET <see cref="JsonSerializerOptions"/> used to get the 
        ///   <see cref="JsonTypeInfo"/> that contains the <see cref="JsonPolymorphismOptions"/>
        ///   for a certain type.</param>
        public PolymorphicJsonSchemaProcessor(JsonSerializerOptions options)
            => _options = options;

        public void Process(SchemaProcessorContext context) {
            // We only want to process real schemas and not reference
            if (context.Schema.HasReference)
                return;

            // If we have no other (OpenAPI) base type and have a single contract interface, we add
            // the interface as an OpenAPI base type (AllOf item).
            if (context.Schema.AllOf.Count == 0 &&
                implementsUniqueContractInterface(context.ContextualType.Type, out Type contractInterface)) {

                JsonSchema baseSchema = GetSchema(context, contractInterface);
                context.Schema.AllOf.Add(new JsonSchema { Reference = baseSchema });

                foreach (string baseProperty in getAllProperties(baseSchema)) {
                    context.Schema.Properties.Remove(baseProperty);
                    context.Schema.RequiredProperties.Remove(baseProperty);
                }
            } else {
                addDiscriminatorObject(context);
            }

            void addDiscriminatorObject(SchemaProcessorContext context) {
                JsonTypeInfo info = _options.GetTypeInfo(context.ContextualType.Type);
                if (info.PolymorphismOptions is { TypeDiscriminatorPropertyName: var discriminatorName }) {
                    context.Schema.Properties[discriminatorName] = new JsonSchemaProperty {
                        Type = JsonObjectType.String,
                        IsRequired = true
                    };

                    OpenApiDiscriminator discriminatorObject = new() { PropertyName = discriminatorName };

                    foreach (JsonDerivedType dt in info.PolymorphismOptions.DerivedTypes) {
                        if (dt.TypeDiscriminator != null && DerivedTypeFilter(dt.DerivedType)) {
                            discriminatorObject.Mapping.Add(
                                dt.TypeDiscriminator.ToString(),
                                new JsonSchema { Reference = GetSchema(context, dt.DerivedType) });
                        }
                    }

                    // IMPORTANT: It is crucial that we REPLACE the discriminator object here, because
                    //            NJsonSchema always adds a 'Mapping' element with the class name of the
                    //            derived type as key: When the schema for a derived type is generated,
                    //            it calls 'JsonSchemaGenerator.GenerateInheritanceDiscriminator' for the
                    //            base type which ALWAYS adds the mapping. This leads to an ArgumentException
                    //            if the PolymorphismOptions also return the class name as discriminator
                    //            value or to multiple (incorrect) discriminator values for a single
                    //            derived type.
                    context.Schema.DiscriminatorObject = discriminatorObject;
                }
            }


            // gets the properties of the given schema and all base schemas
            static IEnumerable<string> getAllProperties(JsonSchema schema) {
                return schema.ActualSchema.Properties.Keys.Concat(schema
                    .AllOf
                    .SelectMany(x => getAllProperties(x)));
            }

            static bool implementsUniqueContractInterface(Type t, [MaybeNullWhen(false)] out Type contractInterface) {
                Type[] contractInterfaces = getContractInterfaces(t).ToArray();

                if (contractInterfaces.Length == 1) {
                    contractInterface = contractInterfaces[0];
                    return true;
                }

                // GetInterfaces() also returns all base interfaces of our implemented interfaces. We get
                // the directly implemented interfaces by removes all base interfaces of all implemented
                // interfaces from our list of interfaces.
                HashSet<Type> immediateContractInterfaces = new(contractInterfaces);

                foreach (Type i in contractInterfaces)
                    immediateContractInterfaces.ExceptWith(i.GetInterfaces());

                if (immediateContractInterfaces.Count == 1) {
                    contractInterface = immediateContractInterfaces.Single();
                    return true;
                }

                contractInterface = null!;
                return false;
            }

            static IEnumerable<Type> getContractInterfaces(Type t) => t
                .GetInterfaces()
                .Where(i => i.GetCustomAttribute<ContractAttribute>() != null);
        }

        private static JsonSchema GetSchema(SchemaProcessorContext context, Type t) {
            return context.Resolver.HasSchema(t, false) ?
                context.Resolver.GetSchema(t, false) :
                context.Generator.Generate(t, context.Resolver);
        }
    }
}
