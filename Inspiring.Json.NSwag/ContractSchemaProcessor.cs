using Inspiring.Contracts;
using NJsonSchema;
using NJsonSchema.Generation;
using System;

namespace Inspiring.Json.NSwag {
    public class ContractSchemaProcessor : ISchemaProcessor {
        private readonly ContractRegistry _registry;

        public ContractSchemaProcessor(ContractRegistry registry)
            => _registry = registry ?? throw new ArgumentNullException(nameof(registry));

        public void Process(SchemaProcessorContext context) {
            if (_registry.IsPolymorphic(context.Type, out ContractTypeHierarchy? hierarchy)) {
                if (hierarchy!.BaseContractType == context.Type) {
                    var discriminator = new OpenApiDiscriminator {
                        PropertyName = hierarchy.DiscriminatorName
                    };

                    foreach (SubcontractType subtype in hierarchy.Subcontracts) {
                        discriminator.Mapping.Add(subtype.DiscriminatorValue, ProcessSubtype(subtype.Type, context.Schema));
                    }

                    context.Schema.DiscriminatorObject = discriminator;
                    context.Schema.Properties[hierarchy.DiscriminatorName] = new JsonSchemaProperty {
                        Type = JsonObjectType.String,
                        IsRequired = true
                    };
                }
            }

            JsonSchema ProcessSubtype(Type t, JsonSchema parent) {
                
                JsonSchema s = context.Resolver.HasSchema(t, false) ?
                    context.Resolver.GetSchema(t, false) : 
                    context.Generator.Generate(t, context.Resolver);

                s.AllOf.Add(new JsonSchema { Reference = parent });
                return new JsonSchema { Reference = s };
            }
        }
    }
}
