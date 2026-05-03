using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class NlothsHungryFace : CustomRelicModel
{
    
    // TODO investigate why this doesn't remove gold from chests
    
    private bool _isUsed;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool IsUsedUp => _isUsed;

    [SavedProperty]
    public bool IsUsed
    {
        get => _isUsed;
        set
        {
            AssertMutable();
            _isUsed = value;
            if (_isUsed)
                Status = RelicStatus.Disabled;
        }
    }

    public override bool ShouldGenerateTreasure(Player player)
    {
        if (player != Owner || _isUsed)
            return true;

        // If Silver Crucible is also suppressing this chest, let it take priority
        // so we don't waste our one-time use on a chest that was already empty
        var silverCrucible = Owner.Relics.OfType<SilverCrucible>().FirstOrDefault();
        if (silverCrucible != null && !silverCrucible.ShouldGenerateTreasure(player))
            return true;

        IsUsed = true;
        Flash();
        return false;
    }
}