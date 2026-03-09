using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public partial class CollectorCurseEffect : NSts1Effect
{
    private float _x;
    private float _y;
    private int _count;
    private float _stakeTimer;

    public static CollectorCurseEffect Create(Vector2 position)
    {
        var effect = new CollectorCurseEffect();
        effect._x = position.X;
        effect._y = position.Y;
        effect._count = 13;
        effect._stakeTimer = 0f;
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = 99f;
        StartingDuration = 99f;
    }

    protected override void Update(float delta)
    {
        _stakeTimer -= delta;

        if (_stakeTimer < 0f)
        {
            if (_count == 13)
            {
                ModAudio.Play("collector", "collector_heavy_attack");
                RoomTintEffect.Play();
                BorderFlashEffect.Play(new Color(1.0f, 0.0f, 1.0f, 0.7f));
            }

            var stakePos = new Vector2(
                _x + (float)GD.RandRange(-50.0, 50.0),
                _y + (float)GD.RandRange(-60.0, 60.0)
            );

            var stake = CollectorStakeEffect.Create(stakePos);
            Node vfxContainer = NCombatRoom.Instance?.CombatVfxContainer;
            vfxContainer?.AddChildSafely(stake);

            _stakeTimer = 0.04f;
            _count--;

            if (_count == 0)
            {
                IsDone = true;
            }
        }
    }
}