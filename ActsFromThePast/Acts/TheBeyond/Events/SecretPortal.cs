using ActsFromThePast.Minigames;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class SecretPortal : CustomEventModel
{
    private const int MinRunTimeSeconds = 800;

    public override bool IsShared => true;

    public override ActModel[] Acts => new[] { ModelDb.Act<TheBeyondAct>() };

    public override bool IsAllowed(IRunState runState)
    {
        if (RunManager.Instance.RunTime <= MinRunTimeSeconds)
            return false;
        if (ActsFromThePastConfig.RebalancedMode && runState.Players.Count > 1)
            return false;
        return true;
    }

    public override void OnRoomEnter()
    {
        ModAudio.Play("events", "secret_portal");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
        {
            return new[]
            {
                Option(Enter, "INITIAL_REBALANCED"),
                Option(ReachIn, "INITIAL_REBALANCED")
            };
        }

        return new[]
        {
            Option(Enter),
            Option(Leave)
        };
    }

    private async Task Enter()
    {
        if (ActsFromThePastConfig.RebalancedMode)
        {
            var concreteState = (RunState)Owner.RunState;
            var map = Owner.RunState.Map;
            var currentRow = Owner.RunState.CurrentMapCoord?.row ?? 0;
            var availableNodes = map.GetRowCount() - 1 - currentRow;

            var minigame = new PortalMapBuilderMinigame(Owner, Rng, availableNodes);
            await minigame.PlayMinigame();

            // Reset the map selection synchronizer
            RunManager.Instance.MapSelectionSynchronizer.BeforeMapGenerated();

            // Build compact map
            var newMap = new PortalBuilderActMap(concreteState, minigame.Nodes, minigame.AvailableNodeCount);
            Owner.RunState.Map = newMap;

            // Clear stale coords and restore at new positions
            concreteState.RemoveStaleVisitedMapCoords(newMap);
            foreach (var coord in newMap.NewVisitedCoords)
                concreteState.AddVisitedMapCoord(coord);

            // Reset synchronizer to accept votes from the new location
            RunManager.Instance.MapSelectionSynchronizer.OnLocationChanged(Owner.RunState.MapLocation);

            // Refresh map screen
            NMapScreen.Instance?.SetMap(newMap, Owner.RunState.Rng.Seed, true);

            SetEventFinished(PageDescription("ENTER_REBALANCED"));
            return;
        }

        SetEventState(PageDescription("ENTER"), new[]
        {
            new EventOption(this, TeleportToBoss,
                $"{Id.Entry}.pages.ENTER.options.CONTINUE",
                Array.Empty<IHoverTip>())
        });
    }
    
    private Task TeleportToBoss()
    {
        var bossCoord = Owner.RunState.Map.BossMapPoint.coord;
        TaskHelper.RunSafely(RunManager.Instance.EnterMapCoord(bossCoord));
        return Task.CompletedTask;
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
    
    private async Task ReachIn()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1);
        foreach (var original in (await CardSelectCmd.FromDeckForTransformation(Owner, prefs)).ToList())
        {
            var transformed = CardFactory.CreateRandomCardForTransform(original, false, Owner.RunState.Rng.Niche);
            CardCmd.Upgrade(transformed);
            await CardCmd.Transform(original, transformed);
        }
        SetEventFinished(PageDescription("REACH_IN"));
    }
}