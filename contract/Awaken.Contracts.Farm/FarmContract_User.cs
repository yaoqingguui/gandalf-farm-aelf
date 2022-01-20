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

        public override Empty ReDeposit(ReDepositInput input)
        {
            var reDepositLimit = GetReDepositLimitInternal(input.Pid, Context.Sender);
            Assert(input.Amount <= reDepositLimit.Sub(State.RedepositAmount[input.Pid][Context.Sender]), "Insufficient reDepositLimit");
            DistributeTokenTransferIn(
                Context.Sender,
                Context.Self,
                input.Amount
            );
            State.TokenContract.Approve.Send(new ApproveInput()
            {
                Amount =  input.Amount,
                Spender = State.Router.Value,
                Symbol = DistributeToken
            });
            //to do: addLiquidity and send tx to farmPool two
            return new Empty();
        }

        public override Empty FixEndBlock(BoolValue input)
        {
            return base.FixEndBlock(input);
        }
    }
}