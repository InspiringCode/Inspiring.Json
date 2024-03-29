using FluentAssertions;
using Inspiring.Contracts;
using Inspiring.Contracts.Core;
using System;
using System.Collections.Generic;
using Xbehave;
using Inspiring;

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
                reg.IsPolymorphic(typeof(IBaseA_1), out _).Should().BeTrue();
                reg.IsPolymorphic(typeof(Subclass_A1), out _).Should().BeTrue();
                reg.IsPolymorphic(typeof(Subclass_A2), out _).Should().BeFalse();
                reg.IsPolymorphic(typeof(Subclass_A2_1), out _).Should().BeTrue();
            };

            AND["IsPolymorphic returns the same hierarchy object for every hierarchy type"] = () => {
                reg.IsPolymorphic(typeof(Subclass_A1), out h);

                reg.IsPolymorphic(typeof(IBaseA), out ContractTypeHierarchy second);
                second.Should().BeSameAs(h);

                reg.IsPolymorphic(typeof(IBaseA_1), out second);
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

            WHEN["getting the discriminator of a type without contract attribute a ContractException is thrown"] = () => {
                reg.IsPolymorphic(typeof(IBaseA), out h);
                new Action(() => h.GetDiscriminatorValue(typeof(Subclass_A2)))
                    .Should().Throw<ContractException>()
                    .WithMessage(String.Format(LContracts.GetDiscriminatorValue_MissingContractAttribute, nameof(Subclass_A2)));
            };

            WHEN["resolving the type of an invalid discriminator a ContractException is thrown"] = () =>
                new Action(() => h.ResolveType("Subclass_B_2"))
                    .Should().Throw<ContractException>()
                    .WithMessage(String.Format(LContracts.ResolveType_NotFound, "Subclass_B_2", nameof(IBaseA)));

            WHEN["reflecting a contract type that has specified the specified the discriminator name multiple times an ArgumentException is thrown"] = () =>
                new Action(() => reg.IsPolymorphic(typeof(IBaseA_2), out _))
                    .Should().Throw<ArgumentException>()
                    .WithMessage(String.Format(LContracts.CreateContract_DiscriminatorSpecifiedMultipleTimes, nameof(IBaseA_2)));

            WHEN["getting the hierarchy of a non contract class THEN a ContractException is thrown"] = () =>
                new Action(() => reg.GetHierarchyInfo(typeof(InvalidContractClass)))
                    .Should().Throw<ContractException>()
                    .WithMessage(String.Format(LContracts.GetHierarchyInfo_NoContractType, nameof(InvalidContractClass)));

            WHEN["the discriminator is name is specified on an abstract class"] = () => reg = new ContractRegistry(
                factory = new TestContractFactory {
                    RelatedTypes = new[] {
                        typeof(BaseClass),
                        typeof(BaseClassSubSubClass) }
                });

            THEN["IsPolymorphic is true for contract types of a hierarchy"] = () => {
                reg.IsPolymorphic(typeof(BaseClass), out _).Should().BeTrue();
                reg.IsPolymorphic(typeof(BaseClassSubSubClass), out _).Should().BeTrue();
            };
        }

        internal class TestContractFactory : DefaultContractFactory<ContractAttribute> {
            public Type[] RelatedTypes { get; set; } = { };

            protected override IEnumerable<Type> GetRelatedTypes(Type type)
                => RelatedTypes;
        }

        [Contract(DiscriminatorName = "Type")]
        private interface IBaseA { }

        [Contract]
        private interface IBaseA_1 : IBaseA { }

        [Contract(DiscriminatorName = "Type")]
        private interface IBaseA_2 : IBaseA { }

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

        [Contract]
        public class InvalidContractClass { }

        [Contract(DiscriminatorName = "Type")]
        private abstract class BaseClass { }

        private abstract class BaseClassSubClass : BaseClass { }

        [Contract("SubSub")]
        private class BaseClassSubSubClass : BaseClassSubClass { }
    }
}
