using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class SecretPortal : CustomEventModel
{
    private const int MinRunTimeSeconds = 800;

    public override bool IsShared => true;

    public override ActModel[] Acts => new[] { ModelDb.Act<TheBeyondAct>() };

    public override bool IsAllowed(IRunState runState)
    {
        return RunManager.Instance.RunTime > MinRunTimeSeconds;
    }

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "secret_portal");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            Option(Enter),
            Option(Leave)
        };
    }

    private Task Enter()
    {
        SetEventState(PageDescription("ENTER"), new[]
        {
            new EventOption(this, TeleportToBoss,
                $"{Id.Entry}.pages.ENTER.options.CONTINUE",
                Array.Empty<IHoverTip>())
        });
        return Task.CompletedTask;
    }

    private Task TeleportToBoss()
    {
        var bossCoord = Owner.RunState.Map.BossMapPoint.coord;
        TaskHelper.RunSafely(RunManager.Instance.EnterMapCoord(bossCoord));
        return Task.CompletedTask;
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}