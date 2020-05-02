using System;
using System.Collections.Generic;
using System.Text;

namespace Inspiring.Contracts.Core {
    public class PolymorphicContract : IContract {
        private readonly ContractTypeHierarchy _hierarchy;

        public PolymorphicContract(ContractTypeHierarchy hierarchy)
            => _hierarchy = hierarchy ?? throw new ArgumentNullException(nameof(hierarchy));

        public bool IsPolymorphic(out ContractTypeHierarchy? hierarchy) {
            hierarchy = _hierarchy;
            return true;
        }
    }
}
