using System;

namespace Inspiring.Contracts {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class ContractAttribute : Attribute {
        private bool _isRootContract = false;

        public ContractAttribute() { }

        public ContractAttribute(string discriminatorValue)
            => DiscriminatorValue = discriminatorValue;

        public string? DiscriminatorName { get; set; }

        public string? DiscriminatorValue { get; set; }

        public bool IsRootContract {
            get => _isRootContract;
            set {
                _isRootContract = value;
                if (string.IsNullOrEmpty(DiscriminatorName))
                    DiscriminatorName = "$type";
            }
        }
    }
}
