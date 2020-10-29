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
        internal void NoDiscriminatorValueInJson() {
            WHEN["deserializing a JSON without discriminator property"] = () => 
                new Action(() => DeserializeJson<IBase>("{ 'Value': '1' }"))
                    .Should().Throw<JsonSerializationException>()
                    .WithMessage(Localized.Deserialize_MissingDiscriminatorProperty.FormatWith(nameof(IBase), "Type"))
                    .Which.Data["DiscriminatorName"].Should().Be("Type");
        }

        [Scenario]
        internal void InvalidDiscriminatorValueInJson(JsonSerializationException ex) {
            WHEN["deserializing a JSON without an invalid discrminator value"] = () =>
                ex = new Action(() => DeserializeJson<IBase>("{ 'Type': '<INVALID>' }"))
                    .Should().Throw<JsonSerializationException>()
                    .WithMessage(Localized.Deserialize_InvalidDiscriminatorValue.FormatWith(nameof(IBase), "<INVALID>", "Type"))
                    .Which;
            THEN["the exception contains additional data"] = () => {
                ex.Data["DiscriminatorName"].Should().Be("Type");
                ex.Data["DiscriminatorValue"].Should().Be("<INVALID>");
            };
        }

        [Scenario]
        internal void ExceptionData(ArgumentOutOfRangeException ex) {
            WHEN["the object constructor throws an exception"] = () =>
                ex = new Action(() => DeserializeJson<IBase>("{ 'Type': 'Subtype', 'Value': 'INVALID' }"))
                    .Should().Throw<ArgumentOutOfRangeException>()
                    .Which;
            THEN["the exception contains additional data"] = () => {
                ex.Data["DiscriminatorName"].Should().Be("Type");
                ex.Data["DiscriminatorValue"].Should().Be("Subtype");
                ex.Data["TargetType"].Should().Be(typeof(Subtype));
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
