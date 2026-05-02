
/*

using System.Reflection;
using ActsFromThePast.Acts.TheBeyond;
using ActsFromThePast.Acts.TheCity;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Patches.Ancients;

[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
public class DarvPatches
{
    private static readonly FieldInfo RoomsField = AccessTools.Field(typeof(ActModel), "_rooms");

    public static void Postfix(ActModel __instance)
    {
        if (!ActsFromThePastConfig.LegacyActsOnly || !ActsFromThePastConfig.DarvOnlyInLegacyActs)
            return;

        if (__instance is not TheCityAct and not TheBeyondAct)
            return;

        var rooms = RoomsField.GetValue(__instance) as RoomSet;
        if (rooms == null) return;

        var darv = ModelDb.AncientEvent<Darv>();
        rooms.Ancient = darv;
    }
}

[HarmonyPatch(typeof(Darv), "GenerateInitialOptions")]
public class DarvUniqueOffersPatch
{
    private static readonly FieldInfo ValidRelicSetsField = AccessTools.Field(typeof(Darv), "_validRelicSets");
    private static readonly Type ValidRelicSetType = typeof(Darv).GetNestedType("ValidRelicSet", BindingFlags.NonPublic);
    private static readonly FieldInfo FilterField = ValidRelicSetType.GetField("filter");
    private static readonly FieldInfo RelicsField = ValidRelicSetType.GetField("relics");
    private static readonly MethodInfo RelicOptionMethod = AccessTools.Method(
        typeof(AncientEventModel), "RelicOption",
        new[] { typeof(RelicModel), typeof(string), typeof(string) });

    public static bool Prefix(Darv __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (!ActsFromThePastConfig.LegacyActsOnly || !ActsFromThePastConfig.DarvOnlyInLegacyActs)
            return true;

        var owner = __instance.Owner;
        var previousTitles = DarvOfferTracker.GetPreviouslyOfferedTitles(owner);

        // First visit — no history yet, let vanilla handle it
        if (previousTitles.Count == 0)
            return true;

        var rng = __instance.Rng;
        var validRelicSets = ValidRelicSetsField.GetValue(null) as System.Collections.IList;

        var candidates = new List<RelicModel>();
        foreach (var set in validRelicSets)
        {
            var filter = (Func<Player, bool>)FilterField.GetValue(set);
            if (!filter(owner)) continue;

            var relics = (RelicModel[])RelicsField.GetValue(set);
            var available = relics
                .Where(r => !previousTitles.Contains(r.Title.GetFormattedText()))
                .ToArray();
            if (available.Length > 0)
                candidates.Add(rng.NextItem<RelicModel>(available));
        }

        candidates = candidates.UnstableShuffle(rng);

        bool dustyTomeAvailable = !previousTitles.Contains(
            ModelDb.Relic<DustyTome>().Title.GetFormattedText());

        List<EventOption> list;
        if (rng.NextBool() && dustyTomeAvailable)
        {
            list = candidates.Take(2)
                .Select(r => MakeRelicOption(__instance, r.ToMutable()))
                .ToList();

            var dustyTome = (DustyTome)ModelDb.Relic<DustyTome>().ToMutable();
            if (owner != null) dustyTome.SetupForPlayer(owner);
            list.Add(MakeRelicOption(__instance, dustyTome));
        }
        else
        {
            list = candidates.Take(3)
                .Select(r => MakeRelicOption(__instance, r.ToMutable()))
                .ToList();
        }

        __result = (IReadOnlyList<EventOption>)list;
        return false;
    }

    private static EventOption MakeRelicOption(AncientEventModel instance, RelicModel relic)
    {
        return (EventOption)RelicOptionMethod.Invoke(instance, new object[] { relic, "INITIAL", null });
    }
}

*/