using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Awaken.Contracts.Farm
{
    public partial class FarmContract
    {
        public override GetDistributeTokenBlockRewardOutput GetDistributeTokenBlockReward(Int64Value input)
        {
            GetDistributeTokenBlockRewardInternal(input.Value,  out var blockReward,out var blockLockReward);
            return new GetDistributeTokenBlockRewardOutput()
            {
                BlockReward = blockReward,
                BlockLockReward = blockLockReward
            };
        }

        public override Address GetAdmin(Empty input)
        {
            return State.Admin.Value;
        }

        public override Int64Value GetCycle(Empty input)
        {
            return new Int64Value()
            {
                Value = State.Cycle.Value
            };
        }

        public override Address GetOwner(Empty input)
        {
            return State.Owner.Value;
        }

        public override Int64Value GetHalvingPeriod0(Empty input)
        {
            return new Int64Value()
            {
                Value = State.HalvingPeriod0.Value
            };
        }

        public override Int64Value GetHalvingPeriod1(Empty input)
        {
            return new Int64Value()
            {
                Value = State.HalvingPeriod1.Value
            };
        }

        public override Int64Value GetIssuedReward(Empty input)
        {
            return new Int64Value()
            {
                Value = State.IssuedReward.Value
            };
        }

        public override Int64Value GetTotalReward(Empty input)
        {
            return new Int64Value()
            {
                Value = State.TotalReward.Value
            };
        }

        public override Int64Value GetDistributeTokenPerBlockConcentratedMining(Empty input)
        {
             return new Int64Value()
            {
                Value = State.DistributeTokenPerBlockConcentratedMining.Value
            };
        }

        public override Int64Value GetDistributeTokenPerBlockContinuousMining(Empty input)
        {
            return new Int64Value()
            {
                Value = State.DistributeTokenPerBlockContinuousMining.Value
            };
        }

        public override Int64Value GetStartBlockOfDistributeToken(Empty input)
        {
            return new Int64Value()
            {
                Value = State.StartBlockOfDistributeToken.Value
            };
        }

        public override Int64Value GetUsdtStartBlock(Empty input)
        {
            return new Int64Value()
            {
                Value = State.UsdtStartBlock.Value
            };
        }

        public override Int64Value GetUsdtPerBlock(Empty input)
        {
            return new Int64Value()
            {
                Value = State.UsdtPerBlock.Value
            };
        }

        public override Int64Value GetUsdtEndBlock(Empty input)
        {
            return new Int64Value()
            {
                Value = State.UsdtEndBlock.Value
            };
        }

        public override Int64Value GetReDepositLimit(GetReDepositLimitInput input)
        {
            return base.GetReDepositLimit(input);
        }

        public override Int64Value GetEndBlock(Empty input)
        {
            return new Int64Value
            {
                Value = State.EndBlock.Value
            };
        }

        public override PoolInfo GetPoolInfo(Int32Value input)
        {
            return State.PoolInfoMap[input.Value];
        }

        public override UserInfo GetUserInfo(GetUserInfoInput input)
        {
            return State.UserInfoMap[input.Pid][input.User];
        }

        public override PendingOutput Pending(PendingInput input)
        {
            var distributeTokenAmount = PendingDistributeToken(input.Pid, input.User);
            var usdtAmount = PendingUsdt(input.Pid, input.User);
            return new PendingOutput()
            {
                DistributeTokenAmount = distributeTokenAmount,
                UsdtAmount = usdtAmount,
                BlockNumber = Context.CurrentHeight
            };
        }

        public override Int64Value PendingLockDistributeToken(PendingLockDistributeTokenInput input)
        {
            var pool = State.PoolInfoMap[input.Pid];
            var accLockDistributeTokenPerShare = pool.AccLockDistributeTokenPerShare;
            long stillLockReward = 0;
            if (Context.CurrentHeight <= pool.LastRewardBlock)
                return new Int64Value
                {
                    Value = stillLockReward
                };
            if (pool.TotalAmount == 0) {
                stillLockReward = GetUserLockReward(input.Pid, input.User, 0);
            } else {
                GetDistributeTokenBlockRewardInternal(
                    pool.LastRewardBlock, out var blockReward,out var blockLockReward
                );

                blockLockReward = blockLockReward.Mul(pool.AllocPoint).Div(
                    State.TotalAllocPoint.Value
                );

                accLockDistributeTokenPerShare = accLockDistributeTokenPerShare.Add(new BigIntValue(blockLockReward)
                    .Mul(Multiplier).Div(pool.TotalAmount)
                );

                stillLockReward = GetUserLockReward(
                    input.Pid,
                    input.User,
                    accLockDistributeTokenPerShare
                );
            }

            return new Int64Value
            {
                Value = stillLockReward
            };

        }
    }
}