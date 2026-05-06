using ActsFromThePast.Minigames;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.SharedEvents;

public sealed class MatchAndKeep : CustomEventModel
{
    private const int Attempts = 5;

    public override ActModel[] Acts => Array.Empty<ActModel>();

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("Attempts", Attempts)
    };

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[] { Option(Continue) };
    }

    private Task Continue()
    {
        SetEventState(PageDescription("RULES"), new[]
        {
            Option(Play, "RULES")
        });
        return Task.CompletedTask;
    }

    private async Task Play()
    {
        var minigame = new MatchAndKeepMinigame(Owner, Rng, Attempts, Owner.RunState.CurrentActIndex);
        await minigame.PlayMinigame();
        SetEventFinished(PageDescription("COMPLETE"));
    }
}