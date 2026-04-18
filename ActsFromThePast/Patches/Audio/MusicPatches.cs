using System.Reflection;
using ActsFromThePast.Acts;
using ActsFromThePast.Acts.Exordium.Events;
using ActsFromThePast.Acts.TheBeyond;
using ActsFromThePast.Acts.TheBeyond.Encounters;
using ActsFromThePast.Acts.TheBeyond.Events;
using ActsFromThePast.Acts.TheCity;
using ActsFromThePast.Acts.TheCity.Events;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Patches.Audio;

public class MusicPatches
{
    [HarmonyPatch(typeof(NRunMusicController))]
    public static class LegacyActMusicPatches
    {
        private static readonly PropertyInfo StateProperty =
            typeof(RunManager).GetProperty("State", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo CombatStateField =
            typeof(CombatManager).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);

        private enum TrackType
        {
            None,
            Exploration,
            Elite,
            Boss
        }

        private enum LegacyAct
        {
            None,
            Exordium,
            City,
            Beyond
        }
        
        public static void SetBossStingerState()
        {
            _playingBossStinger = true;
            _isPlayingLegacyMusic = true;
            _currentTrackType = TrackType.Boss;
        }

        private static TrackType _currentTrackType = TrackType.None;
        public static bool _isPlayingLegacyMusic = false;
        public static bool _isPlayingBaseGameMusic = false;
        public static bool _hexaghostActivated = false;
        public static bool _playingBossStinger = false;

        // Exordium tracks
        private static readonly string[] ExordiumTracks =
        {
            "exordium_theme_1",
            "exordium_theme_2"
        };
        private static readonly string[] ExordiumEliteTracks =
        {
            "exordium_elite"
        };
        private static readonly string[] ExordiumBossTracks =
        {
            "exordium_boss"
        };

        // City tracks
        private static readonly string[] CityTracks =
        {
            "city_theme_1",
            "city_theme_2"
        };
        private static readonly string[] CityEliteTracks =
        {
            "exordium_elite"
        };
        private static readonly string[] CityBossTracks =
        {
            "city_boss"
        };

        // Beyond tracks
        private static readonly string[] BeyondTracks =
        {
            "beyond_theme_1",
            "beyond_theme_2"
        };
        private static readonly string[] BeyondEliteTracks =
        {
            "mind_bloom"
        };
        private static readonly string[] BeyondBossTracks =
        {
            "beyond_boss"
        };

        private const string BaseGameBankPath = "res://banks/desktop/act1_a1.bank";
        private const string BaseGameTrack = "event:/music/act1_a1_v1";

        private static LegacyAct GetCurrentLegacyAct()
        {
            var runState = StateProperty?.GetValue(RunManager.Instance) as RunState;
            return runState?.Act switch
            {
                ExordiumAct => LegacyAct.Exordium,
                TheCityAct => LegacyAct.City,
                TheBeyondAct => LegacyAct.Beyond,
                _ => LegacyAct.None
            };
        }

        private static bool IsLegacyAct() => GetCurrentLegacyAct() != LegacyAct.None;

        private static string[] GetExplorationTracks() => GetCurrentLegacyAct() switch
        {
            LegacyAct.Exordium => ExordiumTracks,
            LegacyAct.City => CityTracks,
            LegacyAct.Beyond => BeyondTracks,
            _ => ExordiumTracks
        };

        private static string[] GetEliteTracks() => GetCurrentLegacyAct() switch
        {
            LegacyAct.Exordium => ExordiumEliteTracks,
            LegacyAct.City => CityEliteTracks,
            LegacyAct.Beyond => BeyondEliteTracks,
            _ => ExordiumEliteTracks
        };

        private static string[] GetBossTracks() => GetCurrentLegacyAct() switch
        {
            LegacyAct.Exordium => ExordiumBossTracks,
            LegacyAct.City => CityBossTracks,
            LegacyAct.Beyond => BeyondBossTracks,
            _ => ExordiumBossTracks
        };

        private static string GetAmbienceTrack() => GetCurrentLegacyAct() switch
        {
            LegacyAct.Exordium => "exordium_ambience",
            LegacyAct.City => "city_ambience",
            LegacyAct.Beyond => "beyond_ambience",
            _ => "exordium_ambience"
        };

        private static Node? GetProxy(NRunMusicController controller)
        {
            return (Node?)controller.Get("_proxy");
        }

        private static void StartBaseGameMusic(NRunMusicController controller, int progress, float fadeDelay = 1f)
        {
            var proxy = GetProxy(controller);
            if (proxy == null) return;

            var tree = Engine.GetMainLoop() as SceneTree;
            tree?.CreateTimer(fadeDelay).Connect("timeout", Callable.From(() =>
            {
                proxy.Call("load_act_banks", new Godot.Collections.Array { BaseGameBankPath });
                proxy.Call("update_music", BaseGameTrack);
                proxy.Call("update_global_parameter", "Progress", progress);
                _isPlayingBaseGameMusic = true;
            }));
        }

