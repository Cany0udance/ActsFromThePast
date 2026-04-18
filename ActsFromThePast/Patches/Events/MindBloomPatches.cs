using ActsFromThePast.Acts.TheBeyond.Encounters;
using ActsFromThePast.Acts.TheBeyond.Events;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Patches.Events;

public class MindBloomPatches
{
    private static readonly HashSet<Type> MindBloomEncounters = new()
    {
        typeof(MindBloomGuardian),
        typeof(MindBloomHexaghost),
        typeof(MindBloomSlimeBoss)
    };

    [HarmonyPatch(typeof(RewardsSet), nameof(RewardsSet.WithRewardsFromRoom))]
    public class RewardsPatch
    {
        public static void Postfix(RewardsSet __result, AbstractRoom room)
        {
            if (room is not CombatRoom combatRoom)
                return;
            if (!MindBloomEncounters.Contains(
                    combatRoom.Encounter.GetType()))
                return;

            var extraRewards = combatRoom.ExtraRewards.Values
                .SelectMany(list => list)
                .ToHashSet();

            __result.Rewards.RemoveAll(r =>
                !extraRewards.Contains(r) &&
                r is GoldReward or RelicReward);
        }
    }
    
    [HarmonyPatch(typeof(CustomEncounterModel), nameof(CustomEncounterModel.RoomType), MethodType.Getter)]
    public class MindBloomEncounterRoomTypePatch
    {
        public static void Postfix(CustomEncounterModel __instance, ref RoomType __result)
        {
            if (MindBloom.CombatActive && __instance is MindBloomGuardian or MindBloomHexaghost or MindBloomSlimeBoss)
                __result = RoomType.Boss;
        }
    }
}