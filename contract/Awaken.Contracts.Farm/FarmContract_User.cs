using System;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Awaken.Contracts.Swap;
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
            Assert(State.RouterContract.Value != null,"Redeposit setting is not ready");
            var reDepositLimit = GetReDepositLimitInternal(input.Pid, Context.Sender);
            Assert(input.DistributeTokenAmount <= reDepositLimit.Sub(State.RedepositAmount[input.Pid][Context.Sender]), "Insufficient reDepositLimit");
            var elfBalance = State.TokenContract.GetBalance.Call(new GetBalanceInput()
            {
                Owner = Context.Self,
                Symbol = "ELF"
            }).Balance;
            var distributeTokenBalance = State.TokenContract.GetBalance.Call(new GetBalanceInput()
            {
                Owner = Context.Self,
                Symbol = DistributeToken
            }).Balance;
            DistributeTokenTransferIn(
                Context.Sender,
                Context.Self,
                input.DistributeTokenAmount
            );
            State.TokenContract.TransferFrom.Send(new TransferFromInput()
            {
                Amount = input.ElfAmount,
                From = Context.Sender,
                Symbol = "ELF",
                To = Context.Self
            });
            State.TokenContract.Approve.Send(new ApproveInput()
            {
                Amount =  input.DistributeTokenAmount, 
                Spender = State.RouterContract.Value,
                Symbol = DistributeToken
            });
            State.TokenContract.Approve.Send(new ApproveInput()
            {
                Amount =  input.ElfAmount, 
                Spender = State.RouterContract.Value,
                Symbol = "ELF"
            });
            //to do
            //: addLiquidity and send tx to farmPool two
          
             State.RouterContract.AddLiquidity.Send(new AddLiquidityInput()
            {
                SymbolA = DistributeToken,
                SymbolB = "ELF",
                AmountADesired = input.DistributeTokenAmount,
                AmountAMin = 0,
                AmountBDesired = input.ElfAmount,
                AmountBMin = 0,
                Channel = "",
                Deadline = Context.CurrentBlockTime.AddMinutes(1),
                To = Context.Self
            });
             Context.SendInline(Context.Self, nameof(DepositLp), new DepositLpInput
             {
                 ElfBalance = elfBalance,
                 DistributeTokenBalance = distributeTokenBalance,
                 DistributeTokenAmount = input.DistributeTokenAmount,
                 Pid = input.Pid,
                 Sender = Context.Sender
             });
            
            return new Empty();
        }
        
    }
}