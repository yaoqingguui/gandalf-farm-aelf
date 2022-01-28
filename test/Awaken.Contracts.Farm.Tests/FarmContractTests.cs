using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.ContractTestBase.ContractTestKit;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Awaken.Contracts.Token;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace Awaken.Contracts.Farm
{
    public class FarmContractTests : FarmContractTestBase
    {
        [Fact]
        public async Task InitializeTest()
        {
            await Initialize();
        }
        private async Task Initialize()
        {
            await CreateAndGetToken();
            var height = await GetCurrentBlockHeight();
            var startBlock = height.Add(10);
            await AdminStub.Initialize.SendAsync(new InitializeInput()
            {
                LpTokenContract = LpTokenContractAddress,
                Admin = AdminAddress,
                Block0 = 50,
                Block1 = 100,
                Cycle = 50,
                DistributeTokenPerBlock0 = 154300000000,
                DistributeTokenPerBlock1 =   7300000000,
                StartBlock = startBlock ,
                TotalReward = 15834375000000
            });
            var endBlock = await AdminStub.GetEndBlock.CallAsync(new Empty());
            endBlock.Value.Sub(startBlock).ShouldBe(600);
        }

        [Fact]
        public async Task AddPoolTest()
        {
            await Initialize();
            var allocPoint = 10;
            var symbol = GetTokenPairSymbol("ELF", "TEST");
            //No permission.
            var permissionExceptionAsync = await UserTomStub.AddPool.SendWithExceptionAsync(new AddPoolInput()
            {
                AllocPoint = allocPoint,
                LpToken = symbol,
                WithUpdate = false
            });
            permissionExceptionAsync.TransactionResult.Error.ShouldContain("No permission.");
            
            //Invalid input
            var inputExceptionAsync = await AdminStub.AddPool.SendWithExceptionAsync(new AddPoolInput()
            {
                AllocPoint = allocPoint,
                WithUpdate = false
            });
            inputExceptionAsync.TransactionResult.Error.ShouldContain("Invalid input");
           
            //SUCCESS
            await AdminStub.AddPool.SendAsync(new AddPoolInput()
            {
                AllocPoint = allocPoint,
                LpToken = symbol,
                WithUpdate = false
            });
           var poolInfo = await AdminStub.GetPoolInfo.CallAsync(new Int32Value()
            {
                Value = 0
            });
           poolInfo.AllocPoint.ShouldBe(allocPoint);
           poolInfo.LpToken.ShouldBe(symbol);
            
        }

        [Fact]
        public async Task DepositTest()
        {
            await Initialize();
            var allocPoint = 10;
            var symbol = GetTokenPairSymbol("ELF", "TEST");
            await AdminStub.AddPool.SendAsync(new AddPoolInput()
            {
                AllocPoint = allocPoint,
                LpToken = symbol,
                WithUpdate = false
            });

            var amount = 10000000000;
            await UserTomStub.Deposit.SendAsync(new DepositInput()
            {
                Pid = 0,
                Amount = amount
            });
           var userInfo = await UserTomStub.GetUserInfo.CallAsync(new GetUserInfoInput()
            {
                Pid = 0,
                User = UserTomAddress
            });
           var balance = await TomLpStub.GetBalance.CallAsync(new GetBalanceInput()
           {
               Owner = UserTomAddress,
               Symbol = symbol
           });
           
           userInfo.Amount.ShouldBe(amount);
           balance.Amount.ShouldBe(0);

           await AdminStub.UpdatePool.SendAsync(new Int32Value() {Value = 0});
           await AdminStub.MassUpdatePools.SendAsync(new Empty());
           await AdminStub.FixEndBlock.SendAsync(new BoolValue() {Value = false});
        }
        
        [Fact]
        public async Task WithdrawTest()
        {
            await Initialize();
            var allocPoint = 10;
            var symbol = GetTokenPairSymbol("ELF", "TEST");
            await AdminStub.AddPool.SendAsync(new AddPoolInput()
            {
                AllocPoint = allocPoint,
                LpToken = symbol,
                WithUpdate = false
            });

            var amount = 10000000000;
            await UserTomStub.Deposit.SendAsync(new DepositInput()
            {
                Pid = 0,
                Amount = amount
            });
            await UserTomStub.Withdraw.SendAsync(new WithdrawInput()
            {
                Pid = 0,
                Amount = amount
            });
            var userInfo = await UserTomStub.GetUserInfo.CallAsync(new GetUserInfoInput()
            {
                Pid = 0,
                User = UserTomAddress
            });
            var balance = await TomLpStub.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = UserTomAddress,
                Symbol = symbol
            });
            userInfo.Amount.ShouldBe(0);
            balance.Amount.ShouldBe(amount);
        }

        [Fact]
        public async Task NewRewardTest()
        {
            await Initialize();
            var allocPoint = 10;
            var symbol = GetTokenPairSymbol("ELF", "TEST");
            await AdminStub.AddPool.SendAsync(new AddPoolInput()
            {
                AllocPoint = allocPoint,
                LpToken = symbol,
                WithUpdate = false
            });

            var amount = 10000000000;
            var newPerBlock = 100000000;
            var height = await GetCurrentBlockHeight();
            var startBlock = height.Add(10);
            //Tool not set
            var toolExceptionAsync = await AdminStub.NewReward.SendWithExceptionAsync(new NewRewardInput()
            {
                StartBlock = startBlock,
                UsdtAmount = amount,
                NewPerBlock = newPerBlock
            });
            toolExceptionAsync.TransactionResult.Error.ShouldContain("Tool not set");
            //
            await AdminStub.SetTool.SendAsync(AdminAddress);
            await AdminStub.NewReward.SendAsync(new NewRewardInput()
            {
                StartBlock = startBlock,
                UsdtAmount = amount,
                NewPerBlock = newPerBlock
            });
            var cycle = await AdminStub.GetCycle.CallAsync(new Empty());
            cycle.Value.ShouldBe(50);
        }
        [Fact]
        public async Task RewardTest()
        {
            await Initialize();
            var allocPoint = 10;
            var symbol = GetTokenPairSymbol("ELF", "TEST");
            await AdminStub.AddPool.SendAsync(new AddPoolInput()
            {
                AllocPoint = allocPoint,
                LpToken = symbol,
                WithUpdate = false
            });
            var amount = 10000000000;
            await UserTomStub.Deposit.SendAsync(new DepositInput()
            {
                Pid = 0,
                Amount = amount
            });
            var startBlock = await AdminStub.GetStartBlockOfDistributeToken.CallAsync(new Empty());
            var distributeTokenPerBlockConcentratedMining = await AdminStub.GetDistributeTokenPerBlockConcentratedMining.CallAsync(new Empty());
            var distributeTokenPerBlockContinuousMining = await AdminStub.GetDistributeTokenPerBlockContinuousMining.CallAsync(new Empty());
            var phase0reward = distributeTokenPerBlockConcentratedMining.Value.Mul(50);
            var phase1reward = distributeTokenPerBlockContinuousMining.Value.Mul(100);
            var expectBalance = phase0reward.Add(phase1reward);
            await SkipToBlockHeight(startBlock.Value.Add(150));
            await UserTomStub.Withdraw.SendAsync(new WithdrawInput()
            {
                Pid = 0,
                Amount = amount.Div(2)
            });
            var balance = await UserTomTokenContractStub.GetBalance.CallAsync(new AElf.Contracts.MultiToken.GetBalanceInput()
            {
                Owner = UserTomAddress,
                Symbol = "AWAKEN"
            });
              balance.Balance.ShouldBe(expectBalance);
              
              //usdt reward
            
              var newPerBlock = 100000000;
              var height = await GetCurrentBlockHeight();
              await AdminStub.SetTool.SendAsync(AdminAddress);
              await AdminStub.NewReward.SendAsync(new NewRewardInput()
              {
                  StartBlock = height.Add(10),
                  UsdtAmount = amount,
                  NewPerBlock = newPerBlock
              });
              await SkipToBlockHeight(height.Add(20));
              var pending = await UserTomStub.Pending.CallAsync(new PendingInput()
              {
                  Pid = 0,
                  User = UserTomAddress
              });
              var pendingLockDistributeToken = await UserTomStub.PendingLockDistributeToken.CallAsync(new PendingLockDistributeTokenInput()
              {
                  Pid = 0,
                  User = UserTomAddress
              });
              await UserTomStub.GetReDepositLimit.CallAsync(new GetReDepositLimitInput()
              {
                  Pid = 0,
                  User = UserTomAddress
              });
              await UserTomStub.Withdraw.SendAsync(new WithdrawInput()
              {
                  Pid = 0,
                  Amount = amount.Div(2)
              });
              await UserTomStub.GetReDepositLimit.CallAsync(new GetReDepositLimitInput()
              {
                  Pid = 0,
                  User = UserTomAddress
              });
        }

        [Fact]
        public async Task SetTest()
        {
            await Initialize();
            var allocPoint = 10;
            var symbol = GetTokenPairSymbol("ELF", "TEST");
            await AdminStub.AddPool.SendAsync(new AddPoolInput()
            {
                AllocPoint = allocPoint,
                LpToken = symbol,
                WithUpdate = false
            });
            //SetTool
            await AdminStub.SetTool.SendAsync(UserTomAddress);
            //SetHalvingPeriod
            var block0 = 10;
            var block1 = 20;
            await AdminStub.SetHalvingPeriod.SendAsync(new SetHalvingPeriodInput()
            {
                Block0 = block0,
                Block1 = block1
            });
            var halvingPeriod0 =await AdminStub.GetHalvingPeriod0.CallAsync(new Empty());
            var halvingPeriod1 =await AdminStub.GetHalvingPeriod1.CallAsync(new Empty());
            halvingPeriod0.Value.ShouldBe(block0);
            halvingPeriod1.Value.ShouldBe(block1);
            //SetDistributeTokenPerBlock
            var perBlock0 = 1000000000;
            var perBlock1 = 2000000000;
            await AdminStub.SetDistributeTokenPerBlock.SendAsync(new SetDistributeTokenPerBlockInput()
            {
                PerBlock0 = perBlock0,
                PerBlock1 = perBlock1

            });
            var perBlock0Real =await AdminStub.GetDistributeTokenPerBlockConcentratedMining.CallAsync(new Empty());
            var perBlock1Real =await AdminStub.GetDistributeTokenPerBlockContinuousMining.CallAsync(new Empty());
            perBlock0Real.Value.ShouldBe(perBlock0);
            perBlock1Real.Value.ShouldBe(perBlock1);
            //SetOwner
            await AdminStub.SetOwner.SendAsync(UserTomAddress);
            var owner = await AdminStub.GetOwner.CallAsync(new Empty());
            owner.ShouldBe(UserTomAddress);
            //SetAdmin
            await AdminStub.SetAdmin.SendAsync(UserTomAddress);
            var admin = await AdminStub.GetAdmin.CallAsync(new Empty());
            admin.ShouldBe(UserTomAddress);
            //SetReDeposit
            await UserTomStub.SetReDeposit.SendAsync(new SetReDepositInput()
            {
            });
        }

        [Fact]
        public async Task GetTest()
        {
            await Initialize();
            var allocPoint = 10;
            var symbol = GetTokenPairSymbol("ELF", "TEST");
            await AdminStub.AddPool.SendAsync(new AddPoolInput()
            {
                AllocPoint = allocPoint,
                LpToken = symbol,
                WithUpdate = false
            });

            var amount = 10000000000;
            var newPerBlock = 100000000;
            var height = await GetCurrentBlockHeight();
            var startBlock = height.Add(10);
            await AdminStub.SetTool.SendAsync(AdminAddress);
            await AdminStub.NewReward.SendAsync(new NewRewardInput()
            {
                StartBlock = startBlock,
                UsdtAmount = amount,
                NewPerBlock = newPerBlock
            });
       
            await SkipToBlockHeight(startBlock.Add(150));
            var totalReward = await AdminStub.GetTotalReward.CallAsync(new Empty());
            totalReward.Value.ShouldBe(15834375000000);
            var usdtEndBlock =await AdminStub.GetUsdtEndBlock.CallAsync(new Empty());
            usdtEndBlock.Value.ShouldBe(startBlock.Add(50));
            var usdtPerBlock = await AdminStub.GetUsdtPerBlock.CallAsync(new Empty());
            usdtPerBlock.Value.ShouldBe(newPerBlock);
            var usdtStartBlock = await AdminStub.GetUsdtStartBlock.CallAsync(new Empty());
            usdtStartBlock.Value.ShouldBe(startBlock);

            var issuedReward = await AdminStub.GetIssuedReward.CallAsync(new Empty());
            issuedReward.Value.ShouldBe(0);

            await AdminStub.GetDistributeTokenBlockReward.CallAsync(new Int64Value() {Value = startBlock.Add(50)});
        }

        [Fact]
        public async Task RedepositTest()
        {
            await Initialize();
            var allocPoint = 10;
            var symbol = GetTokenPairSymbol("ELF", "TEST");
            await AdminStub.AddPool.SendAsync(new AddPoolInput()
            {
                AllocPoint = allocPoint,
                LpToken = symbol,
                WithUpdate = false
            });
            var amount = 10000000000;
            await UserTomStub.Deposit.SendAsync(new DepositInput()
            {
                Pid = 0,
                Amount = amount
            });
            var startBlock = await AdminStub.GetStartBlockOfDistributeToken.CallAsync(new Empty());
            var distributeTokenPerBlockConcentratedMining = await AdminStub.GetDistributeTokenPerBlockConcentratedMining.CallAsync(new Empty());
            var distributeTokenPerBlockContinuousMining = await AdminStub.GetDistributeTokenPerBlockContinuousMining.CallAsync(new Empty());
            var phase0reward = distributeTokenPerBlockConcentratedMining.Value.Mul(50);
            var phase1reward = distributeTokenPerBlockContinuousMining.Value.Mul(100);
            var expectBalance = phase0reward.Add(phase1reward);
            await SkipToBlockHeight(startBlock.Value.Add(150));
            await UserTomStub.Withdraw.SendAsync(new WithdrawInput()
            {
                Pid = 0,
                Amount = amount.Div(2)
            });
            await AdminStub.SetReDeposit.SendAsync(new SetReDepositInput()
            {
                FarmTwoPool = PoolTwoContractAddress,
                Router = RouterContractAddress
            });

            await UserTomStub.ReDeposit.SendAsync(new ReDepositInput()
            {
                DistributeTokenAmount = 10000000,
                ElfAmount = 10000000,
                Pid = 0
            });
        }
        private static string GetTokenPairSymbol(string tokenA, string tokenB)
        {
            var symbols = RankSymbols(tokenA, tokenB);
            return $"GLP {symbols[0]}-{symbols[1]}";
        }
        private static string[] RankSymbols(params string[] symbols)
        {
            return symbols.OrderBy(s => s).ToArray();
        }
         private async Task CreateAndGetToken()
        {
            //Create token 
            //TEST
            var result = await AdminTokenContractStub.Create.SendAsync(new AElf.Contracts.MultiToken.CreateInput
            {
                Issuer = AdminAddress,
                Symbol = "TEST",
                Decimals = 8,
                IsBurnable = true,
                TokenName = "TEST symbol",
                TotalSupply = 100000000_00000000
            }); 
            await AdminTokenContractStub.Create.SendAsync(new AElf.Contracts.MultiToken.CreateInput
            {
                Issuer = AdminAddress,
                Symbol = "USDT",
                Decimals = 8,
                IsBurnable = true,
                TokenName = "USDT symbol",
                TotalSupply = 100000000_00000000
            });
            //AWAKEN
            var result2 = await AdminTokenContractStub.Create.SendAsync(new AElf.Contracts.MultiToken.CreateInput
            {
                Issuer = FarmContractAddress,
                Symbol = "AWAKEN",
                Decimals = 8,
                IsBurnable = true,
                TokenName = "AWAKEN symbol",
                TotalSupply = 100000000_00000000
            });

            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            // ELF-TEST
            await AdminLpStub.Initialize.SendAsync(new Token.InitializeInput()
            {
                Owner = AdminAddress
            });
            await AdminLpStub.Create.SendAsync(new CreateInput()
            {
                Symbol = GetTokenPairSymbol("ELF", "TEST"),
                Decimals = 0,
                TokenName = $"Gandalf {GetTokenPairSymbol("ELF", "TEST")} LP Token",
                Issuer = AdminAddress,
                IsBurnable = true,
                TotalSupply = long.MaxValue
            });

            // Distribute Tokens
            //ELF
            await AdminTokenContractStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
            {
                Amount = 100000000000,
                Symbol = "ELF",
                Memo = "Recharge",
                To = UserTomAddress
            });
              
            await AdminTokenContractStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
            {
                Amount = 100000000000,
                Symbol = "ELF",
                Memo = "Recharge",
                To = UserLilyAddress
            });
            //test
            var issueResult = await AdminTokenContractStub.Issue.SendAsync(new AElf.Contracts.MultiToken.IssueInput
            {
                Amount = 100000000000000,
                Symbol = "TEST",
                To = AdminAddress
            });
            
            await AdminTokenContractStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
            {
                Amount = 100000000000,
                Symbol = "TEST",
                Memo = "Recharge",
                To = UserTomAddress
            });
              
            await AdminTokenContractStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
            {
                Amount = 100000000000,
                Symbol = "TEST",
                Memo = "Recharge",
                To = UserLilyAddress
            });
            //USDT
            
            await AdminTokenContractStub.Issue.SendAsync(new AElf.Contracts.MultiToken.IssueInput
            {
                Amount = 100000000000000,
                Symbol = "USDT",
                To = AdminAddress
            });
            
            await AdminTokenContractStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
            {
                Amount = 100000000000,
                Symbol = "USDT",
                Memo = "Recharge",
                To = UserTomAddress
            });
              
            await AdminTokenContractStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
            {
                Amount = 100000000000,
                Symbol = "USDT",
                Memo = "Recharge",
                To = UserLilyAddress
            });
     
            

            //LP
            await AdminLpStub.Issue.SendAsync(new IssueInput()
            {
                Amount = 100000000000,
                Symbol = GetTokenPairSymbol("ELF", "TEST"),
                To = AdminAddress
            });
            await AdminLpStub.Transfer.SendAsync(new TransferInput()
            {
                Amount = 10000000000,
                Symbol =  GetTokenPairSymbol("ELF", "TEST"),
                Memo = "Recharge",
                To = UserTomAddress
            });
            await AdminLpStub.Transfer.SendAsync(new TransferInput()
            {
                Amount = 10000000000,
                Symbol =  GetTokenPairSymbol("ELF", "TEST"),
                Memo = "Recharge",
                To = UserLilyAddress
            });
          
            
            //Approve
            await UserTomTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            {
                Amount = 100000000000,
                Spender = FarmContractAddress,
                Symbol = "ELF"
            });
            await UserTomTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            {
                Amount = 100000000000,
                Spender = FarmContractAddress,
                Symbol = "TEST"
            });
            await UserTomTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            {
                Amount = 100000000000,
                Spender = FarmContractAddress,
                Symbol = "USDT"
            });
            await UserTomTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            {
                Amount = 100000000000,
                Spender = FarmContractAddress,
                Symbol = "AWAKEN"
            });
            await TomLpStub.Approve.SendAsync(new ApproveInput()
            {
                Amount = 100000000000,
                Spender = FarmContractAddress,
                Symbol = GetTokenPairSymbol("ELF", "TEST")
            });
            
            
            await UserLilyTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            {
                Amount = 100000000000,
                Spender = FarmContractAddress,
                Symbol = "ELF"
            });

            await UserLilyTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            {
                Amount = 100000000000,
                Spender = FarmContractAddress,
                Symbol = "TEST"
            });
            await UserLilyTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            {
                Amount = 100000000000,
                Spender = FarmContractAddress,
                Symbol = "USDT"
            });
            await AdminTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            {
                Amount = 100000000000,
                Spender = FarmContractAddress,
                Symbol = "USDT"
            });
        }

         private async Task<long> GetCurrentBlockHeight()
         {
             var blockChain = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
             return AsyncHelper.RunSync(blockChain.GetChainAsync).BestChainHeight;
         }

         private async Task SkipToBlockHeight(long blockNumber)
         {
             var current = await GetCurrentBlockHeight();
             var span = blockNumber.Sub(current);
             for (var i = 0; i < span; i++)
             {
                 await AdminTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
                 {
                     Amount = 1,
                     Symbol = "ELF",
                     Spender = UserLilyAddress
                 });
             } 
             current = await GetCurrentBlockHeight();
             current.ShouldBe(blockNumber);
         }
        
         
    }
    
}