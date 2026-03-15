


using ActsFromThePast.Cards;
using ActsFromThePast.Patches;
using ActsFromThePast.Patches.Acts;
using ActsFromThePast.Patches.Audio;
using ActsFromThePast.Patches.Cards;
using ActsFromThePast.Patches.Creatures;
using ActsFromThePast.Patches.Powers;

using ActsFromThePast.Patches.RoomEvents;
using ActsFromThePast.Relics;
using BaseLib.Config;
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
        
        ModHelper.AddModelToPool<CurseCardPool, Parasite>();
        ModHelper.AddModelToPool<CurseCardPool, Necronomicurse>();
        ModHelper.AddModelToPool<EventCardPool, Jax>();
        ModHelper.AddModelToPool<EventCardPool, Madness>();
        ModHelper.AddModelToPool<EventCardPool, Bite>();
        ModHelper.AddModelToPool<EventCardPool, RitualDagger>();
        
        ModHelper.AddModelToPool<EventRelicPool, OddMushroom>();
        ModHelper.AddModelToPool<EventRelicPool, MarkOfTheBloom>();
        ModHelper.AddModelToPool<EventRelicPool, MutagenicStrength>();
        ModHelper.AddModelToPool<EventRelicPool, Enchiridion>();
        ModHelper.AddModelToPool<EventRelicPool, Necronomicon>();
        ModHelper.AddModelToPool<EventRelicPool, NilrysCodex>();
        ModHelper.AddModelToPool<EventRelicPool, NlothsGift>();
        ModHelper.AddModelToPool<EventRelicPool, BloodyIdol>();
        ModHelper.AddModelToPool<EventRelicPool, GoldenIdolOriginal>();
        
        ModConfigRegistry.Register("ActsFromThePast" ,new ActsFromThePastConfig());
    }
    

}
