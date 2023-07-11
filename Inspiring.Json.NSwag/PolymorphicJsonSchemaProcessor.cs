using System;
using System.Linq;
using System.Reflection;
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

            // If we have no other (OpenAPI) base type and have a single contract interface, we add
            // the interface as an OpenAPI base type (AllOf item).
            if (context.Schema.AllOf.Count == 0) {
                Type[] contractInterfaces = context
                    .ContextualType.Type
                    .GetInterfaces()
                    .Where(i => i.GetCustomAttribute<ContractAttribute>() != null)
                    .ToArray();

                if (contractInterfaces.Length == 1) {
                    JsonSchema baseSchema = GetSchema(context, contractInterfaces[0]);
                    context.Schema.AllOf.Add(new JsonSchema { Reference = baseSchema });

                    foreach (string baseProperty in baseSchema.Properties.Keys) {
                        context.Schema.Properties.Remove(baseProperty);
                        context.Schema.RequiredProperties.Remove(baseProperty);
                    }
                }
            }
        }

        private static JsonSchema GetSchema(SchemaProcessorContext context, Type t) {
            return context.Resolver.HasSchema(t, false) ?
                context.Resolver.GetSchema(t, false) :
                context.Generator.Generate(t, context.Resolver);
        }
    }
}
