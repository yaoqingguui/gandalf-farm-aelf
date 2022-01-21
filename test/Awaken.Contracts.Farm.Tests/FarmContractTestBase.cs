using AElf.Boilerplate.TestBase;
using AElf.Cryptography.ECDSA;
using System.IO;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Token;
using AElf.Standards.ACS0;
using Awaken.Contracts.Token;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;

namespace Awaken.Contracts.Farm
{
    public class FarmContractTestBase : DAppContractTestBase<FarmContractTestModule>
    {
        // You can get address of any contract via GetAddress method, for example:
        internal readonly Address FarmContractAddress;
        
        internal readonly Address LpTokenContractAddress;
        private Address tokenContractAddress => GetAddress(TokenSmartContractAddressNameProvider.StringName);

        internal FarmContractContainer.FarmContractStub GetFarmContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<FarmContractContainer.FarmContractStub>(DAppContractAddress, senderKeyPair);
        }
        internal Awaken.Contracts.Token.TokenContractContainer.TokenContractStub GetLpContractStub(
            ECKeyPair senderKeyPair)
        {
            return Application.ServiceProvider.GetRequiredService<IContractTesterFactory>()
                .Create<Awaken.Contracts.Token.TokenContractContainer.TokenContractStub>(LpTokenContractAddress, senderKeyPair);
        }

        internal AElf.Contracts.MultiToken.TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair senderKeyPair)
        {
            return Application.ServiceProvider.GetRequiredService<IContractTesterFactory>()
                .Create<AElf.Contracts.MultiToken.TokenContractContainer.TokenContractStub>(tokenContractAddress, senderKeyPair);
        }

        public FarmContractTestBase()
        {
            FarmContractAddress = AsyncHelper.RunSync(() => DeployContractAsync(
                KernelConstants.DefaultRunnerCategory,
                File.ReadAllBytes(typeof(FarmContract).Assembly.Location), SampleAccount.Accounts[0].KeyPair));
            LpTokenContractAddress = AsyncHelper.RunSync(() => DeployContractAsync(
                KernelConstants.DefaultRunnerCategory,
                File.ReadAllBytes(typeof(Token.TokenContract).Assembly.Location), SampleAccount.Accounts[0].KeyPair));
         
        }
        private async Task<Address> DeployContractAsync(int category, byte[] code, ECKeyPair keyPair)
        {
            var addressService = Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();
            var stub = GetTester<ACS0Container.ACS0Stub>(addressService.GetZeroSmartContractAddress(),
                keyPair);
            var executionResult = await stub.DeploySmartContract.SendAsync(new ContractDeploymentInput
            {
                Category = category,
                Code = ByteString.CopyFrom(code)
            });
            return executionResult.Output;
        }
        private ECKeyPair AdminKeyPair { get; set; } = SampleAccount.Accounts[0].KeyPair;
        private ECKeyPair UserTomKeyPair { get; set; } = SampleAccount.Accounts.Last().KeyPair;
        private ECKeyPair UserLilyKeyPair { get; set; } = SampleAccount.Accounts.Reverse().Skip(1).First().KeyPair;
        
        internal Address AdminAddress => Address.FromPublicKey(AdminKeyPair.PublicKey);
        internal Address UserTomAddress => Address.FromPublicKey(UserTomKeyPair.PublicKey);
        internal Address UserLilyAddress => Address.FromPublicKey(UserLilyKeyPair.PublicKey);

       
        internal FarmContractContainer.FarmContractStub AdminStub =>
            GetFarmContractStub(AdminKeyPair);
        internal FarmContractContainer.FarmContractStub UserTomStub =>
            GetFarmContractStub(UserTomKeyPair);

        internal FarmContractContainer.FarmContractStub UserLilyStub =>
            GetFarmContractStub(UserLilyKeyPair);

        internal AElf.Contracts.MultiToken.TokenContractContainer.TokenContractStub AdminTokenContractStub =>
            GetTokenContractStub(AdminKeyPair);
        internal AElf.Contracts.MultiToken.TokenContractContainer.TokenContractStub UserTomTokenContractStub =>
            GetTokenContractStub(UserTomKeyPair);

        internal AElf.Contracts.MultiToken.TokenContractContainer.TokenContractStub UserLilyTokenContractStub =>
            GetTokenContractStub(UserLilyKeyPair);

        internal Awaken.Contracts.Token.TokenContractContainer.TokenContractStub AdminLpStub =>
            GetLpContractStub(AdminKeyPair);

        internal Awaken.Contracts.Token.TokenContractContainer.TokenContractStub TomLpStub =>
            GetLpContractStub(UserTomKeyPair);
    }
}