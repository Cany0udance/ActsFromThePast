


using ActsFromThePast.Cards;
using ActsFromThePast.Patches;
using ActsFromThePast.Patches.Acts;
using ActsFromThePast.Patches.Audio;
using ActsFromThePast.Patches.Cards;
using ActsFromThePast.Patches.Creatures;
using ActsFromThePast.Patches.Powers;

using ActsFromThePast.Patches.RoomEvents;
using ActsFromThePast.Relics;
using BaseLib;
using BaseLib.Config;
using BaseLib.Patches.Content;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ActsFromThePast;
[ModInitializer("Initialize")]
public class ActsFromThePastInitializer
{
    
    public static void Initialize()
    {
        var harmony = new Harmony("actsfromthepast.actsfromthepast");
        harmony.PatchAll(typeof(ActsFromThePastInitializer).Assembly);
        
        ModConfigRegistry.Register("ActsFromThePast" ,new ActsFromThePastConfig());
    }
    

}
