using System.Text.Json;
using Inspiring.Contracts;
using Inspiring.Json.NSwag;
using NJsonSchema;
using NJsonSchema.Generation;
using Xunit.Abstractions;

namespace Inspiring.Json.Tests;

public class JsonSchemaInterfaceHandlingTests : Feature {
    private readonly ITestOutputHelper _output;

    public JsonSchemaInterfaceHandlingTests(ITestOutputHelper output)
        => _output = output;

    [Scenario]
    internal void Tests(JsonSerializerOptions opts,
            JsonSchemaGenerator gen,
            JsonSchema schema
    ) {
        GIVEN["a configured schema generator"] = () => gen = new JsonSchemaGenerator(
            new JsonSchemaGeneratorSettings {
                SchemaType = SchemaType.OpenApi3,
                SchemaProcessors = {
                    new PolymorphicJsonSchemaProcessor(
                        new JsonSerializerOptions { TypeInfoResolver = new ContractJsonTypeInfoResolver() })
                }
            });

        WHEN["a type has a no base type but implements a single [Contract] interface"] = () => schema = gen.Generate(typeof(Root));
        THEN["the interface is added to the 'allOf' list"] = () => schema
            .Definitions[nameof(Base)].AllOf.Should().ContainSingle()
            .Which.Reference.Should().Be(schema.Definitions[nameof(IBase)]);
        AND["'properties' does no contain the interface properties"] = () => schema
            .Definitions[nameof(Base)].Properties.Keys
            .Should().BeEquivalentTo(nameof(Base.BaseProperty));


        THEN["print the schema"] = () => _output.WriteLine(schema.ToJson());

    }


    public class Root {
        public IBase Content { get; }
    }


    [Contract(DiscriminatorName = DiscriminatorName)]
    public interface IBase {
        public const string DiscriminatorName = "$type";

        string InterfaceProperty { get; }
    }

    [Contract]
    public interface ISub {
        string SubProperty { get; }
    }

    [Contract]
    public class Base : IBase {
        public string InterfaceProperty { get; }

        public string BaseProperty { get; }
    }

    [Contract]
    public class Sub : Base, ISub {
        public string SubProperty { get; }
    }
}
