using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class OldBeggar : CustomEventModel
{
    private const int GoldCost = 75;
    private const int SwiftAmount = 2;
    public override ActModel[] Acts => new[] { ModelDb.Act<TheCityAct>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("GoldCost", GoldCost),
        new StringVar("Enchantment", ModelDb.Enchantment<Swift>().Title.GetFormattedText()),
        new IntVar("EnchantmentAmount", SwiftAmount)
    };
    
    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.All(p => p.Gold >= GoldCost);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
        {
            return new[]
            {
                Option(GiveGold),
                Option(KeepWalking, "INITIAL_REBALANCED",
                    HoverTipFactory.FromEnchantment<Swift>(SwiftAmount)
                        .Concat(HoverTipFactory.FromCardWithCardHoverTips<Clumsy>())
                        .ToArray())
            };
        }

        return new[]
        {
            Option(GiveGold),
            Option(Leave)
        };
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
    
    private async Task KeepWalking()
    {
        var swiftModel = ModelDb.Enchantment<Swift>();
        var enchantable = Owner.Deck.Cards.Where(c => swiftModel.CanEnchant(c)).ToList();
        if (enchantable.Count > 0)
        {
            var card = Rng.NextItem(enchantable);
            CardCmd.Enchant<Swift>(card, (decimal)SwiftAmount);
            var child = NCardEnchantVfx.Create(card);
            if (child != null)
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(child);
        }

        var clumsy = Owner.RunState.CreateCard(ModelDb.Card<Clumsy>(), Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(clumsy, PileType.Deck));
        await Cmd.Wait(0.75f);
        SetEventFinished(PageDescription("KEEP_WALKING"));
    }
}