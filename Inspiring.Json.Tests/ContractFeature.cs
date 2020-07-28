using FluentAssertions;
using Inspiring.Contracts;
using Inspiring.Contracts.Core;
using System;
using System.Collections.Generic;
using Xbehave;

namespace Inspiring.Json.Tests {
    public class ContractFeature : Feature {
        [Scenario()]
        internal void ContractTests(ContractRegistry reg, TestContractFactory factory, ContractTypeHierarchy h) {
            WHEN["reflecting a contract type"] = () => reg = new ContractRegistry(
                factory = new TestContractFactory {
                    RelatedTypes = new[] {
                    typeof(IBaseA),
                    typeof(Subclass_A1),
                    typeof(Subclass_A2),
                    typeof(Subclass_A2_1) }
                });

            THEN["IsPolymorphic is true only for contract types of a hierarchy"] = () => {
                reg.IsPolymorphic(typeof(IBaseA), out _).Should().BeTrue();
                reg.IsPolymorphic(typeof(Subclass_A1), out _).Should().BeTrue();
                reg.IsPolymorphic(typeof(Subclass_A2), out _).Should().BeFalse();
                reg.IsPolymorphic(typeof(Subclass_A2_1), out _).Should().BeTrue();
            };

            AND["IsPolymorphic returns the same hierarchy object for every hierarchy type"] = () => {
                reg.IsPolymorphic(typeof(Subclass_A1), out h);

                reg.IsPolymorphic(typeof(IBaseA), out ContractTypeHierarchy second);
                second.Should().BeSameAs(h);

                reg.IsPolymorphic(typeof(Subclass_A2_1), out second);
                second.Should().BeSameAs(h);
            };

            AND["it has the correct base contract type"] = () => h.BaseContractType.Should().Be<IBaseA>();
            AND["the subcontracts include only types with contract attribute"] = () =>
                h.Subcontracts.Should().BeEquivalentTo(
                    new SubcontractType(typeof(Subclass_A1), "Subclass-A1"),
                    new SubcontractType(typeof(Subclass_A2_1), "Subclass-A2-1"));
            AND["the correct discriminator values are returned"] = () => {
                h.GetDiscriminatorValue(typeof(Subclass_A1)).Should().Be("Subclass-A1");
                h.GetDiscriminatorValue(typeof(Subclass_A2_1)).Should().Be("Subclass-A2-1");
            };
            AND["subcontract types can be resolved by discriminator value"] = () => {
                h.ResolveType("Subclass-A1").Should().Be<Subclass_A1>();
                h.ResolveType("Subclass-A2-1").Should().Be<Subclass_A2_1>();
            };

            WHEN["the same discriminator value is used in two hierarchies"] = () =>
                factory.RelatedTypes = typeof(BaseB).Assembly.GetTypes();
            THEN["a subcontract type can still be resolved by discriminator value"] = () => {
                reg.IsPolymorphic(typeof(BaseB), out h).Should().BeTrue();
                h.ResolveType("Subclass-A1").Should().Be<Subclass_B>();
            };

            WHEN["the base class has also a discriminator value"] = null;
            THEN["the subcontract types include the base type"] = () => h.Subcontracts
                .Should().ContainEquivalentOf(new SubcontractType(typeof(BaseB), "Base-B"));

            WHEN["not specifying a discriminator value"] = null;
            THEN["the class name is used by default"] = () => h.GetDiscriminatorValue(typeof(Subclass_B_2)).Should().Be(nameof(Subclass_B_2));
            AND["it can be resolve by its class name"] = () => h.ResolveType(nameof(Subclass_B_2));
        }

        internal class TestContractFactory : DefaultContractFactory<ContractAttribute> {
            public Type[] RelatedTypes { get; set; } = { };

            protected override IEnumerable<Type> GetRelatedTypes(Type type)
                => RelatedTypes;
        }

        [Contract(DiscriminatorName = "Type")]
        private interface IBaseA { }

        [Contract("Subclass-A1")]
        private class Subclass_A1 : IBaseA { }

        private class Subclass_A2 : IBaseA { }

        [Contract("Subclass-A2-1")]
        private class Subclass_A2_1 : Subclass_A2 { }

        [Contract(DiscriminatorName = "Type", DiscriminatorValue = "Base-B")]
        private class BaseB { }

        [Contract("Subclass-A1")]
        private class Subclass_B : BaseB { }

        [Contract]
        private class Subclass_B_2 : BaseB { }
    }
}
