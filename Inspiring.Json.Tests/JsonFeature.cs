using FluentAssertions;
using Inspiring.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Xbehave;

namespace Inspiring.Json.Tests {
    public class JsonFeature : Feature {
        [Scenario]
        internal void SerializationTests(JsonSerializer s, ContainerClass orig, JObject json, ContainerClass result) {
            GIVEN["a serializer"] = () => s = new JsonSerializer {
                Converters = { new ContractJsonConverter(new ContractRegistry()) }
            };

            WHEN["serialzing a class with a polymorphic property"] = () => json = SerializeJson(
                s, orig = new ContainerClass { Value = new Subtype_1_1 { Value1 = "TEST" } });
            THEN["the discriminator is written to the JSON"] = () => 
                ((string)json.SelectToken("$.Value.Type")).Should().Be("ST-1-1");
            AND["the JSON can be deserialized"] = () => result = DeserializeJson<ContainerClass>(s, json);
            AND["the result is equivalent to the original"] = () => result.Should().BeEquivalentTo(orig);
        }

        public class ContainerClass {
            public IBase Value { get; set; }
        }

        [Contract(DiscriminatorName = "Type")]
        public interface IBase { }

        public class Subtype_1 : IBase {
            public string Value1 { get; set; }
        }

        [Contract(DiscriminatorValue = "ST-1-1")]
        public class Subtype_1_1 : Subtype_1 {
            public string Value2 { get; set; }
        }

        private static JObject SerializeJson(JsonSerializer serializer, object value) {
            using JTokenWriter writer = new JTokenWriter();
            serializer.Serialize(writer, value);
            return (JObject)writer.Token;
        }

        private static T DeserializeJson<T>(JsonSerializer serializer, JObject json)
            => (T)serializer.Deserialize<T>(json.CreateReader());
    }
}
