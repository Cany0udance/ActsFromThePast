using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

/// <summary>
/// A state that selects the next state based on a custom function.
/// Useful for complex move patterns that can't be expressed with RandomBranchState.
/// </summary>
public class ConditionalBranchState : MonsterState
{
    private readonly string _stateId;
    private readonly Func<Creature, Rng, MonsterMoveStateMachine, string> _selectNextState;
    
    public override string Id => _stateId;
    public override bool ShouldAppearInLogs => false;
    
    public ConditionalBranchState(
        string stateId, 
        Func<Creature, Rng, MonsterMoveStateMachine, string> selectNextState)
    {
        _stateId = stateId;
        _selectNextState = selectNextState;
    }
    
    public override string GetNextState(Creature owner, Rng rng)
    {
        return _selectNextState(owner, rng, owner.Monster.MoveStateMachine);
    }
    
    public override void RegisterStates(Dictionary<string, MonsterState> monsterStates)
    {
        monsterStates.Add(Id, this);
    }
}