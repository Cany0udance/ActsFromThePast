using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ActsFromThePast;

public partial class IntenseZoomEffect : NSts1Effect
{
    private const int ParticleCount = 10;
    
    private Vector2 _targetPosition;
    private bool _isBlack;
    private bool _spawned;

    public static IntenseZoomEffect Create(Vector2 position, bool isBlack = false)
    {
        var effect = new IntenseZoomEffect();
        effect._targetPosition = position;
        effect._isBlack = isBlack;
        effect.Position = position;
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = 0.1f;
        StartingDuration = 0.1f;
        _spawned = false;
    }

    protected override void Update(float delta)
    {
        if (!_spawned)
        {
            _spawned = true;
            SpawnEffects();
        }
        
        IsDone = true;
    }

    private void SpawnEffects()
    {
        var parent = GetParent();
        if (parent == null) return;
    
        if (_isBlack)
        {
          //  BorderFlashEffect.Play(Colors.Black);
        }
        else
        {
          //  BorderFlashEffect.PlayGold();
        }
    
        for (int i = 0; i < ParticleCount; i++)
        {
            var particle = IntenseZoomParticle.Create(_targetPosition, _isBlack);
            parent.AddChild(particle);
        }
    }
}