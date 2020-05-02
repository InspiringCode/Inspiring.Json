using System;
using System.Collections.Generic;
using System.Linq;

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
        
        public string GetDiscriminatorValue(Type type) 
            => _subcontracts[type].DiscriminatorValue;

        public Type ResolveType(string discriminatorValue)
            => _subtypes[discriminatorValue];
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
