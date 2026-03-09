using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class TheJoust : EventModel
{
    private const int BetAmount = 50;
    private const int WinMurderer = 100;
    private const int WinOwner = 250;
    private const float OwnerWinChance = 0.3f;

    private bool _betForOwner;
    private bool _ownerWins;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new IntVar("BetAmount", BetAmount),
                new IntVar("WinMurderer", WinMurderer),
                new IntVar("WinOwner", WinOwner)
            };
        }
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new EventOption[]
        {
            new EventOption(this, new Func<Task>(ContinueToExplanation),
                "THE_JOUST.pages.INITIAL.options.CONTINUE",
                Array.Empty<IHoverTip>())
        };
    }

    private Task ContinueToExplanation()
    {
        SetEventState(L10NLookup("THE_JOUST.pages.EXPLANATION.description"), new EventOption[]
        {
            new EventOption(this, new Func<Task>(BetOnMurderer),
                "THE_JOUST.pages.EXPLANATION.options.BET_MURDERER",
                Array.Empty<IHoverTip>()),
            new EventOption(this, new Func<Task>(BetOnOwner),
                "THE_JOUST.pages.EXPLANATION.options.BET_OWNER",
                Array.Empty<IHoverTip>())
        });
        return Task.CompletedTask;
    }

    private async Task BetOnMurderer()
    {
        _betForOwner = false;
        await PlayerCmd.LoseGold(BetAmount, Owner);
        
        SetEventState(L10NLookup("THE_JOUST.pages.BET_MURDERER.description"), new EventOption[]
        {
            new EventOption(this, new Func<Task>(WatchJoust),
                "THE_JOUST.pages.BET_MURDERER.options.WATCH",
                Array.Empty<IHoverTip>())
        });
    }

    private async Task BetOnOwner()
    {
        _betForOwner = true;
        await PlayerCmd.LoseGold(BetAmount, Owner);
        
        SetEventState(L10NLookup("THE_JOUST.pages.BET_OWNER.description"), new EventOption[]
        {
            new EventOption(this, new Func<Task>(WatchJoust),
                "THE_JOUST.pages.BET_OWNER.options.WATCH",
                Array.Empty<IHoverTip>())
        });
    }

    private Task WatchJoust()
    {
        _ownerWins = Rng.NextFloat() < OwnerWinChance;
    
        // TODO: Screen shake and sound effects if possible
        // The original had a timed sequence with BLUNT_HEAVY and BLUNT_FAST sounds
    
        SetEventState(L10NLookup("THE_JOUST.pages.COMBAT.description"), new EventOption[]
        {
            new EventOption(this, new Func<Task>(ResolveJoust),
                "THE_JOUST.pages.COMBAT.options.CONTINUE",
                Array.Empty<IHoverTip>())
        });
        return Task.CompletedTask;
    }
    private async Task ResolveJoust()
    {
        if (_ownerWins)
        {
            if (_betForOwner)
            {
                await PlayerCmd.GainGold(WinOwner, Owner);
                SetEventFinished(L10NLookup("THE_JOUST.pages.OWNER_WINS_BET_WON.description"));
            }
            else
            {
                SetEventFinished(L10NLookup("THE_JOUST.pages.OWNER_WINS_BET_LOST.description"));
            }
        }
        else
        {
            if (_betForOwner)
            {
                SetEventFinished(L10NLookup("THE_JOUST.pages.MURDERER_WINS_BET_LOST.description"));
            }
            else
            {
                await PlayerCmd.GainGold(WinMurderer, Owner);
                SetEventFinished(L10NLookup("THE_JOUST.pages.MURDERER_WINS_BET_WON.description"));
            }
        }
    }
}