using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Relics;

public sealed class GoldenIdolOriginal : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;
    
    private const decimal GoldMultiplier = 0.25M;
    
    internal static readonly Dictionary<GoldReward, (int baseAmount, int bonus)> BoostedRewards = new();

    private static readonly PropertyInfo AmountProperty = 
        AccessTools.Property(typeof(GoldReward), nameof(GoldReward.Amount));

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room == null || !room.RoomType.IsCombatRoom())
            return false;

        bool modified = false;
        foreach (var reward in rewards.OfType<GoldReward>())
        {
            if (!reward.IsPopulated)
                continue;

            int baseAmount = reward.Amount;
            int bonus = (int)(baseAmount * GoldMultiplier);
            
            AmountProperty.SetValue(reward, baseAmount + bonus);
            BoostedRewards[reward] = (baseAmount, bonus);
            modified = true;
        }

        return modified;
    }

    public override Task AfterModifyingRewards()
    {
        this.Flash();
        return Task.CompletedTask;
    }
}