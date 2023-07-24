using System.Text.Json;
using Inspiring.Contracts;
using Inspiring.Json.NSwag;
using NJsonSchema;
using NJsonSchema.Annotations;
using NJsonSchema.Generation;
using Xunit.Abstractions;

namespace Inspiring.Json.Tests {
    public class JsonSchemaFeature : Feature {
        private readonly ITestOutputHelper _output;

        public JsonSchemaFeature(ITestOutputHelper output)
            => _output = output;

        [Scenario]
        internal void NJsonSchemaTests(
            JsonSerializerOptions opts,
            JsonSchemaGenerator gen,
            JsonSchema schema,
            JsonSchema baseSchema
        ) {
            GIVEN["JsonSerializerOptions configured for contracts"] = () => opts = new JsonSerializerOptions() {
                TypeInfoResolver = new ContractJsonTypeInfoResolver()
            };

            GIVEN["a schema generator"] = () => gen = new JsonSchemaGenerator(
                new JsonSchemaGeneratorSettings {
                    SchemaType = SchemaType.OpenApi3,
                    SchemaProcessors = { new PolymorphicJsonSchemaProcessor(opts) }
                }
            );

            WHEN["generating a schema"] = () => schema = gen.Generate(typeof(RootClass));
            THEN["a schema for the base interface with the expected properties is generated"] = () => {
                baseSchema = schema.Definitions.Should().ContainKey(nameof(IBase)).WhoseValue;
                baseSchema.Properties.Keys.Should().BeEquivalentTo(IBase.DiscriminatorName, nameof(IBase.InterfaceProperty));
                baseSchema.Properties[IBase.DiscriminatorName].IsRequired.Should().BeTrue();
            };
            AND["schemas for all subcontracts are generated"] = () => schema
                .Definitions.Keys
                .Should().BeEquivalentTo(nameof(IBase), nameof(Subclass_1_1), nameof(Subclass_2), nameof(Subclass_2_1), nameof(Subclass_2_2));
            AND["the base interface schema has mappings for all derived types"] = () => {
                baseSchema.DiscriminatorObject.PropertyName.Should().Be(IBase.DiscriminatorName);
                baseSchema.DiscriminatorObject.Mapping.Keys.Should().BeEquivalentTo(
                    Subclass_1_1.DiscriminatorValue,
                    Subclass_2.DiscriminatorValue,
                    Subclass_2_1.DiscriminatorValue,
                    Subclass_2_2.DiscriminatorValue);
            };
            AND["derived types that inherit from a base type do not have discriminator objects"] = () => {
                schema.Definitions[nameof(Subclass_2)].DiscriminatorObject.Should().BeNull();
                //OpenApiDiscriminator sub2Disc = schema.Definitions[nameof(Subclass_2)].DiscriminatorObject;
                //sub2Disc.PropertyName.Should().Be(IBase.DiscriminatorName);
                //sub2Disc.Mapping.Keys.Should().BeEquivalentTo(
                //    Subclass_2_1.DiscriminatorValue,
                //    Subclass_2_2.DiscriminatorValue);

            };
            AND["leaf classes have no discriminator objects"] = () => schema
                .Definitions[nameof(Subclass_2_1)].DiscriminatorObject
                .Should().BeNull();
            AND["the allOf attribute reflects the inheritance hierarchy"] = () => {
                schema.Definitions[nameof(IBase)].AllOf.Should().BeEmpty();
                schema.Definitions[nameof(Subclass_1_1)].AllOf.Should().HaveCount(1, because: "it has no base schema and implements a single contract interface");
                schema.Definitions[nameof(Subclass_2)].AllOf.Should().HaveCount(1, because: "it has no base schema and implements a single contract interface");
                schema.Definitions[nameof(Subclass_2_1)].AllOf.Should().HaveCount(2);
                schema.Definitions[nameof(Subclass_2_2)].AllOf.Should().HaveCount(2);
            };

            // For debugging only
            THEN["print the schema"] = () => _output.WriteLine(schema.ToJson());
        }

        public class RootClass {
            public IBase Content { get; set; }
        }

        [Contract(DiscriminatorName = DiscriminatorName)]
        public interface IBase {
            public const string DiscriminatorName = "_type";

            public string InterfaceProperty { get; set; }
        }

        public class Subclass_1 : IBase {
            public string InterfaceProperty { get; set; }
            public string SubProperty1 { get; set; }
        }

        [Contract(DiscriminatorValue)]
        [JsonSchemaFlatten]
        public class Subclass_1_1 : Subclass_1 {
            public const string DiscriminatorValue = "Subclass-1-1";

            public string SubSubProperty { get; set; }
        }

        [Contract(DiscriminatorValue)]
        public abstract class Subclass_2 : IBase {
            public const string DiscriminatorValue = "Subclass-2";

            public string InterfaceProperty { get; set; }

            public string SubProperty2 { get; set; }
        }

        [Contract(DiscriminatorValue)]
        public class Subclass_2_1 : Subclass_2 {
            public new const string DiscriminatorValue = "Subclass-2-1";

            public string SubProperty21 { get; set; }
        }

        [Contract(DiscriminatorValue)]
        public class Subclass_2_2 : Subclass_2 {
            public new const string DiscriminatorValue = "Subclass-2-2";

            public string SubProperty22 { get; set; }
        }
    }
}
