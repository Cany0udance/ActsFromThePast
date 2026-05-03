using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace ActsFromThePast.Acts;

public sealed class ExordiumAct : CustomActModel
{
    public ExordiumAct() : base(actNumber: 1) { }

    public override IEnumerable<EncounterModel> GenerateAllEncounters()
    {
        return new EncounterModel[]
        {
        };
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

    // Colors differ from CustomActModel defaults (which are act 3 themed)
    public override Color MapTraveledColor => new Color("28231D");
    public override Color MapUntraveledColor => new Color("877256");
    public override Color MapBgColor => new Color("A78A67");

    // Original had these empty; CustomActModel defaults to act 3 music
    public override string[] BgMusicOptions => Array.Empty<string>();
    public override string[] MusicBankPaths => Array.Empty<string>();
    public override string AmbientSfx => "";
    
    public override string ChestSpineResourcePath => "res://animations/backgrounds/treasure_room/chest_room_act_1_skel_data.tres";
    public override string ChestSpineSkinNameNormal => "act1";
    public override string ChestSpineSkinNameStroke => "act1_stroke";
    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act1";
    
    protected override string CustomMapTopBgPath => "res://images/packed/map/map_bgs/exordium_act/map_top_exordium_act.png";
    protected override string CustomMapMidBgPath => "res://images/packed/map/map_bgs/exordium_act/map_middle_exordium_act.png";
    protected override string CustomMapBotBgPath => "res://images/packed/map/map_bgs/exordium_act/map_middle_exordium_act.png";
    protected override string CustomRestSiteBackgroundPath => "res://scenes/rest_site/overgrowth_rest_site.tscn";
}