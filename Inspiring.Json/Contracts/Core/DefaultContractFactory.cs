using Inspiring.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Inspiring.Contracts.Core {
    public class DefaultContractFactory<TAttribute> : IContractFactory where TAttribute : Attribute {
        private readonly ConcurrentDictionary<Type, ContractTypeHierarchy> _hierarchyCache =
            new ConcurrentDictionary<Type, ContractTypeHierarchy>();

        public IContract CreateContract(Type type) {
            if (type.GetCustomAttribute<TAttribute>(inherit: false) != null) {
                var baseContracts = GetTypeWithBaseTypes(type)
                    .Select(t => (Type: t, DiscriminatorName: t.GetDiscriminatorName<TAttribute>()))
                    .Where(x => !String.IsNullOrWhiteSpace(x.DiscriminatorName))
                    .ToArray();

                if (baseContracts.Length > 1) {
                    throw new ArgumentException(LContracts.CreateContract_DiscriminatorSpecifiedMultipleTimes.FormatWith(type.Name));
                }

                if (baseContracts.Length == 1) {
                    ContractTypeHierarchy hierarchy = _hierarchyCache.GetOrAdd(
                        baseContracts[0].Type,
                        t => CreateHierarchy(t, baseContracts[0].DiscriminatorName!));

                    return new PolymorphicContract(hierarchy);
                }
            }

            return NullContract.Instance;
        }

        protected virtual IEnumerable<Type> GetRelatedTypes(Type type) =>
            new[] { type.Assembly }
                .Union(AppDomain
                    .CurrentDomain
                    .GetAssemblies()
                    .Where(a => a.GetCustomAttribute<ContractAssemblyAttribute>() != null))
                .SelectMany(a => a.GetTypes());

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