        private static void StopBaseGameMusic(NRunMusicController controller)
        {
            if (!_isPlayingBaseGameMusic) return;

            var proxy = GetProxy(controller);
            if (proxy == null) return;

            proxy.Call("stop_music");
            proxy.Call("unload_act_banks");
            _isPlayingBaseGameMusic = false;
        }

        private static bool IsLagavulinEncounter()
        {
            var runState = StateProperty?.GetValue(RunManager.Instance) as RunState;
            var room = runState?.CurrentRoom as CombatRoom;
            return room?.Encounter is LagavulinElite;
        }

        private static bool IsLagavulinAsleep()
        {
            var combatManager = CombatManager.Instance;
            if (combatManager == null) return false;
            var combatState = CombatStateField?.GetValue(combatManager) as CombatState;
            if (combatState == null) return false;
            foreach (var creature in combatState.Enemies ?? Enumerable.Empty<Creature>())
            {
                if (creature.Monster is Lagavulin lagavulin)
                    return !lagavulin.IsAwake;
            }
            return false;
        }
        
        private static bool IsMindBloomEncounter()
        {
            var runState = StateProperty?.GetValue(RunManager.Instance) as RunState;
            var room = runState?.CurrentRoom as CombatRoom;
            return room?.Encounter is MindBloomGuardian or MindBloomHexaghost or MindBloomSlimeBoss;
        }

        private static bool IsDeadAdventurerCombat() => DeadAdventurer.CombatActive;
        private static bool IsMindBloomCombat() => MindBloom.CombatActive;
        private static bool IsMaskedBanditsCombat() => MaskedBandits.CombatActive;

        private static bool IsDeadAdventurerCombatEnded()
        {
            if (!DeadAdventurer.CombatActive) return false;
            return !(CombatManager.Instance?.IsInProgress ?? false);
        }
        
        private static bool IsMindBloomCombatEnded()
        {
            if (!MindBloom.CombatActive) return false;
            return !(CombatManager.Instance?.IsInProgress ?? false);
        }
        
        private static bool IsMaskedBanditsCombatEnded()
        {
            if (!MaskedBandits.CombatActive) return false;
            return !(CombatManager.Instance?.IsInProgress ?? false);
        }

        public static void OnHexaghostActivated()
        {
            _hexaghostActivated = true;
            ModAudio.FadeIn(ExordiumBossTracks, 1f);
            _isPlayingLegacyMusic = true;
            _currentTrackType = TrackType.Boss;
        }

        public static void ResetHexaghostState()
        {
            _hexaghostActivated = false;
        }

        private static bool IsHexaghostEncounter()
        {
            var runState = StateProperty?.GetValue(RunManager.Instance) as RunState;
            var room = runState?.CurrentRoom as CombatRoom;
            return room?.Encounter is HexaghostBoss;
        }

        private static int? GetSpecialRoomProgress()
        {
            var runState = StateProperty?.GetValue(RunManager.Instance) as RunState;
            return runState?.CurrentRoom?.RoomType switch
            {
                RoomType.Shop => 2,
                RoomType.RestSite => 3,
                _ => null
            };
        }

        private static bool IsBossRoom()
        {
            var runState = StateProperty?.GetValue(RunManager.Instance) as RunState;
            return runState?.CurrentRoom?.RoomType == RoomType.Boss;
        }

