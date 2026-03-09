using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ActsFromThePast;

public static class BorderFlashEffect
{
    private static NSmokyVignetteVfx? _currentVfx;

    public static void Play(Color tint, Color? highlight = null)
    {
        var highlightColor = highlight ?? new Color(tint.R, tint.G, tint.B, 0.15f);
        
        if (_currentVfx != null && GodotObject.IsInstanceValid(_currentVfx))
        {
            _currentVfx.Reset(tint, highlightColor);
        }
        else
        {
            _currentVfx = NSmokyVignetteVfx.Create(tint, highlightColor);
            NRun.Instance?.GlobalUi.AddChildSafely(_currentVfx);
        }
    }

    public static void PlayChartreuse()
    {
        // Chartreuse is yellow-green: RGB roughly (0.5, 1.0, 0.0)
        var tint = new Color(0.5f, 1.0f, 0.0f, 0.3f);
        var highlight = new Color(0.7f, 1.0f, 0.2f, 0.15f);
        Play(tint, highlight);
    }
    
    public static void PlaySky()
    {
        var tint = new Color(0.53f, 0.81f, 0.92f, 0.3f);
        var highlight = new Color(0.68f, 0.85f, 0.95f, 0.15f);
        Play(tint, highlight);
    }

    public static void PlayFire()
    {
        // For Inferno's screen-on-fire effect
        var tint = new Color(1.0f, 0.3f, 0.0f, 0.4f);
        var highlight = new Color(1.0f, 0.5f, 0.0f, 0.2f);
        Play(tint, highlight);
    }
    
    public static void PlayGold()
    {
        var tint = new Color(0.937f, 0.808f, 0.373f, 0.3f);
        var highlight = new Color(0.96f, 0.88f, 0.5f, 0.15f);
        Play(tint, highlight);
    }
}