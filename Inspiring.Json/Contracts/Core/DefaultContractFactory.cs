using System;
using System.Reflection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Inspiring.Contracts.Core {
    public class DefaultContractFactory<TAttribute> : IContractFactory where TAttribute : Attribute {
        private readonly ConcurrentDictionary<Type, ContractTypeHierarchy> _hierarchyCache =
            new ConcurrentDictionary<Type, ContractTypeHierarchy>();

        public IContract CreateContract(Type type) {
            if (type.GetCustomAttribute<TAttribute>(inherit: false) != null) {
                var baseInfo = GetTypeWithBaseTypes(type)
                    .Select(t => (Type: t, DiscriminatorName: t.GetDiscriminatorName<TAttribute>()))
                    .SingleOrDefault(x => !String.IsNullOrWhiteSpace(x.DiscriminatorName));

                if (baseInfo.DiscriminatorName != null) {
                    ContractTypeHierarchy hierarchy = _hierarchyCache.GetOrAdd(
                        baseInfo.Type,
                        t => CreateHierarchy(t, baseInfo.DiscriminatorName));

                    return new PolymorphicContract(hierarchy);
                }
            }

            return NullContract.Instance;
        }

        protected virtual IEnumerable<Type> GetRelatedTypes(Type type)
            => type.Assembly.GetTypes();

        private ContractTypeHierarchy CreateHierarchy(Type baseContractType, string discriminatorName) {
            SubcontractType[] subcontracts = GetRelatedTypes(baseContractType)
                .Where(t => baseContractType.IsAssignableFrom(t))
                .Select(t => (Subtype: t, DiscriminatorValue: t.GetDiscriminatorValue<TAttribute>()))
                .Where(x => !String.IsNullOrWhiteSpace(x.DiscriminatorValue))
                .Select(x => new SubcontractType(x.Subtype, x.DiscriminatorValue!))
                .ToArray();

            return new ContractTypeHierarchy(
                baseContractType, 
                discriminatorName,
                subcontracts);
        }

        private static IEnumerable<Type> GetTypeWithBaseTypes(Type t) {
            for (Type b = t; b != null; b = b.BaseType) {
                yield return b;
            }

            foreach (Type i in t.GetInterfaces()) {
                yield return i;
            }
        }
    }
}
