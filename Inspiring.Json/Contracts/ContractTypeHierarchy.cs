using Inspiring.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Inspiring.Contracts {
    public class ContractTypeHierarchy {
        private readonly Dictionary<Type, SubcontractType> _subcontracts;
        private readonly Dictionary<string, Type> _subtypes;

        public Type BaseContractType { get; }

        public string DiscriminatorName { get; }

        public IReadOnlyCollection<SubcontractType> Subcontracts => _subcontracts.Values;

        public ContractTypeHierarchy(Type baseContractType, string discriminatorName, IReadOnlyCollection<SubcontractType> subcontracts) {
            BaseContractType = baseContractType ?? throw new ArgumentNullException(nameof(baseContractType));
            DiscriminatorName = discriminatorName ?? throw new ArgumentNullException(nameof(discriminatorName));
            subcontracts = subcontracts ?? throw new ArgumentNullException(nameof(subcontracts));

            _subcontracts = subcontracts.ToDictionary(x => x.Type);
            _subtypes = _subcontracts.ToDictionary(x => x.Value.DiscriminatorValue, x => x.Key);
        }

        public string GetDiscriminatorValue(Type type) {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (_subcontracts.TryGetValue(type, out SubcontractType contract)) {
                return contract.DiscriminatorValue;
            }

            if (type.GetCustomAttribute<ContractAttribute>(inherit: false) == null) {
                throw new ContractException(Localized.GetDiscriminatorValue_MissingContractAttribute.FormatWith(type.Name));
            }

            if (type.Assembly.GetCustomAttribute<ContractAssemblyAttribute>() == null) {
                throw new ContractException(
                    Localized.GetDiscriminatorValue_MissingContractAssemblyAttribute.FormatWith(
                        type.Name,
                        type.Assembly.GetName().Name));
            }

            throw new ContractException(Localized.GetDiscriminatorValue_InvalidContractType.FormatWith(type.Name));
        }

        public Type ResolveType(string discriminatorValue) {
            if (discriminatorValue == null)
                throw new ArgumentNullException(nameof(discriminatorValue));

            if (_subtypes.TryGetValue(discriminatorValue, out Type type)) {
                return type;
            }

            throw new ContractException(Localized.ResolveType_NotFound.FormatWith(discriminatorValue, BaseContractType.Name));
        }
    }

    public class SubcontractType {
        public Type Type { get; }

        public string DiscriminatorValue { get; }

        public SubcontractType(Type type, string discriminatorValue) {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            DiscriminatorValue = discriminatorValue ?? throw new ArgumentNullException(nameof(discriminatorValue));
        }
    }
}
