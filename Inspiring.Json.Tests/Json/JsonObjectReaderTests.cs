using FluentAssertions;
using Inspiring.Contracts;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xbehave;

namespace Inspiring.Json.Tests {
    public class JsonObjectReaderTests : Feature {
        private List<object> Results;

        [Scenario]
        internal void Success() {
            WHEN["reading a valid json"] = () => Read(@"
                { 'Type': 'test-object', 'Value' : '1' } 
                { 'Type': 'test-object', 'Value' : '2' }
            ");

            THEN["all objects are read"] = () => Results.Should().BeEquivalentTo(new TestObject("1"), new TestObject("2"));
        }

        [Scenario]
        internal void Errors() {
            WHEN["the constructor throws an ArgumentException"] = () =>
                Read("\n{ 'Type': 'test-object', 'Value' : 'INVALID' }");
            THEN["an object error is reported"] = () => Results.Should().SatisfyRespectively(e => e
                .Should().BeOfType<ObjectError>()
                .Which.Message.Should().Be("Cannot read 'test-object' because property 'Value' is missing or invalid (Path '', line 2, position 1): 'Value' must not be 'INVALID'."));

            WHEN["the file has completely invalid json syntax"] = () => Read("invalid test {");
            THEN["a document error is reported"] = () => Results.Should().ContainSingle().Which.Should().BeOfType<DocumentError>();

            WHEN["the file has errors between the objects"] = () => Read(@"
                { 'Type': 'test-object', 'Value': '1' } <
                { 'Type': 'test-object', 'Value': '2' }");
            THEN["the valid objects are processed and a document error is reported"] = () => Results
                .Should().SatisfyRespectively(
                    o => o.Should().BeEquivalentTo(new TestObject("1")),
                    o => o.Should().BeOfType<DocumentError>().Which.Message.Should().Contain("line 2"));

            WHEN["the json of an object has syntax errors"] = () => Read(@"
                { 'Type': 'test-object', 'Value': '1' }
                { 'Type': 'test-object' 'Value': '2' }");
            THEN["the valid objects are processed and a document error is reported"] = () => Results
                .Should().SatisfyRespectively(
                    o => o.Should().BeEquivalentTo(new TestObject("1")),
                    o => o.Should().BeOfType<DocumentError>().Which.Message.Should().Contain("line 3"));

            WHEN["the type property value of an object is invalid"] = () => Read(@"
                { 'Type': 'test-object', 'Value': '1' }
                { 'Type': 'InvalidObjectType' }");
            THEN["the valid objects are processed and a document error is reported"] = () => Results
                .Should().SatisfyRespectively(
                    o => o.Should().BeEquivalentTo(new TestObject("1")),
                    o => o.Should().BeOfType<ObjectError>().Which.Message.Should().Contain("line 3").And.Contain("type property"));

            WHEN["the a json property has a wrong datatype"] = () => Read(@"
                { 'Type': 'test-object', 'Value': '1' }
                { 'Type': 'test-object', 'Value': '2', 'GuidValue': 'invalid' }");
            THEN["the valid objects are processed and a document error is reported"] = () => Results
                .Should().SatisfyRespectively(
                    o => o.Should().BeEquivalentTo(new TestObject("1")),
                    o => o.Should().BeOfType<ObjectError>().Which.Message.Should().Contain("line 3, position 77").And.Contain("GuidValue"));
        }

        private void Read(string json) {
            Results = new List<object>();
            using JsonObjectReader<ITestObject> r = JsonObjectReader<ITestObject>.Create(
                json,
                onObjectRead: (obj, json) => {
                    Results.Add(obj);
                    return Task.CompletedTask;
                },
                onObjectError: (message, json, exception) => {
                    Results.Add(new ObjectError(message));
                    return Task.CompletedTask;
                },
                onDocumentError: (message, exception) => {
                    Results.Add(new DocumentError(message));
                    return Task.CompletedTask;
                }
            );
            r.ReadAsync().Wait();
        }

        private class ObjectError {
            public string Message;
            public ObjectError(string message) {
                Message = message;
            }
        }

        private class DocumentError {
            public string Message;
            public DocumentError(string message) {
                Message = message;
            }
        }

        [Contract(DiscriminatorName = "Type")]
        internal interface ITestObject { }

        [Contract("test-object")]
        internal class TestObject : ITestObject {
            public string Value { get; }

            public Guid GuidValue { get; set; }

            public TestObject(string value) {
                if (value == "INVALID")
                    throw new ArgumentOutOfRangeException(nameof(value), "'value' must not be 'INVALID'.");
                Value = value;
            }
        }
    }
}