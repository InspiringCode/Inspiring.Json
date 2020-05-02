using Inspiring.Contracts;
using Inspiring.Contracts.Core;
using System;
using System.Collections.Concurrent;

namespace Inspiring.Contracts {
    public class ContractRegistry {
        private readonly IContractFactory _factory;
        private readonly ConcurrentDictionary<Type, IContract> _cache
            = new ConcurrentDictionary<Type, IContract>();

        public ContractRegistry()
            : this(new DefaultContractFactory<ContractAttribute>()) { }

        public ContractRegistry(IContractFactory factory)
            => _factory = factory;

        public bool IsPolymorphic(Type type, out ContractTypeHierarchy? hierarchy) {
            type = type ?? throw new ArgumentNullException(nameof(type));
            IContract c = _cache.GetOrAdd(type, _factory.CreateContract);
            return c.IsPolymorphic(out hierarchy);
        }
    }
}
