using Godot;

namespace ActsFromThePast;

public partial class FireballEffect : NSts1Effect
{
    private const float EffectDuration = 0.5f;
    private const float FireballInterval = 0.016f;

    private Vector2 _startPos;
    private Vector2 _targetPos;
    private float _vfxTimer;

    public static FireballEffect Create(Vector2 startPos, Vector2 targetPos)
    {
        var effect = new FireballEffect();
        effect._startPos = startPos;
        effect._targetPos = targetPos + new Vector2(
            (float)(Random.Shared.NextDouble() * 40.0 - 20.0),
            (float)(Random.Shared.NextDouble() * 40.0 - 20.0)
        );
        effect.Position = startPos;
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = EffectDuration;
        StartingDuration = EffectDuration;
        _vfxTimer = 0f;
    }

    protected override void Update(float delta)
    {
        float progress = Duration / StartingDuration;
        float x = Lerp(_targetPos.X, _startPos.X, Fade(progress));
        float y = Lerp(_targetPos.Y, _startPos.Y, Fade(progress));
        Position = new Vector2(x, y);

        _vfxTimer -= delta;
        if (_vfxTimer < 0f)
        {
            _vfxTimer = FireballInterval;
            SpawnTrailParticles();
        }

        Duration -= delta;
        if (Duration < 0f)
        {
            IsDone = true;
            SpawnImpactEffects();
        }
    }

    private void SpawnTrailParticles()
    {
        var parent = GetParent();
        if (parent == null) return;

        var flare = LightFlareParticleEffect.Create(Position.X, Position.Y, new Color(0.5f, 1f, 0f, 1f)); // Chartreuse
        parent.AddChild(flare);

        var burst = FireBurstParticleEffect.Create(Position.X, Position.Y);
        parent.AddChild(burst);
    }

    private void SpawnImpactEffects()
    {
        var parent = GetParent();
        if (parent == null) return;

        var ignite = GhostIgniteEffect.Create(Position.X, Position.Y);
        parent.AddChild(ignite);

        var weakFire = GhostlyWeakFireEffect.Create(Position.X, Position.Y);
        parent.AddChild(weakFire);
    }

    private static float Fade(float t)
    {
        return Mathf.Clamp(t * t * t * (t * (t * 6f - 15f) + 10f), 0f, 1f);
    }
}