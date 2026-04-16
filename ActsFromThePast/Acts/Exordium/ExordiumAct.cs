using ActsFromThePast.Acts.Exordium.Events;
using Godot;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;

namespace ActsFromThePast.Acts;

public sealed class ExordiumAct : ActModel
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
                ModelDb.AncientEvent<Neow>()
            };
        }
    }

    public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState unlockState)
    {
        return AllAncients;
    }
    
    public override bool Equals(object? obj) => obj is ExordiumAct;
    public override int GetHashCode() => typeof(ExordiumAct).GetHashCode();

    public override IEnumerable<EventModel> AllEvents
    {
        get
        {
            return new EventModel[]
            {
                ModelDb.Event<TrashHeap>(),
            };
        }
    }

    protected override void ApplyActDiscoveryOrderModifications(UnlockState unlockState)
    {
        // No modifications needed
    }

    protected override int NumberOfWeakEncounters => 3;
    protected override int BaseNumberOfRooms => 15;

    public override string[] BgMusicOptions => Array.Empty<string>();
    public override string[] MusicBankPaths => Array.Empty<string>();
    public override string AmbientSfx => "";

    // Reuse Underdocks chest assets
    public override string ChestSpineResourcePath => "res://animations/backgrounds/treasure_room/chest_room_act_1_skel_data.tres";
    public override string ChestSpineSkinNameNormal => "act1";
    public override string ChestSpineSkinNameStroke => "act1_stroke";
    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act1";
    
    public override Color MapTraveledColor => new Color("28231D");

    public override Color MapUntraveledColor => new Color("877256");

    public override Color MapBgColor => new Color("A78A67");
    public override MapPointTypeCounts GetMapPointTypes(Rng mapRng)
    {
        int restCount = mapRng.NextGaussianInt(7, 1, 6, 7);
        return new MapPointTypeCounts(MapPointTypeCounts.StandardRandomUnknownCount(mapRng), restCount);
    }
}