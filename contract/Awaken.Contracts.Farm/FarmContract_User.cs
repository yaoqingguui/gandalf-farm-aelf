using System;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
namespace Awaken.Contracts.Farm
{
    public partial class FarmContract
    {
        public override Empty MassUpdatePools(Empty input)
        {
            MassUpdatePoolsInternal();
            return new Empty(); 
        }
        
        public override Empty UpdatePool(Int32Value input)
        {
            UpdatePoolInternal(input.Value);
            return new Empty();
        }

        public override Empty Deposit(DepositInput input)
        {
            DepositInternal(input.Pid, input.Amount, Context.Sender);
            return new Empty();
        }

        public override Empty Withdraw(WithdrawInput input)
        {
            WithdrawInternal(input.Pid, input.Amount, Context.Sender);
            return new Empty();
        }
    }
}