        private static bool IsEliteRoom()
        {
            var runState = StateProperty?.GetValue(RunManager.Instance) as RunState;
            return runState?.CurrentRoom?.RoomType == RoomType.Elite;
        }

[HarmonyPatch(nameof(NRunMusicController.UpdateMusic))]
[HarmonyPrefix]
public static bool UpdateMusic_Prefix(NRunMusicController __instance)
{
    if (!IsLegacyAct())
    {
        if (_isPlayingLegacyMusic)
        {
            ModAudio.StopMusic();
            ModAudio.StopAmbience();
            _isPlayingLegacyMusic = false;
            _currentTrackType = TrackType.None;
            _playingBossStinger = false;
        }
        return true;
    }
    _playingBossStinger = false;
    ResetHexaghostState();
    __instance.StopMusic();
    ModAudio.FadeIn(GetExplorationTracks(), 1.0f);
    _isPlayingLegacyMusic = true;
    _currentTrackType = TrackType.Exploration;
    __instance.UpdateAmbience();
    return false;
}

[HarmonyPatch(nameof(NRunMusicController.UpdateTrack), new Type[0])]
[HarmonyPrefix]
public static bool UpdateTrack_Prefix(NRunMusicController __instance)
{
    if (!IsLegacyAct())
    {
        if (_isPlayingLegacyMusic)
        {
            ModAudio.StopMusic();
            ModAudio.StopAmbience();
            _isPlayingLegacyMusic = false;
            _currentTrackType = TrackType.None;
            _playingBossStinger = false;
        }
        return true;
    }

    var combatManager = CombatManager.Instance;
    var combatInProgress2 = combatManager?.IsInProgress ?? false;

    // Handle Shop / Rest Site (must be before stinger guard so stinger fades on room transition)
    var specialProgress = GetSpecialRoomProgress();
    if (specialProgress.HasValue)
    {
        if (_playingBossStinger || _isPlayingLegacyMusic)
        {
            _playingBossStinger = false;
            ModAudio.FadeOut(1f);
            _isPlayingLegacyMusic = false;
            _currentTrackType = TrackType.None;
            StartBaseGameMusic(__instance, specialProgress.Value, 1f);
        }
        else if (_isPlayingBaseGameMusic)
        {
            var proxy = GetProxy(__instance);
            proxy?.Call("update_global_parameter", "Progress", specialProgress.Value);
        }
        return false;
    }

    if (_playingBossStinger && (IsBossRoom() || IsMindBloomEncounter()))
        return false;

    // Handle Mind Bloom event combat (must be before Boss check since RoomType reports Boss during combat)
    if (IsMindBloomEncounter())
    {
        if (!combatInProgress2)
        {
            StopBaseGameMusic(__instance);
            ModAudio.StopMusic();
            ModAudio.StopAmbience();
            LegacyBossHelper.OnBossVictory();
            return false;
        }
        if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Elite)
        {
            StopBaseGameMusic(__instance);
            ModAudio.FadeIn(new[] { "mind_bloom" }, 1f);
            _isPlayingLegacyMusic = true;
            _currentTrackType = TrackType.Elite;
        }
        return false;
    }

    // Handle Boss rooms
    if (IsBossRoom() && combatInProgress2)
    {
        if (GetCurrentLegacyAct() == LegacyAct.Exordium && IsHexaghostEncounter() && !_hexaghostActivated)
        {
            if (_isPlayingLegacyMusic)
            {
                ModAudio.FadeOut(0.5f);
                _isPlayingLegacyMusic = false;
                _currentTrackType = TrackType.None;
            }
            StopBaseGameMusic(__instance);
            return false;
        }
        if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Boss)
        {
            StopBaseGameMusic(__instance);
            ModAudio.FadeIn(GetBossTracks(), 1f);
            _isPlayingLegacyMusic = true;
            _currentTrackType = TrackType.Boss;
        }
        return false;
    }

    // Handle Boss rewards screen (combat ended but still in boss room)
    if (IsBossRoom() && !combatInProgress2)
    {
        StopBaseGameMusic(__instance);
        ModAudio.PlayBossStinger();
        SetBossStingerState();
        return false;
    }

