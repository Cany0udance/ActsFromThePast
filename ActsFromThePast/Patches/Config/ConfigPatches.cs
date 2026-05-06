using System.Reflection;
using ActsFromThePast.Acts;
using ActsFromThePast.Acts.TheBeyond;
using ActsFromThePast.Acts.TheCity;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Patches.Config;

[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
public static class SharedEventFilterPatch
{
    private static readonly FieldInfo RoomsField =
        typeof(ActModel).GetField("_rooms", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly Assembly ModAssembly = typeof(SharedEventFilterPatch).Assembly;

    private static readonly HashSet<EventModel> ModSharedEvents =
        new(CustomContentDictionary.SharedCustomEvents.Where(e => e.GetType().Assembly == ModAssembly));

    private static bool IsLegacyAct(ActModel act) =>
        act is ExordiumAct or TheCityAct or TheBeyondAct;

    private static bool IsModSharedEvent(EventModel e) =>
        ModSharedEvents.Contains(e);

    private static bool IsBaseGameSharedEvent(EventModel e) =>
        ModelDb.AllSharedEvents.Contains(e) && !IsModSharedEvent(e);

    private static int GetActNumber(ActModel act) => act switch
    {
        Overgrowth or Underdocks => 1,
        Hive => 2,
        Glory => 3,
        CustomActModel custom => custom.ActNumber,
        _ => -1
    };

    public static void Postfix(ActModel __instance)
    {
        var rooms = RoomsField?.GetValue(__instance) as RoomSet;
        if (rooms == null) return;

        // Config: remove base game shared events from legacy acts
        if (IsLegacyAct(__instance) && !ActsFromThePastConfig.AllowNonLegacySharedEventsInLegacyActs)
        {
            rooms.events.RemoveAll(e => IsBaseGameSharedEvent(e));
        }

        // Config: remove mod shared events from non-legacy acts
        if (!IsLegacyAct(__instance) && !ActsFromThePastConfig.AllowLegacySharedEventsInNonLegacyActs)
        {
            rooms.events.RemoveAll(e => IsModSharedEvent(e));
        }

        // Act number restrictions (e.g. DesignerInSpire only in acts 2 and 3)
        int actNumber = GetActNumber(__instance);
        if (actNumber >= 0)
        {
            rooms.events.RemoveAll(e =>
                e is IActRestricted restricted &&
                !restricted.AllowedActIndices.Contains(actNumber));
        }
    }
}