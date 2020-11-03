using FluentAssertions;
using Inspiring.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using Xbehave;

namespace Inspiring.Json.Tests {
    public class JsonFeature : Feature {
        private JsonSerializer Serializer;

        [Background]
        public void Background() {
            GIVEN["a serializer"] = () => Serializer = new JsonSerializer {
                Converters = { ContractJsonConverter.Default }
            };
        }

        [Scenario]
        internal void SerializationTests(ContainerClass orig, JObject json, ContainerClass result) {
            WHEN["serialzing a class with a polymorphic property"] = () => json = SerializeJson(
                orig = new ContainerClass { Value = new Subtype_1_1 { Value1 = "TEST" } });
            THEN["the discriminator is written to the JSON"] = () =>
                ((string)json.SelectToken("$.Value.Type")).Should().Be("ST-1-1");
            AND["the JSON can be deserialized"] = () => result = DeserializeJson<ContainerClass>(json);
            AND["the result is equivalent to the original"] = () => result.Should().BeEquivalentTo(orig);
        }


        [Scenario]
        internal void NoDiscriminatorValueInJson(JsonSerializationException ex) {
            WHEN["deserializing a JSON without discriminator property"] = () =>
                ex = new Action(() => DeserializeJson<IBase>("\n{ 'Value': '1' }"))
                    .Should().Throw<JsonSerializationException>()
                    .WithMessage(Localized.Deserialize_MissingDiscriminatorProperty.FormatWith(nameof(IBase), "Type") + "*")
                    .Which;
            
            THEN["the exception contains additional data"] = () => ex.Data["DiscriminatorName"].Should().Be("Type");
            AND["it contains position information"] = () => {
                ex.LineNumber.Should().Be(2);
                ex.LinePosition.Should().Be(1);
                ex.Message.Should().Contain("line 2, position 1");
            };
        }

        [Scenario]
        internal void InvalidDiscriminatorValueInJson(JsonSerializationException ex) {
            WHEN["deserializing a JSON without an invalid discrminator value"] = () =>
                ex = new Action(() => DeserializeJson<IBase>("\n\n{ 'Type': '<INVALID>' }"))
                    .Should().Throw<JsonSerializationException>()
                    .WithMessage(Localized.Deserialize_InvalidDiscriminatorValue.FormatWith(nameof(IBase), "<INVALID>", "Type") + "*")
                    .Which;
            THEN["the exception contains additional data"] = () => {
                ex.Data["DiscriminatorName"].Should().Be("Type");
                ex.Data["DiscriminatorValue"].Should().Be("<INVALID>");
            };
            AND["it contains position information"] = () => {
                ex.LineNumber.Should().Be(3);
                ex.LinePosition.Should().Be(1);
                ex.Message.Should().Contain("line 3, position 1");
            };
        }

        [Scenario]
        internal void ExceptionData(ArgumentOutOfRangeException ex) {
            WHEN["the object constructor throws an exception"] = () =>
                ex = new Action(() => DeserializeJson<IBase>("\n\n{ 'Type': 'Subtype', 'Value': 'INVALID' }"))
                    .Should().Throw<ArgumentOutOfRangeException>()
                    .Which;
            THEN["the exception contains additional data"] = () => {
                ex.Data["DiscriminatorName"].Should().Be("Type");
                ex.Data["DiscriminatorValue"].Should().Be("Subtype");
                ex.Data["TargetType"].Should().Be(typeof(Subtype));
            };
        }

        [Scenario]
        internal void PositionInfo(JsonReaderException ex) {
            WHEN["the deserialization fails"] = () => ex = new Action(() => DeserializeJson<IBase>("\n\n{ 'Type': 'Subtype', 'IntValue': 'INVALID' }"))
                    .Should().Throw<JsonReaderException>()
                    .Which;

            THEN["the exception has the correct position info set"] = () => {
                ex.LineNumber.Should().Be(3);
                ex.LinePosition.Should().Be(42);
                ex.Message.Should().Contain("Path 'IntValue', line 3, position 42");
            };
        }

        public class ContainerClass {
            public IBase Value { get; set; }
        }

        [Contract(DiscriminatorName = "Type")]
        public interface IBase { }

        [Contract]
        public class Subtype : IBase {
            public string Value { get; }

            public int IntValue { get; set; }

            public Subtype(string value) {
                if (value == "INVALID")
                    throw new ArgumentOutOfRangeException(nameof(value));

                Value = value;
            }
        }

        public class Subtype_1 : IBase {
            public string Value1 { get; set; }
        }

        [Contract(DiscriminatorValue = "ST-1-1")]
        public class Subtype_1_1 : Subtype_1 {
            public string Value2 { get; set; }
        }

        private JObject SerializeJson(object value) {
            using JTokenWriter writer = new JTokenWriter();
            Serializer.Serialize(writer, value);
            return (JObject)writer.Token;
        }

        private T DeserializeJson<T>(JObject json)
            => (T)Serializer.Deserialize<T>(json.CreateReader());

        private T DeserializeJson<T>(string json)
            => (T)Serializer.Deserialize<T>(new JsonTextReader(new StringReader(json)));
    }
}
