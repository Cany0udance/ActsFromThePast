using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class SecretPortal : EventModel
{
    private const int MinRunTimeSeconds = 800;

    public override bool IsAllowed(RunState runState)
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
            new EventOption(this, EnterPortalOption,
                "SECRET_PORTAL.pages.INITIAL.options.ENTER"),
            new EventOption(this, LeaveOption,
                "SECRET_PORTAL.pages.INITIAL.options.LEAVE")
        };
    }

    private Task EnterPortalOption()
    {
        SetEventState(
            L10NLookup("SECRET_PORTAL.pages.ENTER.description"),
            new[]
            {
                new EventOption(this, TeleportToBoss,
                    "SECRET_PORTAL.pages.ENTER.options.CONTINUE")
            }
        );
        return Task.CompletedTask;
    }

    private Task TeleportToBoss()
    {
        var bossCoord = Owner.RunState.Map.BossMapPoint.coord;
        TaskHelper.RunSafely(RunManager.Instance.EnterMapCoord(bossCoord));
        return Task.CompletedTask;
    }

    private Task LeaveOption()
    {
        SetEventFinished(
            L10NLookup("SECRET_PORTAL.pages.LEAVE.description"));
        return Task.CompletedTask;
    }
}