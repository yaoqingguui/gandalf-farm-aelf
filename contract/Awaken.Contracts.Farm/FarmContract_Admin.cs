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
        private void AssertSenderIsAdmin()
        {
            Assert(State.Admin.Value != null, "Contract not initialized.");
            Assert(Context.Sender == State.Admin.Value, "No permission.");
        }
        private void AssertSenderIsOwner()
        {
            Assert(State.Owner.Value != null, "Contract not initialized.");
            Assert(Context.Sender == State.Owner.Value, "No permission.");
        }
        private void AssertSenderIsTool()
        {
            Assert(State.Tool.Value != null, "Tool not set.");
            Assert(Context.Sender == State.Tool.Value, "No permission.");
        }
        public override Empty SetHalvingPeriod(SetHalvingPeriodInput input)
        {
            AssertSenderIsAdmin();
            State.HalvingPeriod0.Value = input.Block0;
            State.HalvingPeriod1.Value = input.Block1;
            return new Empty();
        }

        public override Empty SetDistributeTokenPerBlock(SetDistributeTokenPerBlockInput input)
        {
            AssertSenderIsAdmin();
            State.DistributeTokenPerBlockConcentratedMining.Value = input.PerBlock0;
            State.DistributeTokenPerBlockContinuousMining.Value = input.PerBlock1;
            return new Empty();
        }

        public override Empty SetOwner(Address input)
        {
            AssertSenderIsOwner();
            State.Owner.Value = input;
            return new Empty(); 
        }

        public override Empty SetTool(Address input)
        {
            AssertSenderIsOwner();
            State.Tool.Value = input;
            return new Empty(); 
        }

        public override Empty SetReDeposit(SetReDepositInput input)
        {
            AssertSenderIsAdmin();
            State.RouterContract.Value = input.Router;
            State.FarmTwoPoolContract.Value = input.FarmTwoPool;
            return new Empty(); 
        }

        public override Empty SetAdmin(Address input)
        {
            AssertSenderIsAdmin();
            State.Admin.Value = input;
            return new Empty(); 
        }

        public override Empty AddPool(AddPoolInput input)
        {
            AssertSenderIsAdmin();
            Assert(input.LpToken != "","Invalid input");
            if (input.WithUpdate) {
                MassUpdatePools(new Empty());
            }

            var lastRewardBlock = Context.CurrentHeight > State.StartBlockOfDistributeToken.Value
                ? Context.CurrentHeight
                : State.StartBlockOfDistributeToken.Value;
            State.TotalAllocPoint.Value = State.TotalAllocPoint.Value.Add(input.AllocPoint);
            var index = State.PoolLength.Value;
            State.PoolInfoMap[index] = new PoolInfo()
            {
                LpToken = input.LpToken,
                AllocPoint = input.AllocPoint,
                LastRewardBlock = lastRewardBlock,
                AccDistributeTokenPerShare = 0,
                AccLockDistributeTokenPerShare = 0,
                AccUsdtPerShare = 0,
                TotalAmount = 0,
                LastAccLockDistributeTokenPerShare = 0
            };
            State.PoolLength.Value = State.PoolLength.Value.Add(1);
            Context.Fire(new PoolAdded()
            {
                Pid = index,
                Token = input.LpToken,
                AllocationPoint = input.AllocPoint,
                LastRewardBlockHeight = lastRewardBlock,
                PoolType = 0
            });
            return new Empty(); 
        }

        public override Empty NewReward(NewRewardInput input)
        {
            AssertSenderIsTool();
            Assert(Context.CurrentHeight > State.UsdtEndBlock.Value && input.StartBlock >= State.UsdtEndBlock.Value,"Not finished");
            Assert(input.StartBlock > State.StartBlockOfDistributeToken.Value,"Dividend should follow the distributeToken distribute");
            Assert(input.StartBlock > Context.CurrentHeight,"Invalid startBlock");
            MassUpdatePoolsInternal();
            UsdtTransferIn(Context.Sender, Context.Self, input.UsdtAmount);
            Assert(State.Cycle.Value.Mul(input.NewPerBlock) <= input.UsdtAmount,"Error input");
            State.UsdtPerBlock.Value = input.NewPerBlock;
            State.UsdtStartBlock.Value = input.StartBlock;
            State.UsdtEndBlock.Value = input.StartBlock.Add(State.Cycle.Value);
            Context.Fire(new  NewRewardSet()
            {
                StartBlock = input.StartBlock,
                EndBlock = State.UsdtEndBlock.Value,
                UsdtPerBlock = input.NewPerBlock
            });
            return new Empty();
        }

        public override Empty FixEndBlock(BoolValue input)
        {
            AssertSenderIsOwner();
            FixEndBlockInternal(input.Value);
            return new Empty();
        }
        
        public override Empty DepositLp(DepositLpInput input)
        {
            Assert(Context.Self == Context.Sender,"Invalid");
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            var lpBalance = State.LpTokenContract.GetBalance.Call(new Token.GetBalanceInput()
            {
                Owner = Context.Self,
                Symbol = GetTokenPairSymbol(DistributeToken, "ELF")
            }).Amount;
            var elfBalance = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = Context.Self
            }).Balance;
            var distributeTokenBalanceAfter = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Symbol = DistributeToken,
                Owner = Context.Self
            }).Balance;
            if (distributeTokenBalanceAfter > input.DistributeTokenBalance)
            {
                DistributeTokenTransfer(input.Sender,distributeTokenBalanceAfter.Sub(input.DistributeTokenBalance));
            }

            if (elfBalance > input.ElfBalance)
            {
                State.TokenContract.Transfer.Send(new TransferInput()
                {
                    Amount = elfBalance.Sub(input.ElfBalance),
                    Symbol = "ELF",
                    To = input.Sender
                });
            }
            
            State.LpTokenContract.Approve.Send(new Token.ApproveInput()
            {
                Amount = lpBalance,
                Symbol = GetTokenPairSymbol(DistributeToken, "ELF"),
                Spender = State.FarmTwoPoolContract.Value
            });
            State.FarmTwoPoolContract.Deposit.Send(new Gandalf.Contracts.PoolTwoContract.DepositInput()
            {
                Amount = new BigIntValue(lpBalance),
                Pid = 0
            });
            State.RedepositAmount[input.Pid][input.Sender] =
                input.DistributeTokenAmount.Add(input.DistributeTokenBalance).Sub(distributeTokenBalanceAfter);
            return new Empty();
        }

        public override Empty SetWeight(SetWeightInput input)
        {
            AssertSenderIsAdmin();
            if (input.WithUpdate) {
                MassUpdatePools(new Empty());
            }
            State.TotalAllocPoint.Value = State.TotalAllocPoint.Value.Sub(State.PoolInfoMap[input.Pid].AllocPoint).Add(input.AllocPoint);
            State.PoolInfoMap[input.Pid].AllocPoint = input.AllocPoint;
            Context.Fire(new WeightSet()
            {
                Pid = input.Pid,
                NewAllocationPoint = input.AllocPoint
            });
            return new Empty();
        }
    }
}