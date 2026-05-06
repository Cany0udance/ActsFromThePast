using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.SharedEvents;

public sealed class DesignerInSpire : CustomEventModel, IActRestricted
{
    private const int AdjustCost = 50;
    private const int CleanUpCost = 75;
    private const int FullServiceCost = 110;
    private const int HpLoss = 5;
    
    public int[] AllowedActIndices => new[] { 2, 3 };

    private bool _adjustmentUpgradesOne;
    private bool _cleanUpRemovesCards;

    public override ActModel[] Acts => Array.Empty<ActModel>();
    
    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.All(p => p.Gold >= CleanUpCost);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("AdjustCost", AdjustCost),
        new IntVar("CleanUpCost", CleanUpCost),
        new IntVar("FullServiceCost", FullServiceCost),
        new IntVar("HpLoss", HpLoss)
    };

    public override void CalculateVars()
    {
        _adjustmentUpgradesOne = Rng.NextInt(2) == 0;
        _cleanUpRemovesCards = Rng.NextInt(2) == 0;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[] { Option(Continue) };
    }

    private Task Continue()
    {
        var options = new List<EventOption>();

        bool canAffordAdjust = Owner.Gold >= AdjustCost;
        bool hasUpgradable = Owner.Deck.Cards.Any(c => c.IsUpgradable);
        bool canAffordCleanUp = Owner.Gold >= CleanUpCost;
        bool canAffordFullService = Owner.Gold >= FullServiceCost;
        bool hasRemovable = Owner.Deck.Cards.Any(c => c.IsRemovable);
        bool hasTwoRemovable = Owner.Deck.Cards.Count(c => c.IsRemovable) >= 2;

        // Adjustments
        if (canAffordAdjust && hasUpgradable)
        {
            var optionKey = _adjustmentUpgradesOne
                ? $"{Id.Entry}.pages.MAIN.options.ADJUST_UPGRADE_ONE"
                : $"{Id.Entry}.pages.MAIN.options.ADJUST_UPGRADE_TWO";
            options.Add(new EventOption(this,
                _adjustmentUpgradesOne ? AdjustUpgradeOne : AdjustUpgradeTwo,
                optionKey, Array.Empty<IHoverTip>()));
        }
        else
        {
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.MAIN.options.ADJUST_LOCKED",
                Array.Empty<IHoverTip>()));
        }

        // Clean Up
        if (_cleanUpRemovesCards)
        {
            if (canAffordCleanUp && hasRemovable)
            {
                options.Add(new EventOption(this, CleanUpRemove,
                    $"{Id.Entry}.pages.MAIN.options.CLEANUP_REMOVE",
                    Array.Empty<IHoverTip>()));
            }
            else
            {
                options.Add(new EventOption(this, null,
                    $"{Id.Entry}.pages.MAIN.options.CLEANUP_LOCKED",
                    Array.Empty<IHoverTip>()));
            }
        }
        else
        {
            if (canAffordCleanUp && hasTwoRemovable)
            {
                options.Add(new EventOption(this, CleanUpTransform,
                    $"{Id.Entry}.pages.MAIN.options.CLEANUP_TRANSFORM",
                    Array.Empty<IHoverTip>()));
            }
            else
            {
                options.Add(new EventOption(this, null,
                    $"{Id.Entry}.pages.MAIN.options.CLEANUP_LOCKED",
                    Array.Empty<IHoverTip>()));
            }
        }

        // Full Service
        if (canAffordFullService && hasRemovable)
        {
            options.Add(new EventOption(this, FullService,
                $"{Id.Entry}.pages.MAIN.options.FULL_SERVICE",
                Array.Empty<IHoverTip>()));
        }
        else
        {
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.MAIN.options.FULL_SERVICE_LOCKED",
                Array.Empty<IHoverTip>()));
        }

        // Punch
        options.Add(new EventOption(this, Punch,
            $"{Id.Entry}.pages.MAIN.options.PUNCH",
            Array.Empty<IHoverTip>()).ThatDoesDamage(HpLoss));

        SetEventState(PageDescription("MAIN"), options);
        return Task.CompletedTask;
    }

    private async Task AdjustUpgradeOne()
    {
        await PlayerCmd.LoseGold(AdjustCost, Owner, GoldLossType.Spent);
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
        var card = (await CardSelectCmd.FromDeckForUpgrade(Owner, prefs)).FirstOrDefault();
        if (card != null)
            CardCmd.Upgrade(card);
        SetEventFinished(PageDescription("SERVICE"));
    }

    private async Task AdjustUpgradeTwo()
    {
        await PlayerCmd.LoseGold(AdjustCost, Owner, GoldLossType.Spent);
        foreach (var card in Owner.Deck.Cards
            .Where(c => c.IsUpgradable)
            .ToList()
            .StableShuffle(Owner.RunState.Rng.Niche)
            .Take(2))
        {
            CardCmd.Upgrade(card);
        }
        SetEventFinished(PageDescription("SERVICE"));
    }

    private async Task CleanUpRemove()
    {
        await PlayerCmd.LoseGold(CleanUpCost, Owner, GoldLossType.Spent);
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        await CardPileCmd.RemoveFromDeck(
            (await CardSelectCmd.FromDeckForRemoval(Owner, prefs)).ToList());
        SetEventFinished(PageDescription("SERVICE"));
    }

    private async Task CleanUpTransform()
    {
        await PlayerCmd.LoseGold(CleanUpCost, Owner, GoldLossType.Spent);
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 2);
        foreach (var original in (await CardSelectCmd.FromDeckForTransformation(Owner, prefs)).ToList())
        {
            await CardCmd.TransformToRandom(original, Rng, CardPreviewStyle.EventLayout);
        }
        SetEventFinished(PageDescription("SERVICE"));
    }

    private async Task FullService()
    {
        await PlayerCmd.LoseGold(FullServiceCost, Owner, GoldLossType.Spent);
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        await CardPileCmd.RemoveFromDeck(
            (await CardSelectCmd.FromDeckForRemoval(Owner, prefs)).ToList());

        var upgradable = Owner.Deck.Cards
            .Where(c => c.IsUpgradable)
            .ToList()
            .StableShuffle(Owner.RunState.Rng.Niche)
            .Take(1);
        foreach (var card in upgradable)
            CardCmd.Upgrade(card);

        SetEventFinished(PageDescription("SERVICE"));
    }

    private async Task Punch()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            HpLoss,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null, null);

        var portrait = Node?.FindChild("Portrait", true, false) as TextureRect;
        if (portrait != null)
        {
            portrait.Texture = PreloadManager.Cache.GetTexture2D(
                ImageHelper.GetImagePath("events/actsfromthepast-designer_in_spire_punched.png"));
        }
        
        NDebugAudioManager.Instance.Play("blunt_attack.mp3");

        SetEventFinished(PageDescription("PUNCH"));
    }
}