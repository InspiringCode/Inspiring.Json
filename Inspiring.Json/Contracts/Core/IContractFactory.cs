using System;

namespace Inspiring.Contracts.Core {
    public interface IContractFactory {
        IContract CreateContract(Type type);
    }
}
