using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class OldBeggar : CustomEventModel
{
    private const int GoldCost = 75;

    public override ActModel[] Acts => new[] { ModelDb.Act<TheCityAct>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("GoldCost", GoldCost)
    };

    private bool CanAfford()
    {
        return Owner.Gold >= GoldCost;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        if (CanAfford())
            options.Add(Option(GiveGold));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.GIVE_GOLD_LOCKED",
                Array.Empty<IHoverTip>()));

        options.Add(Option(Leave));
        return options;
    }

    private async Task GiveGold()
    {
        await PlayerCmd.LoseGold(GoldCost, Owner);
        SetEventState(PageDescription("GAVE_GOLD"), new[]
        {
            Option(RemoveCard, "GAVE_GOLD")
        });

        var portrait = Node?.FindChild("Portrait", true, false) as TextureRect;
        if (portrait != null)
        {
            portrait.Texture = PreloadManager.Cache.GetTexture2D(
                ImageHelper.GetImagePath("events/actsfromthepast-cleric.png"));
        }
    }

    private async Task RemoveCard()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        var selectedCards = await CardSelectCmd.FromDeckForRemoval(Owner, prefs);
        await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)selectedCards.ToList());
        SetEventFinished(PageDescription("REMOVE_CARD"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
    }
}