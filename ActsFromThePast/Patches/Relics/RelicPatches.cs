
using System.Reflection;
using System.Reflection.Emit;
using ActsFromThePast.Relics;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Patches.Relics;

public class RelicPatches
{
   
    [HarmonyPatch(typeof(NRewardButton), "Reload")]
    public static class NRewardButtonGoldenIdolPatch
    {
        public static void Postfix(NRewardButton __instance)
        {
            if (__instance.Reward is not GoldReward goldReward)
                return;
            
            if (!GoldenIdol.BoostedRewards.TryGetValue(goldReward, out var info))
                return;

            var labelField = AccessTools.Field(typeof(NRewardButton), "_label");
            var label = (MegaRichTextLabel)labelField.GetValue(__instance);
        
            // Replace the total with "baseAmount Gold (+bonus)"
            var baseText = new LocString("gameplay_ui", "COMBAT_REWARD_GOLD");
            baseText.Add("gold", (decimal)info.baseAmount);
            label.Text = $"{baseText.GetFormattedText()} ({info.bonus})";
        }
    }
    
    [HarmonyPatch(typeof(NRelicCollectionCategory), nameof(NRelicCollectionCategory.LoadRelics))]
public static class RelicCollectionTranspiler
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        
        // Find the error string as our anchor point
        int errorStringIndex = -1;
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Ldstr && 
                codes[i].operand is string s && 
                s.Contains("act list"))
            {
                errorStringIndex = i;
                break;
            }
        }
        
        if (errorStringIndex == -1)
            return codes;
        
        // Find the throw after the error string
        int throwIndex = -1;
        for (int i = errorStringIndex; i < codes.Count && i < errorStringIndex + 5; i++)
        {
            if (codes[i].opcode == OpCodes.Throw)
            {
                throwIndex = i;
                break;
            }
        }
        
        if (throwIndex == -1)
            return codes;
        
        // Find the start of the block (ldc.i4.4 before error string)
        int blockStart = -1;
        for (int i = errorStringIndex - 1; i >= 0; i--)
        {
            if (codes[i].opcode == OpCodes.Ldc_I4_4)
            {
                blockStart = i;
                break;
            }
        }
        
        if (blockStart == -1)
            return codes;
        
        // Find the stloc that stores to the actModelList local
        CodeInstruction stlocInstruction = null;
        for (int i = blockStart; i < errorStringIndex; i++)
        {
            if (codes[i].opcode == OpCodes.Stloc_S &&
                codes[i].operand is LocalBuilder lb &&
                lb.LocalType?.IsGenericType == true &&
                lb.LocalType.GetGenericTypeDefinition() == typeof(List<>))
            {
                stlocInstruction = codes[i].Clone();
                break;
            }
        }
        
        if (stlocInstruction == null)
            return codes;
        
        // Build replacement: ModelDb.Acts.ToList()
        var actModelType = typeof(ModelDb).Assembly.GetTypes().First(t => t.Name == "ActModel");
        var actsGetter = AccessTools.PropertyGetter(typeof(ModelDb), "Acts");
        var toListMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == "ToList" && m.GetParameters().Length == 1)
            .MakeGenericMethod(actModelType);
        
        var replacement = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Call, actsGetter),
            new CodeInstruction(OpCodes.Call, toListMethod),
            stlocInstruction
        };
        
        if (codes[blockStart].labels.Count > 0)
            replacement[0].labels.AddRange(codes[blockStart].labels);
        
        int removeCount = throwIndex - blockStart + 1;
        codes.RemoveRange(blockStart, removeCount);
        codes.InsertRange(blockStart, replacement);
        
        return codes;
    }
}

[HarmonyPatch(typeof(CardFactory), "CreateForReward", 
    typeof(Player), typeof(int), typeof(CardCreationOptions))]
public static class NlothsGiftCardFactoryPatch
{
    [HarmonyPrefix]
    public static void SetCurrentPlayer(Player player)
    {
        NlothsGift.CurrentRewardPlayer = player;
        NlothsGift.ShouldFlash = false;
        NlothsGift.InstanceToFlash = player.Relics.OfType<NlothsGift>().FirstOrDefault();
    }
    
    [HarmonyPostfix]
    public static void ClearCurrentPlayer()
    {
        if (NlothsGift.ShouldFlash && NlothsGift.InstanceToFlash != null)
        {
            NlothsGift.InstanceToFlash.Flash();
        }
        
        NlothsGift.CurrentRewardPlayer = null;
        NlothsGift.InstanceToFlash = null;
        NlothsGift.ShouldFlash = false;
    }
}

[HarmonyPatch(typeof(CardRarityOdds), nameof(CardRarityOdds.RollWithoutChangingFutureOdds), 
    typeof(CardRarityOddsType), typeof(float))]
public static class NlothsGiftRarityPatch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        
        for (int i = 0; i < codes.Count; i++)
        {
            yield return codes[i];
            
            if ((codes[i].opcode == OpCodes.Callvirt || codes[i].opcode == OpCodes.Call) && 
                codes[i].operand is MethodInfo method && 
                method.Name == "NextFloat")
            {
                yield return new CodeInstruction(OpCodes.Dup);
                yield return new CodeInstruction(OpCodes.Call, 
                    AccessTools.Method(typeof(NlothsGiftRarityPatch), nameof(CaptureRoll)));
            }
        }
    }
    
    public static void CaptureRoll(float roll)
    {
        NlothsGift.LastRoll = roll;
    }
    
    [HarmonyPrefix]
    public static void ModifyOdds(CardRarityOddsType type, ref float offset)
    {
        if (type == CardRarityOddsType.BossEncounter || type == CardRarityOddsType.Shop)
        {
            NlothsGift.OriginalThreshold = type == CardRarityOddsType.BossEncounter ? 1f : 0f;
            return;
        }
    
        var baseOdds = GetBaseRareOdds(type);
        NlothsGift.OriginalThreshold = baseOdds + offset;
    
        if (NlothsGift.InstanceToFlash == null)
            return;
        
        var tripled = NlothsGift.OriginalThreshold * 3f;
        offset = tripled - baseOdds;
    }
    
    [HarmonyPostfix]
    public static void CheckForFlash(CardRarity __result)
    {
        if (__result != CardRarity.Rare)
            return;
            
        if (NlothsGift.InstanceToFlash == null)
            return;
        
        if (NlothsGift.LastRoll >= NlothsGift.OriginalThreshold)
        {
            NlothsGift.ShouldFlash = true;
        }
    }
    
    private static float GetBaseRareOdds(CardRarityOddsType type)
    {
        return type switch
        {
            CardRarityOddsType.RegularEncounter => CardRarityOdds.RegularRareOdds,
            CardRarityOddsType.EliteEncounter => CardRarityOdds.EliteRareOdds,
            CardRarityOddsType.Shop => CardRarityOdds.ShopRareOdds,
            CardRarityOddsType.Uniform => 0.33f,
            _ => 0f
        };
    }
}
}