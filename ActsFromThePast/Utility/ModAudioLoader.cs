using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves;

namespace ActsFromThePast;

public static class ModAudio
{
    private static readonly Dictionary<string, AudioStream> CachedStreams = new();
    private static AudioStreamPlayer? _musicPlayer;
    private static string? _currentMusicPath;
    private static float _currentVolumeOffset = 0f;
    private static Tween? _fadeTween;
    private static AudioStreamPlayer? _outgoingPlayer;
    private static Tween? _outgoingFadeTween;
    private static AudioStreamPlayer? _ambiencePlayer;
    private static string? _currentAmbiencePath;
    private static Tween? _ambienceFadeTween;
    private const float MusicVolumeOffset = -6f;
    private const float AmbienceVolumeOffset = -6f;
    private const float SfxVolumeOffset = -3f;
    
    private static readonly string[] BossStingers =
    {
        "boss_victory_stinger_1",
        "boss_victory_stinger_2",
        "boss_victory_stinger_3",
        "boss_victory_stinger_4"
    };
    
    public static void Play(string folder, string soundName, float volume = 0f, float pitchVariation = 0f, float basePitch = 1f)
    {
        var stream = GetOrLoadStream(folder, soundName);
        if (stream == null) return;
        var player = new AudioStreamPlayer();
        player.Stream = stream;
        player.VolumeDb = volume + SfxVolumeOffset;
        player.Bus = "SFX";

        if (pitchVariation > 0f)
        {
            player.PitchScale = basePitch + (float)Rng.Chaotic.NextDouble() * 2f * pitchVariation - pitchVariation;
        }
        else
        {
            player.PitchScale = basePitch;
        }

        var combatRoom = NCombatRoom.Instance;
        if (combatRoom != null)
        {
            combatRoom.AddChild(player);
            player.Play();
            player.Finished += () => player.QueueFree();
        }
    }
    
    public static void Play(Creature creature, string folder, string soundName, float volume = 0f)
    {
        Play(folder, soundName, volume);
    }
    
    private static AudioStream? GetOrLoadStream(string folder, string soundName)
    {
        var key = $"{folder}/{soundName}";
        if (CachedStreams.TryGetValue(key, out var cached))
            return cached;
        var path = $"res://ActsFromThePast/sfx/{folder}/{soundName}.ogg";
        var stream = GD.Load<AudioStream>(path);
        if (stream != null)
        {
            CachedStreams[key] = stream;
        }
    
        return stream;
    }
    
    public static void PlayMusic(string[] musicOptions, float volumeDbOffset = 0f)
    {
        if (musicOptions == null || musicOptions.Length == 0)
            return;

        var musicName = musicOptions[GD.RandRange(0, musicOptions.Length - 1)];
        var path = $"res://ActsFromThePast/bgm/{musicName}.ogg";

        if (_currentMusicPath == path && _musicPlayer?.Playing == true)
            return;

        StopMusic();

        var stream = GD.Load<AudioStream>(path);
        if (stream == null)
        {
            return;
        }

        if (stream is AudioStreamOggVorbis ogg)
            ogg.Loop = true;

        _musicPlayer = new AudioStreamPlayer();
        _musicPlayer.Stream = stream;
        _musicPlayer.Bus = "Master";
    
        _currentVolumeOffset = volumeDbOffset;
        var bgmVolume = SaveManager.Instance.SettingsSave.VolumeBgm;
        _musicPlayer.VolumeDb = Mathf.LinearToDb(Mathf.Pow(bgmVolume, 2f)) + _currentVolumeOffset + MusicVolumeOffset;

        var runNode = NRun.Instance;
        if (runNode != null)
        {
            runNode.AddChild(_musicPlayer);
            _musicPlayer.Play();
            _currentMusicPath = path;
        }
    }
    
