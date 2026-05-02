using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Cards;

[Pool(typeof(QuestCardPool))]
public sealed class TheBox : CustomCardModel
{
    public TheBox()
        : base(-1, CardType.Quest, CardRarity.Quest, TargetType.None)
    {
    }

    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            return new[] { CardKeyword.Unplayable };
        }
    }
}

public static class TheBoxTracker
{
    public static bool NextPurchaseFree { get; set; }
    public static bool SkipNextCompletion { get; set; }
    public static bool PlayerHasBox { get; set; }
    public static bool ShowRemovalDialogue { get; set; }
    public static bool CardRemovalUsed { get; set; }
}