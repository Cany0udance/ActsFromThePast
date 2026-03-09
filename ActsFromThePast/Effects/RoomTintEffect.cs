using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;

namespace ActsFromThePast;

public partial class RoomTintEffect : NSts1Effect
{
    private ColorRect _rect;
    private float _tintTransparency;
    private Color _tintColor;

    public static RoomTintEffect Play(float transparency = 0.8f, float duration = 2.0f)
    {
        var effect = new RoomTintEffect();
        effect._tintTransparency = transparency;
        effect._tintColor = new Color(0f, 0f, 0f, 0f);
        effect.Duration = duration;
        effect.StartingDuration = duration;
        effect.Setup();
        NRun.Instance?.GlobalUi.AddChildSafely(effect);
        return effect;
    }

    protected override void Initialize()
    {
        _rect = new ColorRect();
        _rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _rect.Color = _tintColor;
        _rect.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_rect);
    }

    protected override void Update(float delta)
    {
        Duration -= delta;

        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }

        float halfDur = StartingDuration * 0.5f;

        if (Duration > halfDur)
        {
            float t = (Duration - halfDur) / StartingDuration;
            _tintColor.A = Lerp(_tintTransparency, 0f, Smootherstep(t));
        }
        else
        {
            float t = Duration / StartingDuration / 0.5f;
            _tintColor.A = Lerp(0f, _tintTransparency, Smootherstep(t));
        }

        _rect.Color = _tintColor;
    }
}