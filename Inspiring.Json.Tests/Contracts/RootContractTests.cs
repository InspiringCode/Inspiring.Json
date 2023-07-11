using System;
using Inspiring.Contracts;
using Inspiring.Contracts.Core;
using Xunit;

namespace Inspiring.Json.Tests.Contracts;

public class RootContractTests : Feature {
    [Scenario(DisplayName = "Root Contract", Skip = "Implicit root determination is probably not a good idea (various types of the same hierarchy might return different discriminator names")]
    [InlineData(typeof(HasSingleContractInterface), typeof(IHasContract), "$type")]
    [InlineData(typeof(HasSingleRootInterface), typeof(IHasRootContract), "$type")]
    [InlineData(typeof(HasSingleDiscriminatorNameInterface), typeof(IHasDiscriminatorName), IHasDiscriminatorName.DiscriminatorName)]
    [InlineData(typeof(BaseClassIsRootContract), typeof(Sub2WithRoot), Sub2WithRoot.DiscriminatorName)]
    [InlineData(typeof(BaseClassHasContract), typeof(Base), "$type")]
    internal void GetRootContractType(Type type, Type expectedRootType, string expectedDiscriminator) {
        Type rootType = null;
        string discriminator = null;

        WHEN[$"getting the root contract type for {type.Name}"] = () => TestContractFactory.GetRootContract(type, out rootType, out discriminator);
        THEN[$"its root type is {expectedRootType.Name}"] = () => rootType.Should().Be(expectedRootType);
        AND[$"its discriminator should be {expectedDiscriminator}"] = () => discriminator.Should().Be(expectedDiscriminator);
    }

    [Contract]
    internal class HasSingleContractInterface : IHasContract { }

    [Contract]
    internal class HasSingleRootInterface : IHasContract, IHasRootContract { }

    [Contract]
    internal class HasRootAndDiscriminatorNameInterface : IHasRootContract, IHasDiscriminatorName { }

    [Contract]
    internal class HasSingleDiscriminatorNameInterface : IHasContract, IHasDiscriminatorName { }


    [Contract]
    internal interface IHasContract { }

    [Contract(IsRootContract = true)]
    internal interface IHasRootContract { }

    [Contract(IsRootContract = true)]
    internal interface IHasRootContract2 { }

    [Contract(DiscriminatorName = DiscriminatorName)]
    internal interface IHasDiscriminatorName {
        public const string DiscriminatorName = "disc-name";
    }


    [Contract]
    internal class Base { }

    [Contract]
    internal class Sub1 : Base { }


    [Contract(IsRootContract = true, DiscriminatorName = DiscriminatorName)]
    internal class Sub2WithRoot : Sub1 {
        public const string DiscriminatorName = "disc-name";
    }

    [Contract]
    internal class BaseClassIsRootContract : Sub2WithRoot { }

    [Contract]
    internal class BaseClassHasContract : Sub1 { }


    private class TestContractFactory : DefaultContractFactory<ContractAttribute> {

        public static bool GetRootContract(Type t, out Type rootType, out string discriminatorName)
            => new TestContractFactory().GetRootContractType(t, out rootType, out discriminatorName);
    }
}
