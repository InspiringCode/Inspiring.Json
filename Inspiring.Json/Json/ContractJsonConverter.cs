using Inspiring.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text.RegularExpressions;

namespace Inspiring.Json {
    public class ContractJsonConverter : JsonConverter {
        public static readonly ContractJsonConverter Default = new ContractJsonConverter();

        [ThreadStatic] private static bool _isReading;
        [ThreadStatic] private static bool _isWriting;
        private readonly ContractRegistry _contracts;

        public ContractJsonConverter() : this(ContractRegistry.Default) { }

        public ContractJsonConverter(ContractRegistry contracts)
            => _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));

        public override bool CanWrite {
            get {
                if (_isWriting) {
                    _isWriting = false;
                    return false;
                }
                return true;
            }
        }

        public override bool CanRead {
            get {
                if (_isReading) {
                    _isReading = false;
                    return false;
                }
                return true;
            }
        }

        public override bool CanConvert(Type objectType)
            => _contracts.IsPolymorphic(objectType, out _);

        public sealed override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
            try {
                _isWriting = true;
                WriteJsonCore(writer, value, serializer);
            } finally {
                _isWriting = false;
            }
        }

        public sealed override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
            try {
                _isReading = true;
                return ReadJsonCore(reader, objectType, existingValue, serializer);
            } finally {
                _isReading = false;
            }
        }

        private object ReadJsonCore(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
            if (reader.TokenType == JsonToken.Null) {
                return null!;
            }

            if (!_contracts.IsPolymorphic(objectType, out ContractTypeHierarchy? hierarchy))
                throw new InvalidOperationException("ReadJson must only be called for types for which CanConvert returns true.");

            PositionInfo position = new PositionInfo(reader);

            // IMPORTANT: To get correct LineNumber/LinePosition infos in thrown exceptions we have to use JObject.Load 
            // here and NOT serializer.Deserialize<JObject> which does not set any positions! It shouldn't be a problem
            // that we don't use the JsonSerializerSettings here because we are just buffering the JSON 1-to-1. These
            // settings are considered when we actually deserialize the concrete type at the end of this method.
            JObject json = JObject.Load(reader);

            string? discriminator = json
                .GetValue(hierarchy!.DiscriminatorName)?
                .Value<string>();

            Type? subtype = null;

            if (String.IsNullOrEmpty(discriminator)) {
                throw addContext(
                    position.CreateSerializationException(
                        LJson.Converter_MissingDiscriminatorProperty.FormatWith(
                            objectType.Name,
                            hierarchy!.DiscriminatorName
                        )));
            }

            try {
                subtype = hierarchy.ResolveType(discriminator!);
            } catch (ContractException ex) {
                throw addContext(
                    position.CreateSerializationException(
                        LJson.Converter_InvalidDiscriminatorValue.FormatWith(
                            objectType.Name,
                            discriminator,
                            hierarchy!.DiscriminatorName),
                        ex));
            }

            // We remove the discriminator attribute before doing the deserialization of the actual
            // concrete class for the following reasons:
            //   1. If somebody uses the JsonExtensionDataAttribute, we propbably don't want the descriminator
            //      property to be including in this list of extra attributes.
            //   2. More importantly, if somebody uses the MissingMemberHandling.Error flag, Json.NET would
            //      always throw an exception because the discriminator attribute is usually not mapped to
            //      any .NET member.
            json.Remove(hierarchy!.DiscriminatorName);

            try {
                return serializer.Deserialize(json.CreateReader(), subtype)!;
            } catch (Exception ex) {
                addContext(ex);
                throw;
            }

            Exception addContext(Exception ex) {
                ex.Data["DiscriminatorName"] = hierarchy!.DiscriminatorName;
                if (discriminator != null)
                    ex.Data["DiscriminatorValue"] = discriminator;
                if (subtype != null)
                    ex.Data["TargetType"] = subtype;
                return ex;
            }
        }

        private void WriteJsonCore(JsonWriter writer, object? value, JsonSerializer serializer) {
            if (value != null) {
                if (!_contracts.IsPolymorphic(value.GetType(), out ContractTypeHierarchy? hierarchy)) {
                    throw new ArgumentException();
                }

                string discriminatorValue = hierarchy!.GetDiscriminatorValue(value.GetType());
                var json = JObject.FromObject(value, serializer);
                json.AddFirst(new JProperty(hierarchy!.DiscriminatorName, discriminatorValue));
                writer.WriteToken(json.CreateReader());
            } else {
                writer.WriteNull();
            }
        }

        private class PositionInfo {
            public readonly bool HasInfo;
            private readonly string? Path;
            private readonly int Line;
            private readonly int Position;

            public PositionInfo(JsonReader reader) {
                Path = reader.Path;
                if (reader is IJsonLineInfo info && info.HasLineInfo()) {
                    Line = info.LineNumber;
                    Position = info.LinePosition;
                    HasInfo = true;
                }
            }

            public JsonSerializationException CreateSerializationException(string message, Exception? inner = null) =>
                HasInfo ?
                    new JsonSerializationException(AddInfoTo(message), Path!, Line, Position, inner) :
                    new JsonSerializationException(message, inner!);

            public string AddInfoTo(string message) =>
                HasInfo ?
                    $"{message} Path '{Path}', line {Line}, position {Position}." :
                    message;
        }
    }
}
