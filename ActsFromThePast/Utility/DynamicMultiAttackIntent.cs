using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace ActsFromThePast;

public class DynamicMultiAttackIntent : AttackIntent
{
    private readonly int _repeat;
    private readonly Func<int>? _repeatFunc;
    private readonly Func<int> _damageFunc;
    
    protected override LocString IntentLabelFormat => new LocString("intents", "FORMAT_DAMAGE_MULTI");
    public override int Repeats => _repeatFunc?.Invoke() ?? _repeat;
    
    public DynamicMultiAttackIntent(Func<int> damageFunc, int repeat)
    {
        _damageFunc = damageFunc;
        _repeat = repeat;
        DamageCalc = () => (Decimal)_damageFunc();
    }
    
    public DynamicMultiAttackIntent(Func<int> damageFunc, Func<int> repeatFunc)
    {
        _damageFunc = damageFunc;
        _repeatFunc = repeatFunc;
        DamageCalc = () => (Decimal)_damageFunc();
    }
    
    public override int GetTotalDamage(IEnumerable<Creature> targets, Creature owner)
    {
        return GetSingleDamage(targets, owner) * Repeats;
    }
    
    public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
    {
        LocString intentLabelFormat = IntentLabelFormat;
        float singleDamage = (float)GetSingleDamage(targets, owner);
        intentLabelFormat.Add("Damage", (Decimal)(int)singleDamage);
        intentLabelFormat.Add("Repeat", (Decimal)Repeats);
        return intentLabelFormat;
    }
}