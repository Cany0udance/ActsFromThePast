/*

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.Cards;

public sealed class TestCard : CardModel
{
    protected override int CanonicalEnergyCost => 1;
    public override CardType Type => CardType.Skill;
    public override CardRarity Rarity => CardRarity.Uncommon;
    public override TargetType TargetType => TargetType.Self;

    public override CardPoolModel Pool => ModelDb.CardPool<SilentCardPool>();
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<SilentCardPool>();

    public override string PortraitPath => ModelDb.Card<BubbleBubble>().PortraitPath;

    protected override Task OnPlay(PlayerChoiceContext choiceContext, Creature? target)
    {
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
    }
}

*/