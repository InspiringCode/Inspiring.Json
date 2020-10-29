using Inspiring.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

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

            JObject json = serializer.Deserialize<JObject>(reader)!;

            string? discriminator = json
                .GetValue(hierarchy!.DiscriminatorName)?
                .Value<string>();

            Type? subtype = null;

            if (String.IsNullOrEmpty(discriminator)) {
                throw addContext(
                    new JsonSerializationException(
                        Localized.Deserialize_MissingDiscriminatorProperty.FormatWith(
                            objectType.Name, 
                            hierarchy!.DiscriminatorName)));
            }
            
            try {
                subtype = hierarchy.ResolveType(discriminator!);
            } catch (ContractException ex) {
               throw addContext(
                    new JsonSerializationException(
                        Localized.Deserialize_InvalidDiscriminatorValue.FormatWith(
                            objectType.Name,
                            discriminator,
                            hierarchy!.DiscriminatorName), ex));
            }

            try {
                return serializer.Deserialize(json.CreateReader(), subtype)!;
            } catch (Exception ex) {
                addContext(ex);
                throw;
            }

            T addContext<T>(T ex) where T : Exception {
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
    }
}
