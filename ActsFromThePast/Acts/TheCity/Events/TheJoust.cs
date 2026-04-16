using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class TheJoust : CustomEventModel
{
    private const int BetAmount = 50;
    private const int WinMurderer = 100;
    private const int WinOwner = 250;
    private const float OwnerWinChance = 0.3f;
    private bool _betForOwner;
    private bool _ownerWins;

    public override ActModel[] Acts => new[] { ModelDb.Act<TheCityAct>() };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("BetAmount", BetAmount),
        new IntVar("WinMurderer", WinMurderer),
        new IntVar("WinOwner", WinOwner)
    };

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[] { Option(Continue) };
    }

    private Task Continue()
    {
        SetEventState(PageDescription("EXPLANATION"), new[]
        {
            Option(BetMurderer, "EXPLANATION"),
            Option(BetOwner, "EXPLANATION")
        });
        return Task.CompletedTask;
    }

    private async Task BetMurderer()
    {
        _betForOwner = false;
        await PlayerCmd.LoseGold(BetAmount, Owner);

        SetEventState(PageDescription("BET_MURDERER"), new[]
        {
            new EventOption(this, WatchJoust,
                $"{Id.Entry}.pages.BET_MURDERER.options.WATCH",
                Array.Empty<IHoverTip>())
        });
    }

    private async Task BetOwner()
    {
        _betForOwner = true;
        await PlayerCmd.LoseGold(BetAmount, Owner);

        SetEventState(PageDescription("BET_OWNER"), new[]
        {
            new EventOption(this, WatchJoust,
                $"{Id.Entry}.pages.BET_OWNER.options.WATCH",
                Array.Empty<IHoverTip>())
        });
    }

    private async Task WatchJoust()
    {
        _ownerWins = Rng.NextFloat() < OwnerWinChance;
        NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/cultists/cultists_attack");
        await Cmd.Wait(1.0f);
        NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/assassin_ruby_raider/assassin_ruby_raider_attack");
        await Cmd.Wait(0.25f);
        NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/cultists/cultists_attack");

        SetEventState(PageDescription("COMBAT"), new[]
        {
            new EventOption(this, ResolveJoust,
                $"{Id.Entry}.pages.COMBAT.options.CONTINUE",
                Array.Empty<IHoverTip>())
        });
    }

    private async Task ResolveJoust()
    {
        if (_ownerWins)
        {
            if (_betForOwner)
            {
                await PlayerCmd.GainGold(WinOwner, Owner);
                SetEventFinished(PageDescription("OWNER_WINS_BET_WON"));
            }
            else
            {
                SetEventFinished(PageDescription("OWNER_WINS_BET_LOST"));
            }
        }
        else
        {
            if (_betForOwner)
            {
                SetEventFinished(PageDescription("MURDERER_WINS_BET_LOST"));
            }
            else
            {
                await PlayerCmd.GainGold(WinMurderer, Owner);
                SetEventFinished(PageDescription("MURDERER_WINS_BET_WON"));
            }
        }
    }
}