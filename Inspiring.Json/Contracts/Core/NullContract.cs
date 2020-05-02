namespace Inspiring.Contracts.Core {
    public class NullContract : IContract {
        public static readonly NullContract Instance = new NullContract();

        private NullContract() { }

        public bool IsPolymorphic(out ContractTypeHierarchy? hierarchy) {
            hierarchy = null;
            return false;
        }
    }
}
