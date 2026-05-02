using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace ActsFromThePast.Acts.TheBeyond;

public sealed class TheBeyondAct : CustomActModel
{
    public TheBeyondAct() : base(actNumber: 3) { }

    public override IEnumerable<EncounterModel> GenerateAllEncounters()
    {
        return new EncounterModel[]
        {
        };
    }

    public override bool Equals(object? obj) => obj is TheBeyondAct;
    public override int GetHashCode() => typeof(TheBeyondAct).GetHashCode();

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

    // Chest open SFX says act2 — intentional?
    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act2";

    public override Color MapTraveledColor => new Color("1D1E2F");
    public override Color MapUntraveledColor => new Color("60717C");
    public override Color MapBgColor => new Color("819A97");

    protected override string CustomMapTopBgPath => "res://images/packed/map/map_bgs/the_beyond_act/map_top_the_beyond_act.png";
    protected override string CustomMapMidBgPath => "res://images/packed/map/map_bgs/the_beyond_act/map_middle_the_beyond_act.png";
    protected override string CustomMapBotBgPath => "res://images/packed/map/map_bgs/the_beyond_act/map_middle_the_beyond_act.png";
    protected override string CustomRestSiteBackgroundPath => "res://scenes/rest_site/the_beyond_act_rest_site.tscn";
}