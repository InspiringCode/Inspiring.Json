using Inspiring.Contracts;
using Inspiring.Json.NSwag;
using NJsonSchema;
using NJsonSchema.Generation;
using System;
using System.Collections.Generic;
using System.Text;
using Xbehave;
using Xunit.Abstractions;

namespace Inspiring.Json.Tests {
    public class JsonSchemaFeature : Feature {
        private readonly ITestOutputHelper _output;

        public JsonSchemaFeature(ITestOutputHelper output) 
            => _output = output;

        [Scenario]
        internal void NJsonSchemaTests(JsonSchemaGenerator gen, JsonSchema schema) {
            GIVEN["a schema generator"] = () => gen = new JsonSchemaGenerator(
                new JsonSchemaGeneratorSettings {
                    SchemaType = SchemaType.OpenApi3,
                    SchemaProcessors = { new ContractSchemaProcessor(new ContractRegistry()) },
                    FlattenInheritanceHierarchy = true
                }
            );

            WHEN["generating a schema"] = () => schema = gen.Generate(typeof(RootClass));

            // TODO: Write real tests!
            THEN["the schema is correct"] = () => _output.WriteLine(schema.ToJson());
        }

        public class RootClass {
            public IBase Content { get; set; }
        }

        [Contract(DiscriminatorName = "_type")]
        public interface IBase {
            public string BaseProperty { get; set; }
        }

        public class Subclass_1 : IBase {
            public string BaseProperty { get; set; }
            public string SubProperty { get; set; }
        }

        [Contract("Subclass-1-1")]
        public class Subclass_1_1 : Subclass_1 {
            public string SubSubProperty { get; set; }
        }
    }
}
