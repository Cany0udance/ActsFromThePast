using ActsFromThePast.Acts.TheBeyond.Encounters;
using ActsFromThePast.Acts.TheBeyond.Encounters.Elite;
using ActsFromThePast.Acts.TheBeyond.Events;
using ActsFromThePast.Acts.TheCity;
using ActsFromThePast.Acts.TheCity.Events;
using Godot;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;

namespace ActsFromThePast.Acts.TheBeyond;

public sealed class TheBeyondAct : ActModel
{
    public override IEnumerable<EncounterModel> GenerateAllEncounters()
    {
        return new EncounterModel[]
        {

        };
    }
    
    public override IEnumerable<EncounterModel> BossDiscoveryOrder
    {
        get
        {
            return new EncounterModel[]
            {
            };
        }
    }
    
    public override IEnumerable<AncientEventModel> AllAncients
    {
        get
        {
            return new AncientEventModel[]
            {
                ModelDb.AncientEvent<Nonupeipe>(),
                ModelDb.AncientEvent<Vakuu>(),
                ModelDb.AncientEvent<Tanx>()
            };
        }
    }
    
    public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState unlockState)
    {
        return AllAncients;
    }
    
    public override bool Equals(object? obj) => obj is TheBeyondAct;
    public override int GetHashCode() => typeof(TheBeyondAct).GetHashCode();
    
    public override IEnumerable<EventModel> AllEvents
    {
        get
        {
            return new EventModel[]
            {
                ModelDb.Event<TrashHeap>()
            };
        }
    }
    
    protected override void ApplyActDiscoveryOrderModifications(UnlockState unlockState)
    {
    }
    
    protected override int NumberOfWeakEncounters => 2;
    protected override int BaseNumberOfRooms => 13;
    
    public override string[] BgMusicOptions => Array.Empty<string>();
    public override string[] MusicBankPaths => Array.Empty<string>();
    public override string AmbientSfx => "";
    
    public override string ChestSpineResourcePath
    {
        get => "res://animations/backgrounds/treasure_room/chest_room_act_3_skel_data.tres";
    }

    public override string ChestSpineSkinNameNormal => "act3";

    public override string ChestSpineSkinNameStroke => "act3_stroke";
    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act2";
    public override Color MapTraveledColor => new Color("1D1E2F");

    public override Color MapUntraveledColor => new Color("60717C");

    public override Color MapBgColor => new Color("819A97");
    
    public override MapPointTypeCounts GetMapPointTypes(Rng mapRng)
    {
        int restCount = mapRng.NextInt(5, 7);
        return new MapPointTypeCounts(MapPointTypeCounts.StandardRandomUnknownCount(mapRng) - 1, restCount);
    }
}