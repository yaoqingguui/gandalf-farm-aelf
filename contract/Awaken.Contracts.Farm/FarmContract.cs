using System;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Awaken.Contracts.Farm
{
    /// <summary>
    /// The C# implementation of the contract defined in farm_contract.proto that is located in the "protobuf"
    /// folder.
    /// Notice that it inherits from the protobuf generated code. 
    /// </summary>
    public partial class FarmContract : FarmContractContainer.FarmContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.TokenContract.Value == null, "Already initialized.");
            Assert(input.StartBlock > Context.CurrentHeight,"Invalid Input:StartBlock");
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.LpTokenContract.Value = input.LpTokenContract;
            State.Admin.Value = input.Admin ?? Context.Sender;
            State.Owner.Value = Context.Sender;
            State.HalvingPeriod0.Value = input.Block0;
            State.HalvingPeriod1.Value = input.Block1;
            State.DistributeTokenPerBlockConcentratedMining.Value = input.DistributeTokenPerBlock0;
            State.DistributeTokenPerBlockContinuousMining.Value = input.DistributeTokenPerBlock1;
            State.StartBlockOfDistributeToken.Value = input.StartBlock;
            State.Cycle.Value = input.Cycle;
            State.TotalReward.Value = input.TotalReward;
            return new Empty();
        }

        public override Empty SetHalvingPeriod(SetHalvingPeriodInput input)
        {
            Assert(Context.Sender == State.Admin.Value,"Unauthorized");
            State.HalvingPeriod0.Value = input.Block0;
            State.HalvingPeriod1.Value = input.Block1;
            return new Empty();
        }

        public override Empty SetDistributeTokenPerBlock(SetDistributeTokenPerBlockInput input)
        {
            Assert(Context.Sender == State.Admin.Value,"Unauthorized");
            State.DistributeTokenPerBlockConcentratedMining.Value = input.PerBlock0;
            State.DistributeTokenPerBlockContinuousMining.Value = input.PerBlock1;
            return new Empty();
        }

        public override Empty SetOwner(Address input)
        {
            Assert(Context.Sender == State.Owner.Value,"Unauthorized");
            State.Owner.Value = input;
            return new Empty(); 
        }

        public override Empty SetTool(Address input)
        {
            Assert(Context.Sender == State.Owner.Value,"Unauthorized");
            State.Tool.Value = input;
            return new Empty(); 
        }

        public override Empty SetReDeposit(SetReDepositInput input)
        {
            Assert(Context.Sender == State.Admin.Value,"Unauthorized");
            State.Router.Value = input.Router;
            State.FarmTwoPool.Value = input.FarmTwoPool;
            return new Empty(); 
        }

        public override Empty SetAdmin(Address input)
        {
            Assert(Context.Sender == State.Admin.Value,"Unauthorized");
            State.Admin.Value = input;
            return new Empty(); 
        }

        public override Empty AddPool(AddPoolInput input)
        {
            Assert(Context.Sender == State.Admin.Value,"Unauthorized");
            Assert(input.LpToken == null,"Invalid input");
            if (input.WithUpdate) {
                MassUpdatePools(new Empty());
            }

            var lastRewardBlock = Context.CurrentHeight > State.StartBlockOfDistributeToken.Value
                ? Context.CurrentHeight
                : State.StartBlockOfDistributeToken.Value;
            State.TotalAllocPoint.Value = State.TotalAllocPoint.Value.Add(input.AllocPoint);
            var index = State.PoolLength.Value.Add(1);
            State.PoolInfo[index] = new PoolInfo()
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

        public override Empty MassUpdatePools(Empty input)
        {
            MassUpdatePoolsInternal();
            return new Empty(); 
        }

        private void MassUpdatePoolsInternal()
        {
            var length = State.PoolLength.Value;
            for (var i = 0; i < length; i++)
            {
                UpdatePoolInternal(i);
            }
        }

        public override Empty UpdatePool(Int32Value input)
        {
            UpdatePoolInternal(input.Value);
            return new Empty();
        }

        private void UpdatePoolInternal(int pid)
        {
           var pool = State.PoolInfo[pid];
           if (Context.CurrentHeight <= pool.LastRewardBlock)
           {
               return;
           }

           var lpSupply = pool.TotalAmount;
           if (lpSupply == 0)
           {
               State.PoolInfo[pid].LastRewardBlock = Context.CurrentHeight;
               return;
           }

           GetDistributeTokenBlockRewardInternal(pool.LastRewardBlock,out var blockReward,out var blockLockReward);
           if (blockLockReward <= 0 && blockReward <= 0) {
               return;
           }
           var distributeTokenLockReward = blockLockReward.Mul(pool.AllocPoint).Div(
               State.TotalAllocPoint.Value
           );
           var distributeTokenReward = blockReward.Mul(pool.AllocPoint).Div(
               State.TotalAllocPoint.Value
           );
           var totalReward = distributeTokenLockReward.Add(distributeTokenReward);
           State.TokenContract.Issue.Send(new IssueInput()
           {
               Amount = totalReward,
               Symbol = DistributeToken,
               To = Context.Self
           });
           State.IssuedReward.Value = State.IssuedReward.Value.Add(totalReward);
           var lastAccLockDistributeTokenPerShare = GetLastAccLockDistributeTokenPerShare(pid);
          
           if (lastAccLockDistributeTokenPerShare.Equals(pool.LastAccLockDistributeTokenPerShare) ) {
               State.PoolInfo[pid].LastAccLockDistributeTokenPerShare = lastAccLockDistributeTokenPerShare;
           }

           State.PoolInfo[pid].AccLockDistributeTokenPerShare = pool.AccLockDistributeTokenPerShare.Add(
               new BigIntValue(distributeTokenLockReward)
                   .Mul(Multiplier).Div(lpSupply));
           State.PoolInfo[pid].AccDistributeTokenPerShare = pool.AccDistributeTokenPerShare.Add(
               new BigIntValue(distributeTokenReward)
                   .Mul(Multiplier).Div(lpSupply));
           long multiplier = 0;
           if (Context.CurrentHeight > State.UsdtStartBlock.Value)
           {
               long subtractor;
               if (Context.CurrentHeight <=  State.UsdtEndBlock.Value) {
                   subtractor = pool.LastRewardBlock > State.UsdtStartBlock.Value
                       ? pool.LastRewardBlock
                       : State.UsdtStartBlock.Value;
                   multiplier = Context.CurrentHeight.Sub(subtractor);
               } else {
                   if (pool.LastRewardBlock < State.UsdtEndBlock.Value) {
                       subtractor = pool.LastRewardBlock > State.UsdtStartBlock.Value
                           ? pool.LastRewardBlock
                           : State.UsdtStartBlock.Value;
                       multiplier = State.UsdtEndBlock.Value.Sub(subtractor);
                   }
               }
           }
         
           var usdtReward = new BigIntValue(multiplier).Mul(State.UsdtPerBlock.Value)
               .Mul(pool.AllocPoint)
               .Div(State.TotalAllocPoint.Value);
           State.PoolInfo[pid].AccUsdtPerShare = pool.AccUsdtPerShare.Add(
               usdtReward.Mul(multiplier).Div(lpSupply)
           );
      
           State.PoolInfo[pid].LastRewardBlock = Context.CurrentHeight;
           Context.Fire(new UpdatePool()
           {
               Pid = pid,
               DistributeTokenAmount = totalReward,
               UpdateBlockHeight = Context.CurrentHeight,
               UsdtAmount = Convert.ToInt64(usdtReward.ToString())
           });

        }

        public override GetDistributeTokenBlockRewardOutput GetDistributeTokenBlockReward(Int64Value input)
        {
            GetDistributeTokenBlockRewardInternal(input.Value,  out var blockReward,out var blockLockReward);
            return new GetDistributeTokenBlockRewardOutput()
            {
                BlockReward = blockReward,
                BlockLockReward = blockLockReward
            };
        }

           
    }
}