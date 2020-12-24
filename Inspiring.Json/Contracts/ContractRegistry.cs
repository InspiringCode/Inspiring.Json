using Inspiring.Contracts;
using Inspiring.Contracts.Core;
using Inspiring.Json;
using System;
using System.Collections.Concurrent;

namespace Inspiring.Contracts {
    public class ContractRegistry {
        public static readonly ContractRegistry Default = new ContractRegistry(new DefaultContractFactory<ContractAttribute>());

        private readonly IContractFactory _factory;
        private readonly ConcurrentDictionary<Type, IContract> _cache
            = new ConcurrentDictionary<Type, IContract>();

        public ContractRegistry(IContractFactory factory)
            => _factory = factory;

        public bool IsPolymorphic(Type type, out ContractTypeHierarchy? hierarchy) {
            type = type ?? throw new ArgumentNullException(nameof(type));
            IContract c = _cache.GetOrAdd(type, _factory.CreateContract);
            return c.IsPolymorphic(out hierarchy);
        }

        public ContractTypeHierarchy GetHierarchyInfo(Type type) {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (IsPolymorphic(type, out ContractTypeHierarchy? info)) {
                return info!;
            }

            throw new ContractException(LContracts.GetHierarchyInfo_NoContractType.FormatWith(type.Name));
        }
    }
}
