using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Relics;

public sealed class NlothsGift : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;
    
    internal static Player? CurrentRewardPlayer;
    internal static NlothsGift? InstanceToFlash;
    internal static bool ShouldFlash;
    internal static float LastRoll;
    internal static float OriginalThreshold;
}