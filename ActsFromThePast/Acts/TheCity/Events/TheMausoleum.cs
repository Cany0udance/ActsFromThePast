using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class TheMausoleum : CustomEventModel
{
    public override ActModel[] Acts => new[] { ModelDb.Act<TheCityAct>() };

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "ghosts");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            Option(Open, "INITIAL", HoverTipFactory.FromCard(ModelDb.Card<Writhe>())),
            Option(Leave)
        };
    }

    private async Task Open()
    {
        // TODO: Screen shake if possible
        // CardCrawlGame.sound.play("BLUNT_HEAVY");
        // CardCrawlGame.screenShake.rumble(2.0F);

        var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(relic, Owner);

        var writhe = Owner.RunState.CreateCard(ModelDb.Card<Writhe>(), Owner);
        var curseResult = await CardPileCmd.Add(writhe, PileType.Deck);
        CardCmd.PreviewCardPileAdd(curseResult, 2f);
        SetEventFinished(PageDescription("OPEN_CURSED"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
    }
}