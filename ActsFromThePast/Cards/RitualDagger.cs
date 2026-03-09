using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Cards;

public sealed class RitualDagger : CardModel
{
    private const string _increaseKey = "Increase";
    private const int _baseDamage = 15;
    private int _currentDamage = 15;
    private int _increasedDamage;

    public RitualDagger() : base(
        canonicalEnergyCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Event,
        targetType: TargetType.AnyEnemy)
    {
    }

    [SavedProperty]
    public int CurrentDamage
    {
        get => this._currentDamage;
        set
        {
            this.AssertMutable();
            this._currentDamage = value;
            this.DynamicVars.Damage.BaseValue = this._currentDamage;
        }
    }

    [SavedProperty]
    public int IncreasedDamage
    {
        get => this._increasedDamage;
        set
        {
            this.AssertMutable();
            this._increasedDamage = value;
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            return new[] { CardKeyword.Exhaust };
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new DamageVar(this.CurrentDamage, ValueProp.Move),
                new IntVar(_increaseKey, 3M)
            };
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            return new IHoverTip[]
            {
                HoverTipFactory.Static(StaticHoverTip.Fatal)
            };
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        var shouldTriggerFatal = cardPlay.Target.Powers.All(p => p.ShouldOwnerDeathTriggerFatal());
        var attackCommand = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        if (!shouldTriggerFatal || !attackCommand.Results.Any(r => r.WasTargetKilled))
            return;
        var increase = DynamicVars[_increaseKey].IntValue;
        BuffFromPlay(increase);
        if (DeckVersion is RitualDagger deckVersion)
        {
            deckVersion.BuffFromPlay(increase);
        }
    }

    protected override void OnUpgrade() => DynamicVars[_increaseKey].UpgradeValueBy(2M);

    protected override void AfterDowngraded() => UpdateDamage();

    private void BuffFromPlay(int extraDamage)
    {
        IncreasedDamage += extraDamage;
        UpdateDamage();
    }

    private void UpdateDamage() => CurrentDamage = _baseDamage + IncreasedDamage;
}