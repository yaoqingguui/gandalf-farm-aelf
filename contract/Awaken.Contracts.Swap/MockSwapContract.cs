using System;
using Google.Protobuf.WellKnownTypes;

namespace Awaken.Contracts.Swap
{
    public class AwakenSwapContract :AwakenSwapContractContainer.AwakenSwapContractBase
    {
        public override AddLiquidityOutput AddLiquidity(AddLiquidityInput input)
        {
            return new AddLiquidityOutput();
        }
    }
}