    // Handle Elite rooms
    if (IsEliteRoom() && combatInProgress2)
    {
        if (GetCurrentLegacyAct() == LegacyAct.Exordium && IsLagavulinEncounter() && IsLagavulinAsleep())
        {
            if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Exploration)
            {
                StopBaseGameMusic(__instance);
                ModAudio.FadeIn(GetExplorationTracks(), 1f);
                _isPlayingLegacyMusic = true;
                _currentTrackType = TrackType.Exploration;
            }
            return false;
        }
        if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Elite)
        {
            StopBaseGameMusic(__instance);
            ModAudio.FadeIn(GetEliteTracks(), 1f);
            _isPlayingLegacyMusic = true;
            _currentTrackType = TrackType.Elite;
        }
        return false;
    }

    // Handle Dead Adventurer event combat
    if (IsDeadAdventurerCombat())
    {
        if (IsDeadAdventurerCombatEnded())
        {
            if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Exploration)
            {
                StopBaseGameMusic(__instance);
                ModAudio.FadeIn(GetExplorationTracks(), 1f);
                _isPlayingLegacyMusic = true;
                _currentTrackType = TrackType.Exploration;
            }
            return false;
        }
        if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Elite)
        {
            StopBaseGameMusic(__instance);
            ModAudio.FadeIn(GetEliteTracks(), 1f);
            _isPlayingLegacyMusic = true;
            _currentTrackType = TrackType.Elite;
        }
        return false;
    }

    // Handle Masked Bandits event combat
    if (IsMaskedBanditsCombat())
    {
        if (IsMaskedBanditsCombatEnded())
        {
            if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Exploration)
            {
                StopBaseGameMusic(__instance);
                ModAudio.FadeIn(GetExplorationTracks(), 1f);
                _isPlayingLegacyMusic = true;
                _currentTrackType = TrackType.Exploration;
            }
            return false;
        }
        if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Elite)
        {
            StopBaseGameMusic(__instance);
            ModAudio.FadeIn(GetEliteTracks(), 1f);
            _isPlayingLegacyMusic = true;
            _currentTrackType = TrackType.Elite;
        }
        return false;
    }

    // Default: exploration music
    if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Exploration)
    {
        StopBaseGameMusic(__instance);
        ModAudio.FadeIn(GetExplorationTracks(), 1f);
        _isPlayingLegacyMusic = true;
        _currentTrackType = TrackType.Exploration;
    }
    return false;
}

        [HarmonyPatch(nameof(NRunMusicController.ToggleMerchantTrack))]
        [HarmonyPrefix]
        public static bool ToggleMerchantTrack_Prefix(NRunMusicController __instance)
        {
            if (!IsLegacyAct()) return true;
            if (_isPlayingLegacyMusic) return false;

            var proxy = GetProxy(__instance);
            if (proxy == null) return false;

            var mapVisible = NMapScreen.Instance?.IsVisible() ?? false;
            proxy.Call("update_global_parameter", "Progress", mapVisible ? 9 : 2);
            return false;
        }

        [HarmonyPatch(nameof(NRunMusicController.TriggerEliteSecondPhase))]
        [HarmonyPrefix]
        public static bool TriggerEliteSecondPhase_Prefix(NRunMusicController __instance)
        {
            if (!IsLegacyAct()) return true;
            if (_isPlayingLegacyMusic) return false;

            var proxy = GetProxy(__instance);
            if (proxy == null) return false;

            proxy.Call("update_global_parameter", "Progress", 8);
            return false;
        }

        [HarmonyPatch(nameof(NRunMusicController.TriggerCampfireGoingOut))]
        [HarmonyPrefix]
        public static bool TriggerCampfireGoingOut_Prefix() => !IsLegacyAct();

        [HarmonyPatch(nameof(NRunMusicController.StopMusic))]
        [HarmonyPostfix]
        public static void StopMusic_Postfix(NRunMusicController __instance)
        {
            ModAudio.StopMusic();
            ModAudio.StopAmbience();
            StopBaseGameMusic(__instance);
            _isPlayingLegacyMusic = false;
            _currentTrackType = TrackType.None;
        }

        [HarmonyPatch(nameof(NRunMusicController.UpdateAmbience))]
        [HarmonyPrefix]
        public static bool UpdateAmbience_Prefix()
        {
            if (!IsLegacyAct())
            {
                if (_isPlayingLegacyMusic)
                {
                    ModAudio.StopAmbience();
                }
                return true;
            }

            var combatInProgress = CombatManager.Instance?.IsInProgress ?? false;
            if (IsMindBloomEncounter() && !combatInProgress && !_playingBossStinger)
            {
                ModAudio.StopMusic();
                ModAudio.StopAmbience();
                ModAudio.PlayBossStinger(1.5f);
                SetBossStingerState();
                return false;
            }
            if (IsBossRoom() && !combatInProgress && !_playingBossStinger)
            {
                ModAudio.StopMusic();
                ModAudio.StopAmbience();
                ModAudio.PlayBossStinger(1.5f);
                SetBossStingerState();
                return false;
            }

            ModAudio.FadeInAmbience(GetAmbienceTrack(), 1f);
            return false;
        }
        
        [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
        public static class BeforeCombatStartStingerPatch
        {
            public static void Prefix()
            {
                if (_playingBossStinger)
                {
                    _playingBossStinger = false;
                    _isPlayingLegacyMusic = false;
                    _currentTrackType = TrackType.None;
                }
            }
        }
    }

    [HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.SetBgmVol))]
    public static class SetBgmVolPatch
    {
        public static void Postfix(float volume)
        {
            ModAudio.SetMusicVolume(volume);
        }
    }

    [HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.SetAmbienceVol))]
    public static class SetAmbienceVolPatch
    {
        public static void Postfix(float volume)
        {
            ModAudio.SetAmbienceVolume(volume);
        }
    }
    
    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
    public static class AfterCombatEndPatch
    {
        public static void Postfix(IRunState runState, CombatState? combatState, CombatRoom room)
        {
            
            // Putting Mind Bloom logic here since I don't want to patch the same hook twice in multiple classes
            
            if (combatState?.Encounter is MindBloomGuardian or MindBloomHexaghost or MindBloomSlimeBoss)
                MindBloom.CombatActive = false;

            if (combatState?.Encounter is SlimeBossBoss or CollectorBoss or HexaghostBoss
                or GuardianBoss or ChampBoss or BronzeAutomatonBoss or TimeEaterBoss
                or AwakenedOneBoss or DonuAndDecaBoss)
            {
                LegacyBossHelper.OnBossVictory();
            }
        }
    }
}