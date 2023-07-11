using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Inspiring.Contracts;

namespace Inspiring.Json.Tests {
    public class ContractJsonConverterTests : Feature {
        private readonly JsonSerializerOptions _options = new JsonSerializerOptions {
            TypeInfoResolver = new ContractJsonTypeInfoResolver()
        };

        [Scenario]
        internal void SerializationTests(ContainerClass orig, JsonDocument json, ContainerClass result) {
            WHEN["serializing a class with a polymorphic property"] = () => json = SerializeJson(
                orig = new ContainerClass { Value = new Subtype_2_1 { Value1 = "TEST" } });
            THEN["the discriminator is written to the JSON"] = () => json
                .RootElement.GetProperty("Value")
                .GetProperty("Type").GetString()
                .Should().Be("ST-2-1");
            AND["the JSON can be deserialized"] = () => result = DeserializeJson<ContainerClass>(json);
            AND["the result is equivalent to the original"] = () => result.Should().BeEquivalentTo(orig);
        }

        [Scenario]
        internal void JsonTypeInfo(ContractJsonTypeInfoResolver r, JsonPolymorphismOptions p) {
            GIVEN["a resolver"] = () => r = new ContractJsonTypeInfoResolver();

            WHEN["getting the info for a root class"] = () => p = r
                .GetTypeInfo(typeof(IBase), new())
                .PolymorphismOptions;
            THEN["it has the configured discriminator value"] = () => p.TypeDiscriminatorPropertyName.Should().Be(IBase.DiscriminatorName);
            AND["DerivedTypes includes all subconstracts"] = () => p
                .DerivedTypes.Select(x => x.DerivedType)
                .Should().BeEquivalentTo(new[] { typeof(Subtype_1), typeof(Subtype_1_2), typeof(Subtype_2_1) });

            WHEN["getting the info for a subtype"] = () => p = r
                .GetTypeInfo(typeof(Subtype_1), new())
                .PolymorphismOptions;
            THEN["it has the configured discriminator value"] = () => p.TypeDiscriminatorPropertyName.Should().Be(IBase.DiscriminatorName);
            AND["DerivedTypes does not include the type itself"] = () => p.DerivedTypes.Should().NotContain(jt => jt.DerivedType == typeof(Subtype_1));
            AND["DerivedTypes includes all subconstracts"] = () => p
                .DerivedTypes.Select(x => x.DerivedType)
                .Should().BeEquivalentTo(new[] { typeof(Subtype_1_2) });

            WHEN["getting the info for a subtype"] = () => p = r
                .GetTypeInfo(typeof(Subtype_2_1), new())
                .PolymorphismOptions;
            THEN["the PolymorphismOptions are null"] = () => p.Should().BeNull();
        }


        [Scenario]
        internal void NoDiscriminatorValueInJson(Action act) {
            WHEN["deserializing a JSON without discriminator property"] = () =>
                act = new Action(() => DeserializeJson<IBase>("\n" + """{ "Value": "1" }"""));

            // TODO
            //THEN["the exception contains additional data"] = () => ex.Data["DiscriminatorName"].Should().Be("Type");

            THEN["an exception is thrown"] = () => {
                NotSupportedException ex = act.Should().Throw<NotSupportedException>().Which;
                //ex.LineNumber.Should().Be(2 -1);
                //ex.BytePositionInLine.Should().Be(1);
            };
        }

        [Scenario]
        internal void InvalidDiscriminatorValueInJson(Action act) {
            WHEN["deserializing a JSON without an invalid discriminator value"] = () =>
                act = new Action(() => DeserializeJson<IBase>("\n\n" + """{ "Type": "<INVALID>" }"""));
            THEN["an exception is thrown"] = () => act
                    .Should().Throw<JsonException>()
                    .WithMessage("*discriminator*<INVALID>*");
        }

        [Scenario]
        internal void ExceptionData(ArgumentOutOfRangeException ex) {
            WHEN["the object constructor throws an exception"] = () =>
                ex = new Action(() => DeserializeJson<IBase>("""{ "Type": "Subtype_1", "Value": "INVALID" }"""))
                    .Should().Throw<ArgumentOutOfRangeException>()
                    .Which;

            // TODO: Adapt to System.Text.Json. One could probably plug in some custom converter
            //       that catches and enriches the exception.

            //THEN["the exception contains additional data"] = () => {
            //    ex.Data["DiscriminatorName"].Should().Be("Type");
            //    ex.Data["DiscriminatorValue"].Should().Be("Subtype");
            //    ex.Data["TargetType"].Should().Be(typeof(Subtype));
            //};
        }

        [Scenario]
        internal void PositionInfo(Action act) {
            WHEN["the deserialization fails"] = () => act = new Action(() =>
                    DeserializeJson<IBase>("\n\n" + """{ "Type": "Subtype_1", "IntValue": "INVALID" }"""));

            THEN["an exception with the correct position info is thrown"] = () => {
                JsonException ex = act.Should().Throw<JsonException>().Which;
                ex.LineNumber.Should().Be(3 - 1);
                ex.BytePositionInLine.Should().Be(44);
                ex.Message.Should().Match("*IntValue*Line*2*Position*44*");
            };
        }

        [Scenario]
        internal void DiscriminatorAttributeHandling(Subtype_1 result) {
            WHEN["deserializing a polymorphic object"] = () =>
                result = (Subtype_1)DeserializeJson<IBase>("""{ "Type": "Subtype_1" }""");
            THEN["the discriminator attribute is removed before deserializing the concrete object"] = () =>
                result.ExtraProperties.Should().BeEmpty();
        }

        public class ContainerClass {
            public IBase Value { get; set; }
        }

        [Contract(DiscriminatorName = DiscriminatorName)]
        public interface IBase {
            public const string DiscriminatorName = "Type";
        }

        [Contract]
        public class Subtype_1 : IBase {
            public const string DiscriminatorValue = nameof(Subtype_1);

            public string Value { get; }

            public int IntValue { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtraProperties { get; set; } = new();

            public Subtype_1(string value) {
                if (value == "INVALID")
                    throw new ArgumentOutOfRangeException(nameof(value));

                Value = value;
            }
        }

        [Contract]
        public class Subtype_1_2 : Subtype_1 {
            public Subtype_1_2(string value) : base(value) { }
        }

        public class Subtype_2 : IBase {
            public string Value1 { get; set; }
        }

        [Contract(DiscriminatorValue = DiscriminatorValue)]
        public class Subtype_2_1 : Subtype_2 {
            public const string DiscriminatorValue = "ST-2-1";

            public string Value2 { get; set; }
        }

        private JsonDocument SerializeJson(object value) {
            string json = JsonSerializer.Serialize(value, _options);
            return JsonDocument.Parse(json);
        }

        private T DeserializeJson<T>(JsonDocument json)
            => json.Deserialize<T>(_options);

        private T DeserializeJson<T>(string json)
            => JsonSerializer.Deserialize<T>(json, _options);
    }
}
