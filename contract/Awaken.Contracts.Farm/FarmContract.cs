using System;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Awaken.Contracts.Farm
{
    /// <summary>
    /// The C# implementation of the contract defined in farm_contract.proto that is located in the "protobuf"
    /// folder.
    /// Notice that it inherits from the protobuf generated code. 
    /// </summary>
    public partial class FarmContract : FarmContractContainer.FarmContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.TokenContract.Value == null, "Already initialized.");
            Assert(input.StartBlock > Context.CurrentHeight,"Invalid Input:StartBlock");
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.LpTokenContract.Value = input.LpTokenContract;
            State.Admin.Value = input.Admin ?? Context.Sender;
            State.Owner.Value = Context.Sender;
            State.HalvingPeriod0.Value = input.Block0;
            State.HalvingPeriod1.Value = input.Block1;
            State.DistributeTokenPerBlockConcentratedMining.Value = input.DistributeTokenPerBlock0;
            State.DistributeTokenPerBlockContinuousMining.Value = input.DistributeTokenPerBlock1;
            State.StartBlockOfDistributeToken.Value = input.StartBlock;
            State.Cycle.Value = input.Cycle;
            State.TotalReward.Value = input.TotalReward;
            FixEndBlockInternal(false);
            return new Empty();
        }
        
    }
}