using Inspiring.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Inspiring.Json {
    /// <summary>
    /// Adds support for [Contract]-types to the System.Text.Json library.
    /// </summary>
    public class ContractJsonTypeInfoResolver : IJsonTypeInfoResolver {
        private readonly ContractRegistry _registry;

        public ContractJsonTypeInfoResolver()
            : this(ContractRegistry.Default) { }

        public ContractJsonTypeInfoResolver(ContractRegistry registry)
            => _registry = registry ?? throw new ArgumentNullException(nameof(registry));

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) {
            JsonTypeInfo info = new DefaultJsonTypeInfoResolver().GetTypeInfo(type, options);

            if (_registry.IsPolymorphic(type, out ContractTypeHierarchy? h)) {
                JsonPolymorphismOptions opts = new();
                IEnumerable<JsonDerivedType> derivedTypes = h
                    .Subcontracts
                    .Where(s => s.Type != type && type.IsAssignableFrom(s.Type))
                    .Select(s => new JsonDerivedType(s.Type, s.DiscriminatorValue));

                bool hasDerivedTypes = false;
                foreach (JsonDerivedType dt in derivedTypes) {
                    hasDerivedTypes = true;
                    opts.DerivedTypes.Add(dt);
                }

                if (hasDerivedTypes) {
                    opts.TypeDiscriminatorPropertyName = h.DiscriminatorName;
                    info.PolymorphismOptions = opts;
                }
            }

            return info;
        }
    }
}
