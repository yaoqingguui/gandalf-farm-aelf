using System;
using Gandalf.Contracts.PoolTwoContract;
using Google.Protobuf.WellKnownTypes;

namespace Gandalf.Contracts.PoolTwoContract
{
    public class PoolTwoContract: PoolTwoContractContainer.PoolTwoContractBase
    {
        public override Empty Deposit(DepositInput input)
        {
            return new Empty();
        }
    }
}