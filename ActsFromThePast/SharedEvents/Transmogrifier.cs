using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace ActsFromThePast.SharedEvents;

public sealed class Transmogrifier : CustomEventModel, IShrineEvent
{
    public override ActModel[] Acts => Array.Empty<ActModel>();
    
    private const int KneelMaxHpLoss = 10;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("KneelMaxHpLoss", KneelMaxHpLoss)
    };

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
        {
            return new[]
            {
                Option(Pray),
                Option(Kneel, "INITIAL_REBALANCED")
                    .ThatDecreasesMaxHp(KneelMaxHpLoss)
            };
        }
        return new[]
        {
            Option(Pray),
            Option(Leave)
        };
    }
    private async Task Pray()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1);
        foreach (var original in (await CardSelectCmd.FromDeckForTransformation(Owner, prefs)).ToList())
        {
            await CardCmd.TransformToRandom(original, Rng, CardPreviewStyle.EventLayout);
        }

        SetEventFinished(PageDescription("PRAY"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
    
    private async Task Kneel()
    {
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            KneelMaxHpLoss,
            false);

        var cards = Owner.Deck.Cards
            .Where(c => c.Type != CardType.Quest && c.IsTransformable)
            .ToList()
            .StableShuffle(Owner.RunState.Rng.Niche)
            .Take(2)
            .ToList();

        foreach (var original in cards)
        {
            var transformed = CardFactory.CreateRandomCardForTransform(original, false, Owner.RunState.Rng.Niche);
            CardCmd.Upgrade(transformed);
            await CardCmd.Transform(original, transformed);
        }

        SetEventFinished(PageDescription("PRAY"));
    }
}