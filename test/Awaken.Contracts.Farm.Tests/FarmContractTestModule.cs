using System.Collections.Generic;
using System.IO;
using AElf.Boilerplate.TestBase;
using AElf.ContractTestBase;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Awaken.Contracts.Token;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Awaken.Contracts.Farm
{
    [DependsOn(typeof(MainChainDAppContractTestModule))]
    public class FarmContractTestModule : MainChainDAppContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
             Configure<ContractOptions>(o=>o.ContractDeploymentAuthorityRequired=false); 
            // context.Services.AddSingleton<IContractInitializationProvider, FarmContractInitializationProvider>();
            // context.Services.AddSingleton<IContractInitializationProvider, LPTokenContractInitializationProvider>();
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            // var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>();
            // var contractDllLocation = typeof(FarmContract).Assembly.Location;
            // var contractCodes = new Dictionary<string, byte[]>(contractCodeProvider.Codes)
            // {
            //     {
            //         new FarmContractInitializationProvider().ContractCodeName,
            //         File.ReadAllBytes(contractDllLocation)
            //     },
            //     {
            //         new LPTokenContractInitializationProvider().ContractCodeName,
            //         File.ReadAllBytes(typeof(TokenContract).Assembly.Location)
            //     }
            // };
            // contractCodeProvider.Codes = contractCodes;
        }
    }
}