    public static void SetMusicVolume(float volume)
    {
        if (_musicPlayer != null && GodotObject.IsInstanceValid(_musicPlayer))
        {
            _musicPlayer.VolumeDb = Mathf.LinearToDb(Mathf.Pow(volume, 2f)) + _currentVolumeOffset + MusicVolumeOffset;
        }
    }
    
public static void FadeIn(string[] musicOptions, float duration = 1.0f, float volumeDbOffset = 0f)
{
    if (musicOptions == null || musicOptions.Length == 0)
        return;
    
    var musicName = musicOptions[GD.RandRange(0, musicOptions.Length - 1)];
    var path = $"res://ActsFromThePast/bgm/{musicName}.ogg";
    
    if (_currentMusicPath == path && _musicPlayer?.Playing == true)
        return;
    
    // Move current player to outgoing slot for crossfade instead of immediate stop
    if (_musicPlayer != null && GodotObject.IsInstanceValid(_musicPlayer))
    {
        _outgoingFadeTween?.Kill();
        _outgoingPlayer?.QueueFree();
        
        _outgoingPlayer = _musicPlayer;
        _outgoingFadeTween = _outgoingPlayer.CreateTween();
        _outgoingFadeTween.TweenProperty(_outgoingPlayer, "volume_db", -80f, duration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
        _outgoingFadeTween.TweenCallback(Callable.From(() =>
        {
            _outgoingPlayer?.QueueFree();
            _outgoingPlayer = null;
        }));
    }
    
    _fadeTween?.Kill();
    _musicPlayer = null;
    _currentMusicPath = null;
    
    var stream = GD.Load<AudioStream>(path);
    if (stream == null)
    {
        return;
    }
    
    if (stream is AudioStreamOggVorbis ogg)
        ogg.Loop = true;
    
    _musicPlayer = new AudioStreamPlayer();
    _musicPlayer.Stream = stream;
    _musicPlayer.Bus = "Master";
    _musicPlayer.VolumeDb = -80f;
    
    _currentVolumeOffset = volumeDbOffset;
    
    var runNode = NRun.Instance;
    if (runNode != null)
    {
        runNode.AddChild(_musicPlayer);
        _musicPlayer.Play();
        _currentMusicPath = path;
        
        var targetDb = Mathf.LinearToDb(Mathf.Pow(SaveManager.Instance.SettingsSave.VolumeBgm, 2f)) + _currentVolumeOffset + MusicVolumeOffset;
        
        _fadeTween = _musicPlayer.CreateTween();
        _fadeTween.TweenProperty(_musicPlayer, "volume_db", targetDb, duration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        
    }
}

public static void FadeOut(float duration = 1.0f)
{
    if (_musicPlayer == null || !GodotObject.IsInstanceValid(_musicPlayer))
        return;
    
    _fadeTween?.Kill();
    _fadeTween = _musicPlayer.CreateTween();
    _fadeTween.TweenProperty(_musicPlayer, "volume_db", -80f, duration)
        .SetTrans(Tween.TransitionType.Sine)
        .SetEase(Tween.EaseType.In);
    _fadeTween.TweenCallback(Callable.From(() => StopMusicImmediate()));
}

private static void StopMusicImmediate()
{
    _fadeTween?.Kill();
    _fadeTween = null;
    _outgoingFadeTween?.Kill();
    _outgoingFadeTween = null;
    
    if (_musicPlayer != null && GodotObject.IsInstanceValid(_musicPlayer))
    {
        _musicPlayer.Stop();
        _musicPlayer.QueueFree();
    }
    _musicPlayer = null;
    _currentMusicPath = null;
    
    if (_outgoingPlayer != null && GodotObject.IsInstanceValid(_outgoingPlayer))
    {
        _outgoingPlayer.Stop();
        _outgoingPlayer.QueueFree();
    }
    _outgoingPlayer = null;
}

public static void StopMusic()
{
    StopMusicImmediate();
}
    
    public static bool IsPlayingLegacyMusic() => _musicPlayer?.Playing == true;
    
public static void PlayAmbience(string ambienceName, float volumeDbOffset = 0f)
{
    var path = $"res://ActsFromThePast/bgm/{ambienceName}.ogg";
    
    if (_currentAmbiencePath == path && _ambiencePlayer?.Playing == true)
        return;
    
    StopAmbience();
    
    var stream = GD.Load<AudioStream>(path);
    if (stream == null) return;
    
    if (stream is AudioStreamOggVorbis ogg)
        ogg.Loop = true;
    
    _ambiencePlayer = new AudioStreamPlayer();
    _ambiencePlayer.Stream = stream;
    _ambiencePlayer.Bus = "Master";
    
    var ambienceVolume = SaveManager.Instance.SettingsSave.VolumeAmbience;
    _ambiencePlayer.VolumeDb = Mathf.LinearToDb(Mathf.Pow(ambienceVolume, 2f)) + volumeDbOffset + AmbienceVolumeOffset;
    
    var runNode = NRun.Instance;
    if (runNode != null)
    {
        runNode.AddChild(_ambiencePlayer);
        _ambiencePlayer.Play();
        _currentAmbiencePath = path;
    }
}

public static void FadeInAmbience(string ambienceName, float duration = 1.0f, float volumeDbOffset = 0f)
{
    var path = $"res://ActsFromThePast/bgm/{ambienceName}.ogg";
    
    if (_currentAmbiencePath == path && _ambiencePlayer?.Playing == true)
        return;
    
    StopAmbience();
    
    var stream = GD.Load<AudioStream>(path);
    if (stream == null) return;
    
    if (stream is AudioStreamOggVorbis ogg)
        ogg.Loop = true;
    
    _ambiencePlayer = new AudioStreamPlayer();
    _ambiencePlayer.Stream = stream;
    _ambiencePlayer.Bus = "Master";
    _ambiencePlayer.VolumeDb = -80f;
    
    var runNode = NRun.Instance;
    if (runNode != null)
    {
        runNode.AddChild(_ambiencePlayer);
        _ambiencePlayer.Play();
        _currentAmbiencePath = path;
        
        var targetDb = Mathf.LinearToDb(Mathf.Pow(SaveManager.Instance.SettingsSave.VolumeAmbience, 2f)) + volumeDbOffset + AmbienceVolumeOffset;
        
        _ambienceFadeTween = _ambiencePlayer.CreateTween();
        _ambienceFadeTween.TweenProperty(_ambiencePlayer, "volume_db", targetDb, duration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }
}

public static void FadeOutAmbience(float duration = 1.0f)
{
    if (_ambiencePlayer == null || !GodotObject.IsInstanceValid(_ambiencePlayer))
        return;
    
    _ambienceFadeTween?.Kill();
    _ambienceFadeTween = _ambiencePlayer.CreateTween();
    _ambienceFadeTween.TweenProperty(_ambiencePlayer, "volume_db", -80f, duration)
        .SetTrans(Tween.TransitionType.Sine)
        .SetEase(Tween.EaseType.In);
    _ambienceFadeTween.TweenCallback(Callable.From(() => StopAmbience()));
}

public static void StopAmbience()
{
    _ambienceFadeTween?.Kill();
    _ambienceFadeTween = null;
    
    if (_ambiencePlayer != null && GodotObject.IsInstanceValid(_ambiencePlayer))
    {
        _ambiencePlayer.Stop();
        _ambiencePlayer.QueueFree();
    }
    _ambiencePlayer = null;
    _currentAmbiencePath = null;
}

public static void SetAmbienceVolume(float volume)
{
    if (_ambiencePlayer != null && GodotObject.IsInstanceValid(_ambiencePlayer))
    {
        _ambiencePlayer.VolumeDb = Mathf.LinearToDb(Mathf.Pow(volume, 2f)) + AmbienceVolumeOffset;
    }
}

public static void PlayBossStinger(float seekFrom = 0f)
{
    var stinger = BossStingers[GD.RandRange(0, BossStingers.Length - 1)];
    var path = $"res://ActsFromThePast/bgm/{stinger}.ogg";

    var stream = GD.Load<AudioStream>(path);
    if (stream == null) return;

    if (stream is AudioStreamOggVorbis ogg)
        ogg.Loop = false;

    _musicPlayer = new AudioStreamPlayer();
    _musicPlayer.Stream = stream;
    _musicPlayer.Bus = "Master";

    var bgmVolume = SaveManager.Instance.SettingsSave.VolumeBgm;
    _musicPlayer.VolumeDb = Mathf.LinearToDb(Mathf.Pow(bgmVolume, 2f)) + MusicVolumeOffset;

    var runNode = NRun.Instance;
    if (runNode != null)
    {
        runNode.AddChild(_musicPlayer);
        _musicPlayer.Play(seekFrom);
        _currentMusicPath = path;
    }
}
}