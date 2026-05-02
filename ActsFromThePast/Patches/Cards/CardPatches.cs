using System.Reflection;
using ActsFromThePast.Cards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Patches.Cards;
[HarmonyPatch(typeof(Burn))]
public static class BurnUpgradePatch
{
    public static bool AllowBurnUpgrade = false;

    [HarmonyPatch(nameof(Burn.MaxUpgradeLevel), MethodType.Getter)]
    [HarmonyPostfix]
    static void MaxUpgradeLevel_Postfix(ref int __result)
    {
        __result = AllowBurnUpgrade ? 1 : 0;
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.UpgradeInternal))]
    [HarmonyPostfix]
    static void UpgradeInternal_Postfix(CardModel __instance)
    {
        if (__instance is Burn burn && burn.IsUpgraded)
        {
            burn.DynamicVars.Damage.UpgradeValueBy(2M);
        }
    }
    
}

[HarmonyPatch(typeof(CardModel), "ToMutable")]
public class TagClassicSlimedPatch
{
    public static void Postfix(CardModel __result)
    {
        if (__result is Slimed && ClassicSlimedTracker.CreatingClassicSlimed)
        {
            ClassicSlimedTracker.IsClassicSlimed.Set(__result, true);
        }
    }
}

[HarmonyPatch]
public class ClassicSlimedDescriptionPatch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CardModel), "GetDescriptionForPile",
            new[] { typeof(PileType), AccessTools.Inner(typeof(CardModel), "DescriptionPreviewType"), typeof(Creature) });
    }

    public static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not Slimed)
            return;
        if (!ClassicSlimedTracker.IsClassicSlimed.Get(__instance))
            return;

        var desc = __instance.Description;
        __instance.DynamicVars.AddTo(desc);
        var descText = desc.GetFormattedText();

        __result = __result.Replace(descText, "").Trim('\n');
    }
}

[HarmonyPatch(typeof(Slimed), "OnPlay")]
public class ClassicSlimedOnPlayPatch
{
    public static bool Prefix(Slimed __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        if (!ClassicSlimedTracker.IsClassicSlimed.Get(__instance))
            return true;

        var child = NGoopyImpactVfx.Create(__instance.Owner.Creature);
        if (child != null)
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(child);

        __result = Task.CompletedTask;
        return false;
    }
}


[HarmonyPatch(typeof(MerchantEntry), nameof(MerchantEntry.Cost), MethodType.Getter)]
public static class TheBoxFreePurchasePatch
{
    public static void Postfix(ref int __result)
    {
        if (TheBoxTracker.NextPurchaseFree)
            __result = 0;
    }
}

[HarmonyPatch(typeof(MerchantEntry), nameof(MerchantEntry.OnTryPurchaseWrapper))]
public static class TheBoxIgnoreCostPatch
{
    public static void Prefix(ref bool ignoreCost)
    {
        if (TheBoxTracker.NextPurchaseFree)
            ignoreCost = true;
    }
}

[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.RemoveFromDeck), new[] { typeof(CardModel), typeof(bool) })]
public static class TheBoxRemovalPatch
{
    public static void Prefix(CardModel card)
    {
        if (card is not TheBox)
            return;

        var currentRoom = card.Owner?.RunState?.CurrentRoom;
        if (currentRoom?.RoomType != RoomType.Shop)
            return;

        TheBoxTracker.NextPurchaseFree = true;
        TheBoxTracker.SkipNextCompletion = true;
        TheBoxTracker.ShowRemovalDialogue = true;
        TheBoxTracker.PlayerHasBox = false;
    }
}

[HarmonyPatch(typeof(MerchantEntry), nameof(MerchantEntry.InvokePurchaseCompleted))]
public static class TheBoxResetAfterPurchasePatch
{
    public static void Prefix(MerchantEntry __instance)
    {
        if (__instance is MerchantCardRemovalEntry)
            TheBoxTracker.CardRemovalUsed = true;

        if (TheBoxTracker.SkipNextCompletion)
        {
            TheBoxTracker.SkipNextCompletion = false;
            return;
        }

        if (TheBoxTracker.NextPurchaseFree)
            TheBoxTracker.NextPurchaseFree = false;
    }
}

[HarmonyPatch(typeof(MerchantRoom), nameof(MerchantRoom.EnterInternal))]
public static class TheBoxResetOnShopEnterPatch
{
    public static void Prefix(IRunState runState)
    {
        TheBoxTracker.NextPurchaseFree = false;
        TheBoxTracker.SkipNextCompletion = false;
        TheBoxTracker.ShowRemovalDialogue = false;
        TheBoxTracker.CardRemovalUsed = false;
        TheBoxTracker.PlayerHasBox = runState?.Players.Any(p =>
            p.Deck.Cards.Any(c => c is TheBox)) ?? false;
    }
}

[HarmonyPatch]
public static class TheBoxGreenPricePatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        return AccessTools.GetTypesFromAssembly(typeof(NMerchantSlot).Assembly)
            .Where(t => t.IsSubclassOf(typeof(NMerchantSlot)) && !t.IsAbstract)
            .Select(t => AccessTools.Method(t, "UpdateVisual"))
            .Where(m => m != null);
    }

    public static void Postfix(MegaLabel ____costLabel)
    {
        if (!TheBoxTracker.NextPurchaseFree || ____costLabel == null)
            return;

        ____costLabel.Modulate = StsColors.green;
    }
}

[HarmonyPatch(typeof(NMerchantDialogue), nameof(NMerchantDialogue.ShowOnInventoryOpen))]
public static class TheBoxMerchantOpenPatch
{
    private static readonly LocString _boxLine1 = new LocString("merchant_room", "ACTSFROMTHEPAST-MERCHANT.talk.openInventory.theBox.1");
    private static readonly LocString _boxLine2 = new LocString("merchant_room", "ACTSFROMTHEPAST-MERCHANT.talk.openInventory.theBox.2");

    public static bool Prefix(NMerchantDialogue __instance)
    {
        if (!TheBoxTracker.PlayerHasBox || TheBoxTracker.CardRemovalUsed)
            return true;

        var showRandom = AccessTools.Method(typeof(NMerchantDialogue), "ShowRandom");
        showRandom?.Invoke(__instance, new object[] { new List<LocString> { _boxLine1, _boxLine2 } });
        return false;
    }
}

[HarmonyPatch(typeof(NMerchantDialogue), nameof(NMerchantDialogue.ShowForPurchaseAttempt))]
public static class TheBoxPurchaseDialoguePatch
{
    private static readonly LocString _removeLine1 = new LocString("merchant_room", "ACTSFROMTHEPAST-MERCHANT.talk.purchaseSuccess.theBox.1");
    private static readonly LocString _removeLine2 = new LocString("merchant_room", "ACTSFROMTHEPAST-MERCHANT.talk.purchaseSuccess.theBox.2");

    public static bool Prefix(NMerchantDialogue __instance, PurchaseStatus status)
    {
        if (!TheBoxTracker.ShowRemovalDialogue || status != PurchaseStatus.Success)
            return true;

        TheBoxTracker.ShowRemovalDialogue = false;

        var showRandom = AccessTools.Method(typeof(NMerchantDialogue), "ShowRandom");
        showRandom?.Invoke(__instance, new object[] { new List<LocString> { _removeLine1, _removeLine2 } });
        return false;
    }
}