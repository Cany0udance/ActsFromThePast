using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace ActsFromThePast;

public static class ShockWaveEffect
{
    private const int ParticleCount = 40;
    
    public static void Play(Vector2 position, Color color, ShockWaveType type, float duration = 2f)
    {
        float speed = (float)GD.RandRange(1000f, 1200f);

        if (type == ShockWaveType.Chaotic)
        {
            NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Short);
        }

        for (int i = 0; i < ParticleCount; i++)
        {
            var particle = BlurWaveEffect.Create(position, color, type, speed, duration);
            Sts1VfxHelper.Play(particle);
        }
    }
    
    public static void PlayRoyal(Vector2 position)
    {
        var color = new Color(0.255f, 0.412f, 0.878f, 1f);
        Play(position, color, ShockWaveType.Additive);
    }
    
    public static void PlayChaotic(Vector2 position)
    {
        var color1 = new Color(0.1f, 0.0f, 0.2f, 1f);
        var color2 = new Color(0.3f, 0.2f, 0.4f, 1f);
        Play(position, color1, ShockWaveType.Chaotic, 0.3f);
        Play(position, color2, ShockWaveType.Chaotic, 1.0f);
    }
}