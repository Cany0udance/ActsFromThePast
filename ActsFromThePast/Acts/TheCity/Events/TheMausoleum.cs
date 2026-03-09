using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class TheMausoleum : EventModel
{
    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "ghosts");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new EventOption[]
        {
            new EventOption(this, new Func<Task>(OpenOption),
                "THE_MAUSOLEUM.pages.INITIAL.options.OPEN",
                HoverTipFactory.FromCard(ModelDb.Card<Writhe>())),
            new EventOption(this, new Func<Task>(LeaveOption),
                "THE_MAUSOLEUM.pages.INITIAL.options.LEAVE",
                Array.Empty<IHoverTip>())
        };
    }

    private async Task OpenOption()
    {
        // TODO: Screen shake if possible
        // CardCrawlGame.sound.play("BLUNT_HEAVY");
        // CardCrawlGame.screenShake.rumble(2.0F);
        
        var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(relic, Owner);
        
        var writhe = Owner.RunState.CreateCard(ModelDb.Card<Writhe>(), Owner);
        var curseResult = await CardPileCmd.Add(writhe, PileType.Deck);
        CardCmd.PreviewCardPileAdd(curseResult, 2f);

        SetEventFinished(L10NLookup("THE_MAUSOLEUM.pages.OPEN_CURSED.description"));
    }

    private async Task LeaveOption()
    {
        SetEventFinished(L10NLookup("THE_MAUSOLEUM.pages.LEAVE.description"));
    }
}