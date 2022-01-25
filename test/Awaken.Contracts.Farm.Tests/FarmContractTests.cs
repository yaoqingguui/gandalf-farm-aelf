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
                Issuer = AdminAddress,
                Symbol = "AWAKEN",
                Decimals = 8,
                IsBurnable = true,
                TokenName = "DAI symbol",
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
     
            //AWAKEN
            await AdminTokenContractStub.Issue.SendAsync(new AElf.Contracts.MultiToken.IssueInput
            {
                Amount = 100000000000000,
                Symbol = "AWAKEN",
                To = AdminAddress
            });
            await AdminTokenContractStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
            {
                Amount = 100000000000,
                Symbol = "AWAKEN",
                Memo = "Recharge",
                To = UserTomAddress
            });
              
            await AdminTokenContractStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
            {
                Amount = 100000000000,
                Symbol = "AWAKEN",
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
        
         
    }
    
}