using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;

namespace ActsFromThePast.Acts.TheCity;

public sealed class TheCityAct : CustomActModel
{
    public TheCityAct() : base(actNumber: 2) { }

    public override IEnumerable<EncounterModel> GenerateAllEncounters()
    {
        return new EncounterModel[]
        {
        };
    }

    public override bool Equals(object? obj) => obj is TheCityAct;
    public override int GetHashCode() => typeof(TheCityAct).GetHashCode();

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

    public override string[] BgMusicOptions => Array.Empty<string>();
    public override string[] MusicBankPaths => Array.Empty<string>();
    public override string AmbientSfx => "";

    // Act 2 chest assets
    public override string ChestSpineResourcePath => "res://animations/backgrounds/treasure_room/chest_room_act_2_skel_data.tres";
    public override string ChestSpineSkinNameNormal => "act2";
    public override string ChestSpineSkinNameStroke => "act2_stroke";
    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act2";

    // These happen to match CustomActModel's defaults, but keeping them explicit doesn't hurt
    public override Color MapTraveledColor => new Color("27221C");
    public override Color MapUntraveledColor => new Color("6E7750");
    public override Color MapBgColor => new Color("9B9562");

    protected override string CustomMapTopBgPath => "res://images/packed/map/map_bgs/the_city_act/map_top_the_city_act.png";
    protected override string CustomMapMidBgPath => "res://images/packed/map/map_bgs/the_city_act/map_middle_the_city_act.png";
    protected override string CustomMapBotBgPath => "res://images/packed/map/map_bgs/the_city_act/map_middle_the_city_act.png";
    protected override string CustomRestSiteBackgroundPath => "res://scenes/rest_site/the_city_act_rest_site.tscn";
}