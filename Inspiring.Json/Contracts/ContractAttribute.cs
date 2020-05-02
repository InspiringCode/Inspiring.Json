using System;

namespace Inspiring.Contracts {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public class ContractAttribute : Attribute {
        public ContractAttribute() { }

        public ContractAttribute(string discriminatorValue)
            => DiscriminatorValue = discriminatorValue;

        public string? DiscriminatorName { get; set; }

        public string? DiscriminatorValue { get; set; }
    }
}
