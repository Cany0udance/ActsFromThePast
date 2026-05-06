using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Minigames;

public class WheelSpinMinigame
{
    private readonly TaskCompletionSource _completionSource = new();
    private readonly Player _owner;
 
    /// <summary>
    /// The winning segment index (0–5), determined by the event's Rng.
    /// </summary>
    public int Result { get; }
 
    /// <summary>
    /// Visual landing angle in degrees, includes a small cosmetic offset.
    /// </summary>
    public float ResultAngle { get; }
 
    /// <summary>
    /// Current act index, used to select the event background.
    /// </summary>
    public int ActIndex { get; }
 
    public event Action? Finished;
 
    public WheelSpinMinigame(Player owner, int result, int actIndex)
    {
        _owner = owner;
        Result = result;
        ActIndex = actIndex;
        ResultAngle = result * 60f + Rng.Chaotic.NextInt(-10, 11);
    }
 
    public void Complete()
    {
        if (_completionSource.Task.IsCompleted) return;
        _completionSource.SetResult();
        Finished?.Invoke();
    }
 
    public void ForceEnd()
    {
        _completionSource.TrySetCanceled();
    }
 
    public async Task PlayMinigame()
    {
        if (!LocalContext.IsMe(_owner))
            return;
        
        NWheelSpinScreen.ShowScreen(this);
        await _completionSource.Task;
    }
}

