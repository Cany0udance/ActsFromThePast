using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace ActsFromThePast;

public class DynamicSingleAttackIntent : AttackIntent
{
    private readonly Func<int> _damageFunc;

    protected override LocString IntentLabelFormat => new LocString("intents", "FORMAT_DAMAGE_SINGLE");
    public override int Repeats => 1;

    public DynamicSingleAttackIntent(Func<int> damageFunc)
    {
        _damageFunc = damageFunc;
        DamageCalc = () => (decimal)_damageFunc();
    }

    public override int GetTotalDamage(IEnumerable<Creature> targets, Creature owner)
    {
        return GetSingleDamage(targets, owner);
    }

    public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
    {
        LocString intentLabelFormat = IntentLabelFormat;
        float totalDamage = (float)GetTotalDamage(targets, owner);
        intentLabelFormat.Add("Damage", (decimal)(int)totalDamage);
        return intentLabelFormat;
    }
}