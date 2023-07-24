using System;
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

        WHEN["a class without base class implements a single [Contract] interface"] = () =>
            schema = gen.Generate(typeof(Root<ChangeNameIndirectlyImplementingOneContractInterface>));
        THEN["the interface is added to the 'allOf' list"] = () => schema
            .Definitions[nameof(ChangeNameIndirectlyImplementingOneContractInterface)].AllOf.Should().ContainSingle()
            .Which.Reference.Should().Be(schema.Definitions[nameof(ICommand)]);
        AND["the discriminator object is only generated for the root contract type"] = () => {
            schema.Definitions[nameof(ICommand)].DiscriminatorObject.Should().NotBeNull();
            schema.Definitions[nameof(ChangeNameIndirectlyImplementingOneContractInterface)].DiscriminatorObject.Should().BeNull();
        };
        AND["'properties' does not contain the interface properties"] = () => schema
            .Definitions[nameof(ChangeNameIndirectlyImplementingOneContractInterface)].Properties.Keys
            .Should().BeEquivalentTo(nameof(ChangeName.NewName));

        WHEN["an interface implements a single [Contract] interface"] = () =>
            schema = gen.Generate(typeof(Root<IEmployeeCommand>));
        THEN["the base interface is added to the 'allOf' list"] = () => schema
            .Definitions[nameof(IEmployeeCommand)].AllOf.Should().ContainSingle()
            .Which.Reference.Should().Be(schema.Definitions[nameof(ICommand)]);
        AND["the discriminator object is only generated for the root contract type"] = () => {
            schema.Definitions[nameof(ICommand)].DiscriminatorObject.Should().NotBeNull();
            schema.Definitions[nameof(IEmployeeCommand)].DiscriminatorObject.Should().BeNull();
        };
        AND["'properties' does not contain the interface properties"] = () => schema
            .Definitions[nameof(IEmployeeCommand)].Properties.Keys
            .Should().BeEquivalentTo(nameof(IEmployeeCommand.EmployeeId));
        
        WHEN["a type without base class implements a single [Contract] interface that is not inherited from implemented interfaces"] = () =>
            schema = gen.Generate(typeof(Root<ChangeName>));
        THEN["the interface is added to the 'allOf' list"] = () => schema
            .Definitions[nameof(ChangeName)].AllOf.Should().ContainSingle()
            .Which.Reference.Should().Be(schema.Definitions[nameof(IEmployeeCommand)]);
        AND["the discriminator object is only generated for the root contract type"] = () => {
            schema.Definitions[nameof(ICommand)].DiscriminatorObject.Should().NotBeNull();
            schema.Definitions[nameof(IEmployeeCommand)].DiscriminatorObject.Should().BeNull();
            schema.Definitions[nameof(ChangeName)].DiscriminatorObject.Should().BeNull();
        };
        AND["'properties' does not contain the interface properties"] = () => schema
            .Definitions[nameof(ChangeName)].Properties.Keys
            .Should().BeEquivalentTo(nameof(ChangeName.NewName));

        THEN["print the schema"] = () => _output.WriteLine(schema.ToJson());

    }


    public class Root<T> {
        public T Content { get; }
    }


    [Contract(DiscriminatorName = DiscriminatorName)]
    public interface ICommand {
        public const string DiscriminatorName = "$type";

        string Id { get; }
    }

    [Contract]
    public interface IEmployeeCommand : ICommand {
        string EmployeeId { get; }
    }

    [Contract]
    public class ChangeName : IEmployeeCommand {
        public string Id { get; }

        public string EmployeeId { get; }


        public string NewName { get; }
    }

    public interface IEmployeeCommandWithoutContractAttribute : ICommand { }

    [Contract]
    public class ChangeNameIndirectlyImplementingOneContractInterface : IEmployeeCommandWithoutContractAttribute {
        public string Id { get; }

        public string NewName { get; }
    }
}
