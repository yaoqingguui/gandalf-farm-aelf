using AElf.Boilerplate.TestBase;
using AElf.Cryptography.ECDSA;

namespace Awaken.Contracts.Farm
{
    public class FarmContractTestBase : DAppContractTestBase<FarmContractTestModule>
    {
        // You can get address of any contract via GetAddress method, for example:
        // internal Address DAppContractAddress => GetAddress(DAppSmartContractAddressNameProvider.StringName);

        internal FarmContractContainer.FarmContractStub GetFarmContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<FarmContractContainer.FarmContractStub>(DAppContractAddress, senderKeyPair);
        }
    }
}