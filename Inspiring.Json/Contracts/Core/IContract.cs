namespace Inspiring.Contracts.Core {
    public interface IContract {
        public bool IsPolymorphic(out ContractTypeHierarchy? hierarchy);
    }
}
