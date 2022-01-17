using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace Awaken.Contracts.Farm
{
    /// <summary>
    /// The state class of the contract, it inherits from the AElf.Sdk.CSharp.State.ContractState type. 
    /// </summary>
    public class FarmContractState : ContractState
    {
        internal AElf.Contracts.MultiToken.TokenContractContainer.TokenContractReferenceState TokenContract
        {
            get;
            set;
        }
        internal Token.TokenContractContainer.TokenContractReferenceState LpTokenContract { get; set; }
        public SingletonState<Address> Admin { get; set; }
        public SingletonState<Address> Owner { get; set; }
        public MappedState<int, Address, long> RedepositAmount{ get; set; }
        public SingletonState<long> StartBlockOfDistributeToken { get; set; }
        public SingletonState<long> DistributeTokenPerBlockConcentratedMining { get; set; }
        public SingletonState<long> DistributeTokenPerBlockContinuousMining { get; set; }
        public SingletonState<long> UsdtPerBlock { get; set; }
        public SingletonState<long> UsdtStartBlock { get; set; }
        public SingletonState<long> UsdtEndBlock { get; set; }
        public SingletonState<long> Cycle { get; set; }
        public MappedState<int, PoolInfo> PoolInfo{ get; set; }
        public MappedState<int, Address, UserInfo> UserInfo{ get; set; }

        public SingletonState<long> TotalAllocPoint
        {
            get;
            set;
        }

        public SingletonState<int> PoolLength{ get; set; }
        public SingletonState<long> HalvingPeriod0 { get; set; }
        public SingletonState<long> HalvingPeriod1 { get; set; }
        public SingletonState<long> TotalReward { get; set; }
        public SingletonState<long> IssuedReward { get; set; }
        public SingletonState<long> EndBlock { get; set; }
        
        public SingletonState<Address> Tool { get; set; }   
        public SingletonState<Address> Router { get; set; }  
        public SingletonState<Address> FarmTwoPool { get; set; }  
        
    }
}