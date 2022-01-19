using System;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Awaken.Contracts.Token;
using Google.Protobuf.WellKnownTypes;
using IssueInput = AElf.Contracts.MultiToken.IssueInput;
using TransferInput = AElf.Contracts.MultiToken.TransferInput;

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
            var pool = State.PoolInfoMap[pid];
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
         
         
        private void MassUpdatePoolsInternal()
        {
            var length = State.PoolLength.Value;
            for (var i = 0; i < length; i++)
            {
                UpdatePoolInternal(i);
            }
        }
        
        private void UpdatePoolInternal(int pid)
        {
           var pool = State.PoolInfoMap[pid];
           if (Context.CurrentHeight <= pool.LastRewardBlock)
           {
               return;
           }

           var lpSupply = pool.TotalAmount;
           if (lpSupply == 0)
           {
               State.PoolInfoMap[pid].LastRewardBlock = Context.CurrentHeight;
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
               State.PoolInfoMap[pid].LastAccLockDistributeTokenPerShare = lastAccLockDistributeTokenPerShare;
           }

           State.PoolInfoMap[pid].AccLockDistributeTokenPerShare = pool.AccLockDistributeTokenPerShare.Add(
               new BigIntValue(distributeTokenLockReward)
                   .Mul(Multiplier).Div(lpSupply));
           State.PoolInfoMap[pid].AccDistributeTokenPerShare = pool.AccDistributeTokenPerShare.Add(
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
           State.PoolInfoMap[pid].AccUsdtPerShare = pool.AccUsdtPerShare.Add(
               usdtReward.Mul(multiplier).Div(lpSupply)
           );
      
           State.PoolInfoMap[pid].LastRewardBlock = Context.CurrentHeight;
           Context.Fire(new UpdatePool()
           {
               Pid = pid,
               DistributeTokenAmount = totalReward,
               UpdateBlockHeight = Context.CurrentHeight,
               UsdtAmount = Convert.ToInt64(usdtReward.ToString())
           });
        }

        private void DepositInternal(int pid, long amount, Address user)
        {
            var pool = State.PoolInfoMap[pid];
            var userInfo = State.UserInfoMap[pid][user];
            UpdatePoolInternal(pid);
            long stillLockReward = 0;
            if (userInfo.Amount >= 0)
            {
                var userReward = pool.AccDistributeTokenPerShare.Mul(userInfo.Amount).Div(Multiplier)
                    .Sub(userInfo.RewardDistributeTokenDebt);

                var userLockReward = pool.AccLockDistributeTokenPerShare
                    .Mul(userInfo.Amount)
                    .Div(Multiplier)
                    .Sub(userInfo.RewardLockDistributeTokenDebt)
                    .Add(userInfo.LockPending);
                stillLockReward = GetUserLockReward(
                    pid,
                    user,
                    pool.AccDistributeTokenPerShare
                );
                var pendingAmount = Convert.ToInt64(userReward.Add(userLockReward).Sub(
                    stillLockReward
                ).ToString());
                if (pendingAmount > 0)
                {
                    DistributeTokenTransfer(user, pendingAmount);
                    State.UserInfoMap[pid][user].ClaimedAmount = userInfo.ClaimedAmount.Add(pendingAmount);
                    Context.Fire(new ClaimRevenue
                    {
                        User = user,
                        Amount = pendingAmount,
                        Pid = pid,
                        Token = DistributeToken
                    });
                }

                var usdtReward = Convert.ToInt64(pool.AccUsdtPerShare.Mul(userInfo.Amount)
                    .Div(Multiplier)
                    .Sub(userInfo.RewardUsdtDebt).ToString());

                if (usdtReward <= 0) return;
                UsdtTransfer(user, usdtReward);
                Context.Fire(new ClaimRevenue
                {
                    User = user,
                    Amount = usdtReward,
                    Pid = pid,
                    Token = Usdt
                });
            }

            if (amount > 0) {
                LpTokenTransferIn(user, Context.Self, pool.LpToken, amount);
                State.UserInfoMap[pid][user].Amount = userInfo.Amount.Add(amount);
                State.PoolInfoMap[pid].TotalAmount = pool.TotalAmount.Add(amount);
            }
            State.UserInfoMap[pid][user].RewardDistributeTokenDebt = pool.AccDistributeTokenPerShare.Mul(userInfo.Amount).Div(Multiplier);
            State.UserInfoMap[pid][user].LockPending = stillLockReward;
            State.UserInfoMap[pid][user].RewardLockDistributeTokenDebt = pool.AccLockDistributeTokenPerShare.Mul(userInfo.Amount).Div(Multiplier);
                
            State.UserInfoMap[pid][user].RewardUsdtDebt = pool.AccUsdtPerShare.Mul(userInfo.Amount).Div(Multiplier);
            State.UserInfoMap[pid][user].LastRewardBlock = Math.Max(Context.CurrentHeight, pool.LastRewardBlock);
            Context.Fire(new  Deposit()
            {    
                Pid = pid,
                User = user,
                Amount = amount
            }); 
            
        }

        private void WithdrawInternal(int pid, long amount, Address user)
        {
            var pool = State.PoolInfoMap[pid];
            var userInfo = State.UserInfoMap[pid][user];
            Assert(userInfo.Amount >= amount, "withdraw: Insufficient amount");
            UpdatePoolInternal(pid);
            long stillLockReward = 0;
            if (userInfo.Amount >= 0)
            {
                var userReward = pool.AccDistributeTokenPerShare.Mul(userInfo.Amount).Div(Multiplier)
                    .Sub(userInfo.RewardDistributeTokenDebt);

                var userLockReward = pool.AccLockDistributeTokenPerShare
                    .Mul(userInfo.Amount)
                    .Div(Multiplier)
                    .Sub(userInfo.RewardLockDistributeTokenDebt)
                    .Add(userInfo.LockPending);
                stillLockReward = GetUserLockReward(
                    pid,
                    user,
                    pool.AccDistributeTokenPerShare
                );
                var pendingAmount = Convert.ToInt64(userReward.Add(userLockReward).Sub(
                    stillLockReward
                ).ToString());
                if (pendingAmount > 0)
                {
                    DistributeTokenTransfer(user, pendingAmount);
                    State.UserInfoMap[pid][user].ClaimedAmount = userInfo.ClaimedAmount.Add(pendingAmount);
                    Context.Fire(new ClaimRevenue
                    {
                        User = user,
                        Amount = pendingAmount,
                        Pid = pid,
                        Token = DistributeToken
                    });
                }

                var usdtReward = Convert.ToInt64(pool.AccUsdtPerShare.Mul(userInfo.Amount)
                    .Div(Multiplier)
                    .Sub(userInfo.RewardUsdtDebt).ToString());

                if (usdtReward <= 0) return;
                UsdtTransfer(user, usdtReward);
                Context.Fire(new ClaimRevenue
                {
                    User = user,
                    Amount = usdtReward,
                    Pid = pid,
                    Token = Usdt
                });
            }
            if (amount > 0) {
                LpTokenTransferOut(user, pool.LpToken, amount);
                State.UserInfoMap[pid][user].Amount = userInfo.Amount.Sub(amount);
                State.PoolInfoMap[pid].TotalAmount = pool.TotalAmount.Sub(amount);
            }
            State.UserInfoMap[pid][user].RewardDistributeTokenDebt = pool.AccDistributeTokenPerShare.Mul(userInfo.Amount).Div(Multiplier);
            State.UserInfoMap[pid][user].LockPending = stillLockReward;
            State.UserInfoMap[pid][user].RewardLockDistributeTokenDebt = pool.AccLockDistributeTokenPerShare.Mul(userInfo.Amount).Div(Multiplier);
                
            State.UserInfoMap[pid][user].RewardUsdtDebt = pool.AccUsdtPerShare.Mul(userInfo.Amount).Div(Multiplier);
            State.UserInfoMap[pid][user].LastRewardBlock = Math.Max(Context.CurrentHeight, pool.LastRewardBlock);
            Context.Fire(new  Withdraw()
            {    
                Pid = pid,
                User = user,
                Amount = amount
            }); 

        }
        private long GetUserLockReward(int pid, Address user, BigIntValue accLockDistributeTokenPerShare)
        {
            var userInfo = State.UserInfoMap[pid][user];
            var halvingPeriod = State.HalvingPeriod0.Value.Add(State.HalvingPeriod1.Value);
            var userAmount = userInfo.Amount;
            var m = Phase(Context.CurrentHeight);
            var n = Phase(userInfo.LastRewardBlock);

            if(m > Phase(State.EndBlock.Value)){
                return 0;
            }
            var lastAccLockDistributeTokenPerShare = GetLastAccLockDistributeTokenPerShare(pid);
            BigIntValue totalLockPendingAmount;
            if (m > n) {
                var rewardDebt = lastAccLockDistributeTokenPerShare.Mul(userAmount).Div(
                    Multiplier
                );
                totalLockPendingAmount = accLockDistributeTokenPerShare.Mul(userAmount)
                    .Div(Multiplier)
                    .Sub(rewardDebt);
            } else {
                totalLockPendingAmount =accLockDistributeTokenPerShare.Mul(userAmount) 
                    .Div(Multiplier)
                    .Sub(userInfo.RewardLockDistributeTokenDebt)
                    .Add(userInfo.LockPending);

            }
            var switchBlock = m.Mul(halvingPeriod).Add(State.StartBlockOfDistributeToken.Value).Add(
                State.HalvingPeriod0.Value
            );
            BigIntValue realLockDistributeTokenReward;
            if (userInfo.LastRewardBlock < switchBlock) {
                if (Context.CurrentHeight > switchBlock) {
                    var unLockDistributeTokenReward = totalLockPendingAmount.Mul(Context.CurrentHeight
                            .Sub(switchBlock))
                        .Div(State.HalvingPeriod1.Value);
                    realLockDistributeTokenReward = totalLockPendingAmount.Sub(unLockDistributeTokenReward);
                } else realLockDistributeTokenReward = totalLockPendingAmount;
            } else {
                var unLockDistributeTokenReward = totalLockPendingAmount.Mul(Context.CurrentHeight
                        .Sub(userInfo.LastRewardBlock)) 
                 
                    .Div(State.HalvingPeriod1.Value.Add(switchBlock).Sub(userInfo.LastRewardBlock));
                realLockDistributeTokenReward = totalLockPendingAmount.Sub(unLockDistributeTokenReward);
            }
            return Convert.ToInt64(realLockDistributeTokenReward.ToString());
        }

        private void DistributeTokenTransfer(Address to, long amount)
        {
            State.TokenContract.Transfer.Send(new TransferInput()
            {
                Symbol = DistributeToken,
                Amount = amount,
                To = to
            });
        }
        
        private void UsdtTransfer(Address to, long amount)
        {
            State.TokenContract.Transfer.Send(new TransferInput()
            {
                Symbol = Usdt,
                Amount = amount,
                To = to
            });
        }
        private void UsdtTransferIn(Address from, Address to, long amount)
        {
            State.TokenContract.TransferFrom.Send(new AElf.Contracts.MultiToken.TransferFromInput()
            {
                Symbol = Usdt,
                Amount = amount,
                From = from,
                To = to
            });
        }

        private void LpTokenTransferIn(Address from, Address to, string symbol, long amount)
        {
            State.LpTokenContract.TransferFrom.Send(new TransferFromInput()
            {
                From = from,
                To = Context.Self,
                Symbol = symbol,
                Amount = amount
            });
        }
        private void LpTokenTransferOut(Address to, string symbol, long amount)
        {
            State.LpTokenContract.Transfer.Send(new Token.TransferInput()
            {
                Symbol = symbol,
                Amount = amount,
                To = to
            });
        }

         private long PendingDistributeToken(int pid, Address user)
        {
            var pool = State.PoolInfoMap[pid];
            var userInfo = State.UserInfoMap[pid][user];
            var accDistributeTokenPerShare = pool.AccDistributeTokenPerShare;
            var accLockDistributeTokenPerShare = pool.AccLockDistributeTokenPerShare;
            if (userInfo.Amount < 0) return 0;
            if (userInfo.Amount == 0 && userInfo.LockPending == 0) {
                return 0;
            }
            long stillLockReward = 0;
            BigIntValue userReward= 0;
            BigIntValue userLockAllReward = 0;
            if (Context.CurrentHeight <= pool.LastRewardBlock)
                return Convert.ToInt64(userReward.Add(userLockAllReward).Sub(stillLockReward).ToString());
            if (pool.TotalAmount == 0) {
                userLockAllReward = userInfo.LockPending;
                stillLockReward = GetUserLockReward(pid, user, 0);
            }
            else {
                GetDistributeTokenBlockRewardInternal(pool.LastRewardBlock,  out var blockReward,out var blockLockReward);
                blockReward = blockReward.Mul(pool.AllocPoint).Div(
                    State.TotalAllocPoint.Value
                );
                blockLockReward = blockLockReward.Mul(pool.AllocPoint).Div(
                    State.TotalAllocPoint.Value
                );
                accDistributeTokenPerShare = accDistributeTokenPerShare.Add(new BigIntValue(blockReward)
                    .Mul(Multiplier).Div(pool.TotalAmount)
                );
                accLockDistributeTokenPerShare = accLockDistributeTokenPerShare.Add(
                    new BigIntValue(blockLockReward).Mul(Multiplier).Div(pool.TotalAmount)
                );
                userReward = accDistributeTokenPerShare.Mul(userInfo.Amount).Div(Multiplier).Sub(
                    userInfo.RewardDistributeTokenDebt
                );

                userLockAllReward = accLockDistributeTokenPerShare.Mul(userInfo
                        .Amount).
                     
                    Div(Multiplier)
                    .Sub(userInfo.RewardLockDistributeTokenDebt)
                    .Add(userInfo.LockPending);
                stillLockReward = GetUserLockReward(
                    pid,
                    user,
                    accLockDistributeTokenPerShare
                ); }
            return Convert.ToInt64(userReward.Add(userLockAllReward).Sub(stillLockReward).ToString());
        }

         private long PendingUsdt(int pid, Address user)
         {
             var pool = State.PoolInfoMap[pid];
             var userInfo = State.UserInfoMap[pid][user];

             var lpSupply = pool.TotalAmount;

             if (Context.CurrentHeight <= State.UsdtStartBlock.Value || lpSupply == 0) return 0;
             long subtractor;
             long multiplier = 0;
             if (Context.CurrentHeight <=  State.UsdtEndBlock.Value) {
                 subtractor = Math.Max(pool.LastRewardBlock, State.UsdtStartBlock.Value); 
                 multiplier = Context.CurrentHeight.Sub(subtractor);
             } else {
                 if (pool.LastRewardBlock < State.UsdtEndBlock.Value) {
                     subtractor = Math.Max(pool.LastRewardBlock, State.UsdtStartBlock.Value); 
                     multiplier = State.UsdtEndBlock.Value.Sub(subtractor);
                 }
             }
                 
             var usdtReward = multiplier
                 .Mul(State.UsdtPerBlock.Value)
                 .Mul(pool.AllocPoint)
                 .Div(State.TotalAllocPoint.Value);
             var accUsdtPerShare = pool.AccUsdtPerShare.Add(new BigIntValue(usdtReward)
                 .Mul(Multiplier).Div(lpSupply)
             );

             return
                 Convert.ToInt64(accUsdtPerShare.Mul(userInfo.Amount).Div(Multiplier).Sub(
                         userInfo.RewardUsdtDebt).ToString() 
                 );
         }
    }
    
    
}