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
        
        internal Awaken.Contracts.Swap.AwakenSwapContractContainer.AwakenSwapContractReferenceState RouterContract { get; set; }
        
        internal Gandalf.Contracts.PoolTwoContract.PoolTwoContractContainer.PoolTwoContractReferenceState FarmTwoPoolContract { get; set; }
        public SingletonState<Address> Admin { get; set; }
        public SingletonState<Address> Owner { get; set; }
        /// <summary>
        /// Pid -> UserAddress -> RedepositAmount
        /// </summary>
        public MappedState<int, Address, long> RedepositAmount{ get; set; }
        public SingletonState<long> StartBlockOfDistributeToken { get; set; }
        public SingletonState<long> DistributeTokenPerBlockConcentratedMining { get; set; }
        public SingletonState<long> DistributeTokenPerBlockContinuousMining { get; set; }
        public SingletonState<long> UsdtPerBlock { get; set; }
        public SingletonState<long> UsdtStartBlock { get; set; }
        public SingletonState<long> UsdtEndBlock { get; set; }
        public SingletonState<long> Cycle { get; set; }
        /// <summary>
        /// Pid -> PoolInfo
        /// </summary>
        public MappedState<int, PoolInfo> PoolInfoMap{ get; set; }
        /// <summary>
        /// Pid -> UserAddress -> UserInfo
        /// </summary>
        public MappedState<int, Address, UserInfo> UserInfoMap{ get; set; }

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
      
        
    }
}