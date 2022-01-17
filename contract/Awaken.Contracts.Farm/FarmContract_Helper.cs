using System;
using AElf.CSharp.Core;
using AElf.Types;

namespace Awaken.Contracts.Farm
{
    public partial class FarmContract
    {
        private long Phase(long blockNumber)
        {
            var halvingPeriod = State.HalvingPeriod0.Value.Add(State.HalvingPeriod1.Value);
            if (halvingPeriod == 0)
            {
                return 0;
            }

            if (blockNumber > State.StartBlockOfDistributeToken.Value)
            {
                return blockNumber.Sub(State.StartBlockOfDistributeToken.Value).Sub(1)
                    .Div(halvingPeriod);
            }

            return 0;
        }

        private void GetDistributeTokenBlockRewardInternal(long lastRewardBlock, out long blockReward, out long blockLockReward)
        {
            blockReward = 0;
            blockLockReward = 0;
            var halvingPeriod = State.HalvingPeriod0.Value.Add(State.HalvingPeriod1.Value);
            var rewardBlock = Context.CurrentHeight > State.EndBlock.Value
                ? State.EndBlock.Value
                : Context.CurrentHeight;
            if (rewardBlock <= lastRewardBlock) return;
            var n = Phase(lastRewardBlock);
            var m = Phase(rewardBlock);
            while (n < m)
            {
                n++;
                var r = n.Mul(halvingPeriod).Add(State.StartBlockOfDistributeToken.Value);
                var switchBlock = (n - 1)
                    .Mul(halvingPeriod)
                    .Add(State.StartBlockOfDistributeToken.Value)
                    .Add(State.HalvingPeriod0.Value);
                if (switchBlock > lastRewardBlock)
                {
            
                    blockLockReward = blockLockReward.Add(
                        switchBlock.Sub(lastRewardBlock).Mul(
                            State.DistributeTokenPerBlockConcentratedMining.Value.Div(2 << Convert.ToInt32(n.Sub(1)))
                        )
                    );
            
                    blockReward = blockReward.Add(
                        r.Sub(switchBlock).Mul(
                            State.DistributeTokenPerBlockContinuousMining.Value.Div(2 << Convert.ToInt32(n.Sub(1)))
                        )
                    );
                }
                else
                {
                    blockReward = blockReward.Add(
                        r.Sub(lastRewardBlock).Mul(
                            State.DistributeTokenPerBlockContinuousMining.Value.Div(2 << Convert.ToInt32(n.Sub(1)))
                        )
                    );
                }
            
                lastRewardBlock = r;
            }
            
            var switchBlockNext = m.Mul(halvingPeriod).Add(State.StartBlockOfDistributeToken.Value).Add(
                State.HalvingPeriod0.Value
            );
            
            if (switchBlockNext >= rewardBlock)
            {
                blockLockReward = blockLockReward.Add(
                    (rewardBlock.Sub(lastRewardBlock)).Mul(
                        State.DistributeTokenPerBlockConcentratedMining.Value.Div(2 << Convert.ToInt32(m))
                    )
                );
            }
            else
            {
                if (switchBlockNext > lastRewardBlock)
                {
                    blockLockReward = blockLockReward.Add(
                        (switchBlockNext.Sub(lastRewardBlock)).Mul(
                            State.DistributeTokenPerBlockConcentratedMining.Value.Div(2 << Convert.ToInt32(m))
                        )
                    );
                    blockReward = blockReward.Add(
                        rewardBlock.Sub(switchBlockNext).Mul(
                            State.DistributeTokenPerBlockContinuousMining.Value.Div(2 << Convert.ToInt32(m))
                        )
                    );
                }
                else
                {
                    blockReward = blockReward.Add(
                        rewardBlock.Sub(lastRewardBlock).Mul(
                            State.DistributeTokenPerBlockContinuousMining.Value.Div(2 << Convert.ToInt32(m))
                        )
                    );
                }
            }
        }

        private BigIntValue GetLastAccLockDistributeTokenPerShare(int pid)
        {
            var pool = State.PoolInfo[pid];
            var lastRewardBlock = pool.LastRewardBlock;
            var rewardBlock = Context.CurrentHeight > State.EndBlock.Value ? State.EndBlock.Value : Context.CurrentHeight;
            var m = Phase(rewardBlock);
            var n = Phase(lastRewardBlock);
            var lastAccLockDistributeTokenPerShare = pool.LastAccLockDistributeTokenPerShare;
            if (m > n) {
                var accLockDistributeTokenPerShare = pool.AccLockDistributeTokenPerShare;
                var halvingPeriod = State.HalvingPeriod0.Value.Add(State.HalvingPeriod1.Value);
                long blockLockReward = 0;
                while (m > n) {
                    n++;
                    var r = n.Mul(halvingPeriod).Add(State.StartBlockOfDistributeToken.Value);

                    var switchBlock = (n - 1)
                        .Mul(halvingPeriod)
                        .Add(State.StartBlockOfDistributeToken.Value)
                        .Add(State.HalvingPeriod0.Value);
                    if (switchBlock > lastRewardBlock) {
                        blockLockReward = blockLockReward.Add(
                            switchBlock.Sub(lastRewardBlock).Mul(
                                State.DistributeTokenPerBlockContinuousMining.Value.Div(2 << Convert.ToInt32(n.Sub(1)))
                            )
                        );
                    }
                    lastRewardBlock = r;
                }
                blockLockReward = blockLockReward.Mul(pool.AllocPoint).Div(
                    State.TotalAllocPoint.Value
                );
                var lpSupply = pool.TotalAmount;
                if (lpSupply == 0) {
                    return 0;
                }
                lastAccLockDistributeTokenPerShare = new BigIntValue(accLockDistributeTokenPerShare).Add(
                    new BigIntValue(blockLockReward).Mul(Multiplier).Div(lpSupply)
                );
              
            }
            return lastAccLockDistributeTokenPerShare;
        }


    }
}