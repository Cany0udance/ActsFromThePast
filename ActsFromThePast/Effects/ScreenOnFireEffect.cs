namespace ActsFromThePast;

public partial class ScreenOnFireEffect : NSts1Effect
{
    private const float EffectDuration = 3f;
    private const float SpawnInterval = 0.05f;

    private float _spawnTimer;
    private bool _playedInitialEffects;

    public static ScreenOnFireEffect Create()
    {
        var effect = new ScreenOnFireEffect();
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = EffectDuration;
        StartingDuration = EffectDuration;
        _spawnTimer = 0f;
        _playedInitialEffects = false;
    }

    protected override void Update(float delta)
    {
        if (!_playedInitialEffects)
        {
            _playedInitialEffects = true;
            ModAudio.Play("hexaghost", "ghost_flames");
            BorderFlashEffect.PlayFire();
        }

        Duration -= delta;
        _spawnTimer -= delta;

        if (_spawnTimer < 0f)
        {
            _spawnTimer = SpawnInterval;
            
            var parent = GetParent();
            if (parent != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    var fire = GiantFireEffect.Create();
                    parent.AddChild(fire);
                }
            }
        }

        if (Duration < 0f)
        {
            IsDone = true;
        }
    }
}