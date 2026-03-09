using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ActsFromThePast;

public partial class IntimidateEffect : NSts1Effect
{
    private const float EffectDuration = 1.0f;
    private const float VfxInterval = 0.016f;
    private float _vfxTimer;
    private Color _particleColor;

    public static IntimidateEffect Create(Vector2 position)
    {
        var effect = new IntimidateEffect();
        effect.Position = position;
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = EffectDuration;
        StartingDuration = EffectDuration;
        _vfxTimer = 0f;
        _particleColor = new Color(1.0f, 0.96f, 0.88f, 1.0f);
    }

    protected override void Update(float delta)
    {
        Duration -= delta;
        _vfxTimer -= delta;

        if (_vfxTimer < 0f)
        {
            _vfxTimer = VfxInterval;
            SpawnWobblyLine();
        }

        if (Duration < 0f)
        {
            IsDone = true;
        }
    }

    private void SpawnWobblyLine()
    {
        var wobblyLine = WobblyLineEffect.Create(GlobalPosition, _particleColor);
        GetParent().AddChild(wobblyLine);
        wobblyLine.GlobalPosition = GlobalPosition;
    }
}