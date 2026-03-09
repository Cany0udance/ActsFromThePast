using ActsFromThePast.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class CursedTome : EventModel
{
    private const int DmgPage1 = 1;
    private const int DmgPage2 = 2;
    private const int DmgPage3 = 3;
    private const int DmgStop = 3;
    private const int DmgObtain = 15; // A15+ baked in

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new IntVar("DmgPage1", DmgPage1),
                new IntVar("DmgPage2", DmgPage2),
                new IntVar("DmgPage3", DmgPage3),
                new IntVar("DmgStop", DmgStop),
                new IntVar("DmgObtain", DmgObtain)
            };
        }
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new EventOption[]
        {
            new EventOption(this, new Func<Task>(ReadOption),
                "CURSED_TOME.pages.INITIAL.options.READ",
                Array.Empty<IHoverTip>()),
            new EventOption(this, new Func<Task>(LeaveOption),
                "CURSED_TOME.pages.INITIAL.options.LEAVE",
                Array.Empty<IHoverTip>())
        };
    }

    private Task ReadOption()
    {
        ModAudio.Play("events", "cursed_tome");
        
        SetEventState(L10NLookup("CURSED_TOME.pages.PAGE_1.description"), new EventOption[]
        {
            new EventOption(this, new Func<Task>(Page1Continue),
                "CURSED_TOME.pages.PAGE_1.options.CONTINUE",
                Array.Empty<IHoverTip>())
        });
        return Task.CompletedTask;
    }

    private async Task Page1Continue()
    {
        ModAudio.Play("events", "cursed_tome");
        
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DmgPage1,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        SetEventState(L10NLookup("CURSED_TOME.pages.PAGE_2.description"), new EventOption[]
        {
            new EventOption(this, new Func<Task>(Page2Continue),
                "CURSED_TOME.pages.PAGE_2.options.CONTINUE",
                Array.Empty<IHoverTip>())
        });
    }

    private async Task Page2Continue()
    {
        ModAudio.Play("events", "cursed_tome");
        
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DmgPage2,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        SetEventState(L10NLookup("CURSED_TOME.pages.PAGE_3.description"), new EventOption[]
        {
            new EventOption(this, new Func<Task>(Page3Continue),
                "CURSED_TOME.pages.PAGE_3.options.CONTINUE",
                Array.Empty<IHoverTip>())
        });
    }

    private async Task Page3Continue()
    {
        ModAudio.Play("events", "cursed_tome");
        
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DmgPage3,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        SetEventState(L10NLookup("CURSED_TOME.pages.LAST_PAGE.description"), new EventOption[]
        {
            new EventOption(this, new Func<Task>(ObtainBookOption),
                "CURSED_TOME.pages.LAST_PAGE.options.OBTAIN",
                Array.Empty<IHoverTip>()),
            new EventOption(this, new Func<Task>(StopReadingOption),
                "CURSED_TOME.pages.LAST_PAGE.options.STOP",
                Array.Empty<IHoverTip>())
        });
    }

    private async Task ObtainBookOption()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DmgObtain,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
    
        var relic = GetRandomBook().ToMutable();
    
        await RewardsCmd.OfferCustom(Owner, new List<Reward>(1)
        {
            new RelicReward(relic, Owner)
        });
    
        SetEventFinished(L10NLookup("CURSED_TOME.pages.OBTAIN.description"));
    }

    private async Task StopReadingOption()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DmgStop,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        SetEventFinished(L10NLookup("CURSED_TOME.pages.STOP.description"));
    }

    private async Task LeaveOption()
    {
        SetEventFinished(L10NLookup("CURSED_TOME.pages.LEAVE.description"));
    }

    private RelicModel GetRandomBook()
    {
        var possibleBooks = new List<RelicModel>();

        if (!Owner.Relics.Any(r => r is Necronomicon))
        {
            possibleBooks.Add(ModelDb.Relic<Necronomicon>());
        }

        if (!Owner.Relics.Any(r => r is Enchiridion))
        {
            possibleBooks.Add(ModelDb.Relic<Enchiridion>());
        }

        if (!Owner.Relics.Any(r => r is NilrysCodex))
        {
            possibleBooks.Add(ModelDb.Relic<NilrysCodex>());
        }

        if (possibleBooks.Count == 0)
        {
            return RelicFactory.PullNextRelicFromFront(Owner);
        }

        return Rng.NextItem(possibleBooks);
    }
}