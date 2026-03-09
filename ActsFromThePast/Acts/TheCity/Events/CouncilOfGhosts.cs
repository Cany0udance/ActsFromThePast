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

public sealed class CouncilOfGhosts : EventModel
{
    private const int ApparitionCount = 3;
    
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new HpLossVar(0),
                new IntVar("ApparitionCount", ApparitionCount)
            };
        }
    }

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
        return new EventOption[]
        {
            new EventOption(this, new Func<Task>(AcceptOption),
                "COUNCIL_OF_GHOSTS.pages.INITIAL.options.ACCEPT",
                HoverTipFactory.FromCard(ModelDb.Card<Apparition>())),
            new EventOption(this, new Func<Task>(RefuseOption),
                "COUNCIL_OF_GHOSTS.pages.INITIAL.options.REFUSE",
                Array.Empty<IHoverTip>())
        };
    }

    private async Task AcceptOption()
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
    
        SetEventFinished(L10NLookup("COUNCIL_OF_GHOSTS.pages.ACCEPT.description"));
    }

    private async Task RefuseOption()
    {
        SetEventFinished(L10NLookup("COUNCIL_OF_GHOSTS.pages.REFUSE.description"));
    }
}