using ActsFromThePast.Acts.TheCity.Events;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Patches.Events;

public class ColosseumPatches
{
    private static readonly HashSet<Type> ColosseumEncounters = new()
    {
        typeof(ColosseumSecondEncounter)
    };

    [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.StartCombatInternal))]
    public class ReplayWriterFixPatch
    {
        public static void Prefix()
        {
            if (!Colosseum.NeedsReplayFix)
                return;
            Colosseum.NeedsReplayFix = false;
            var replayWriter = RunManager.Instance.CombatReplayWriter;
            if (!replayWriter.IsEnabled || replayWriter.IsRecordingReplay)
                return;
            replayWriter.RecordInitialState(
                RunManager.Instance.ToSave(null));
        }
    }

    [HarmonyPatch(typeof(RewardsSet), nameof(RewardsSet.WithRewardsFromRoom))]
    public class RewardsPatch
    {
        public static void Postfix(RewardsSet __result, AbstractRoom room)
        {
            if (room is not CombatRoom combatRoom)
                return;
            if (!ColosseumEncounters.Contains(
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
}