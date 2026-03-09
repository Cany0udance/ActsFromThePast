using Godot;

namespace ActsFromThePast;

public partial class WebEffect : NSts1Effect
{
    private const float EffectDuration = 1.0f;
    private const float SpawnInterval = 0.1f;
    
    private float _timer;
    private int _count;
    private Vector2 _targetPosition;
    private Vector2 _sourcePosition;

    public static WebEffect Create(Vector2 sourcePosition, Vector2 targetPosition)
    {
        var effect = new WebEffect();
        effect._sourcePosition = sourcePosition;
        effect._targetPosition = targetPosition;
        effect.Position = sourcePosition;
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = EffectDuration;
        StartingDuration = EffectDuration;
        _timer = 0f;
        _count = 0;
    }

    protected override void Update(float delta)
    {
        Duration -= delta;
        _timer -= delta;

        if (_timer < 0f)
        {
            _timer += SpawnInterval;
            SpawnEffectsForCount(_count);
            _count++;
        }

        if (Duration < 0f)
        {
            IsDone = true;
        }
    }

    private void SpawnEffectsForCount(int count)
    {
        var parent = GetParent();
        if (parent == null) return;

        switch (count)
        {
            case 0:
                SpawnWebLine(parent);
                SpawnWebLine(parent);
                // Y inverted: -10 becomes +10
                SpawnWebParticle(parent, _targetPosition + new Vector2(-90f, 10f));
                break;
            case 1:
                SpawnWebLine(parent);
                SpawnWebLine(parent);
                break;
            case 2:
                SpawnWebLine(parent);
                SpawnWebLine(parent);
                // Y inverted: +80 becomes -80
                SpawnWebParticle(parent, _targetPosition + new Vector2(70f, -80f));
                break;
            case 4:
                // Y inverted: -100 becomes +100
                SpawnWebParticle(parent, _targetPosition + new Vector2(30f, 100f));
                break;
        }
    }

    private void SpawnWebLine(Node parent)
    {
        var line = WebLineEffect.Create(_sourcePosition, true);
        parent.AddChild(line);
    }

    private void SpawnWebParticle(Node parent, Vector2 position)
    {
        var particle = WebParticleEffect.Create(position);
        parent.AddChild(particle);
    }
}