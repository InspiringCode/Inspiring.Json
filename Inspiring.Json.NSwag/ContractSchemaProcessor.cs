using Inspiring.Contracts;
using NJsonSchema;
using NJsonSchema.Generation;
using System;

namespace Inspiring.Json.NSwag {
    public class ContractSchemaProcessor : ISchemaProcessor {
        public static readonly ContractSchemaProcessor Default = new ContractSchemaProcessor(ContractRegistry.Default);

        private readonly ContractRegistry _registry;

        public Func<Type, bool> SubcontractFilter { get; set; } = _ => true;

        public ContractSchemaProcessor(ContractRegistry registry)
            => _registry = registry ?? throw new ArgumentNullException(nameof(registry));

        public void Process(SchemaProcessorContext context) {
            if (_registry.IsPolymorphic(context.Type, out ContractTypeHierarchy? hierarchy)) {
                if (hierarchy!.BaseContractType == context.Type) {
                    context.Schema.DiscriminatorObject = new OpenApiDiscriminator {
                        PropertyName = hierarchy.DiscriminatorName
                    };

                    context.Schema.Properties[hierarchy.DiscriminatorName] = new JsonSchemaProperty {
                        Type = JsonObjectType.String,
                        IsRequired = true
                    };

                    foreach (SubcontractType subtype in hierarchy.Subcontracts)
                        ProcessContractSubtype(context, subtype);
                }
            }
        }

        protected virtual void ProcessContractSubtype(SchemaProcessorContext context,  SubcontractType subtype) {
            if (SubcontractFilter(subtype.Type)) {
                context
                    .Schema
                    .DiscriminatorObject
                    .Mapping
                    .Add(subtype.DiscriminatorValue, createSubtypeSchema(subtype.Type, context.Schema));

                JsonSchema createSubtypeSchema(Type t, JsonSchema parent) {
                    JsonSchema s = context.Resolver.HasSchema(t, false) ?
                        context.Resolver.GetSchema(t, false) :
                        context.Generator.Generate(t, context.Resolver);

                    s.AllOf.Add(new JsonSchema { Reference = parent });
                    return new JsonSchema { Reference = s };
                }
            }
        }
    }
}
