using Google.Protobuf.WellKnownTypes;

namespace Awaken.Contracts.Farm
{
    public partial class FarmContract
    {
        public override GetDistributeTokenBlockRewardOutput GetDistributeTokenBlockReward(Int64Value input)
        {
            GetDistributeTokenBlockRewardInternal(input.Value,  out var blockReward,out var blockLockReward);
            return new GetDistributeTokenBlockRewardOutput()
            {
                BlockReward = blockReward,
                BlockLockReward = blockLockReward
            };
        }

    }
}