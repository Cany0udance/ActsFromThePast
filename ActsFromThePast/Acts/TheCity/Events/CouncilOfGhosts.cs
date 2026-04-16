using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class CouncilOfGhosts : CustomEventModel
{
    public override ActModel[] Acts => new[] { ModelDb.Act<TheCityAct>() };

    private const int ApparitionCount = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new HpLossVar(0),
        new IntVar("ApparitionCount", ApparitionCount)
    };

    public override void CalculateVars()
    {
        var hpLoss = (int)Math.Ceiling(Owner.Creature.MaxHp * 0.5M);
        if (hpLoss >= Owner.Creature.MaxHp)
        {
            hpLoss = Owner.Creature.MaxHp - 1;
        }
        DynamicVars.HpLoss.BaseValue = hpLoss;
    }

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "ghosts");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            Option(Accept, "INITIAL", HoverTipFactory.FromCard(ModelDb.Card<Apparition>())),
            Option(Refuse)
        };
    }

    private async Task Accept()
    {
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars.HpLoss.BaseValue,
            false);

        var apparitionResults = new List<CardPileAddResult>();
        for (var i = 0; i < ApparitionCount; i++)
        {
            var apparition = Owner.RunState.CreateCard(ModelDb.Card<Apparition>(), Owner);
            apparitionResults.Add(await CardPileCmd.Add(apparition, PileType.Deck));
        }
        CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)apparitionResults, 2f);
        await Cmd.Wait(0.75f);

        SetEventFinished(PageDescription("ACCEPT"));
    }

    private async Task Refuse()
    {
        SetEventFinished(PageDescription("REFUSE"));
    }
}