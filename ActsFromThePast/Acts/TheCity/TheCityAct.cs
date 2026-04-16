using ActsFromThePast.Acts.Exordium.Events;
using ActsFromThePast.Acts.TheBeyond.Events;
using ActsFromThePast.Acts.TheCity.Events;
using Godot;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;

namespace ActsFromThePast.Acts.TheCity;

public sealed class TheCityAct : ActModel
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
                ModelDb.AncientEvent<Orobas>(),
                ModelDb.AncientEvent<Pael>(),
                ModelDb.AncientEvent<Tezcatara>()
            };
        }
    }
    
    public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState unlockState)
    {
        return AllAncients;
    }
    
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
    }
    
    protected override int NumberOfWeakEncounters => 2;
    protected override int BaseNumberOfRooms => 14;
    
    public override string[] BgMusicOptions => Array.Empty<string>();
    public override string[] MusicBankPaths => Array.Empty<string>();
    public override string AmbientSfx => ""; // or keep the base game's ambient if you want
    
    public override string ChestSpineResourcePath
    {
        get => "res://animations/backgrounds/treasure_room/chest_room_act_2_skel_data.tres";
    }

    public override string ChestSpineSkinNameNormal => "act2";

    public override string ChestSpineSkinNameStroke => "act2_stroke";
    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act2";
    public override Color MapTraveledColor => new Color("27221C");

    public override Color MapUntraveledColor => new Color("6E7750");

    public override Color MapBgColor => new Color("9B9562");
    
    public override MapPointTypeCounts GetMapPointTypes(Rng mapRng)
    {
        int restCount = mapRng.NextGaussianInt(6, 1, 6, 7);
        return new MapPointTypeCounts(MapPointTypeCounts.StandardRandomUnknownCount(mapRng) - 1, restCount);
    }
}