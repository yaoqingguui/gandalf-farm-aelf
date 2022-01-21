using System.Linq;
using System.Threading.Tasks;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Types;
using Awaken.Contracts.Token;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Awaken.Contracts.Farm
{
    public class FarmContractTests : FarmContractTestBase
    {
        [Fact]
        public async Task Test()
        {
            // Get a stub for testing.
            var keyPair = SampleAccount.Accounts.First().KeyPair;
            await Initialize();
        }
        private async Task Initialize()
        {
            await CreateAndGetToken();
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

            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var issueResult = await AdminTokenContractStub.Issue.SendAsync(new AElf.Contracts.MultiToken.IssueInput
            {
                Amount = 100000000000000,
                Symbol = "TEST",
                To = AdminAddress
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var balance = await AdminTokenContractStub.GetBalance.SendAsync(new AElf.Contracts.MultiToken.GetBalanceInput()
            {
                Owner = AdminAddress,
                Symbol = "TEST"
            });
            balance.Output.Balance.ShouldBe(100000000000000);
            //DAI
            var result2 = await AdminTokenContractStub.Create.SendAsync(new AElf.Contracts.MultiToken.CreateInput
            {
                Issuer = AdminAddress,
                Symbol = "DAI",
                Decimals = 10,
                IsBurnable = true,
                TokenName = "DAI symbol",
                TotalSupply = 100000000_00000000
            });

            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var issueResult2 = await AdminTokenContractStub.Issue.SendAsync(new AElf.Contracts.MultiToken.IssueInput
            {
                Amount = 100000000000000,
                Symbol = "DAI",
                To = AdminAddress
            });
            issueResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var balance2 = await AdminTokenContractStub.GetBalance.SendAsync(new AElf.Contracts.MultiToken.GetBalanceInput()
            {
                Owner = AdminAddress,
                Symbol = "DAI"
            });
            balance2.Output.Balance.ShouldBe(100000000000000);
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
            await AdminTokenContractStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
            {
                Amount = 100000000000,
                Symbol = "DAI",
                Memo = "Recharge",
                To = UserTomAddress
            });
            //authorize  Tom and Lily and admin to transfer ELF and TEST and DAI to FinanceContract
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
                Symbol = "DAI"
            });
            await AdminTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            {
                Amount = 100000000000,
                Spender = FarmContractAddress,
                Symbol = "ELF"
            });
            await UserLilyTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
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
            await AdminTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            {
                Amount = 100000000000,
                Spender = FarmContractAddress,
                Symbol = "TEST"
            });
            await UserLilyTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            {
                Amount = 100000000000,
                Spender = FarmContractAddress,
                Symbol = "TEST"
            });
        }
    }
}