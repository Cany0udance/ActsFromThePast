using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class BronzeOrb : CustomMonsterModel
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 54, 52);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 60, 58);

    private const int BeamDamage = 8;
    private const int SupportBlockAmount = 12;

    protected override string VisualsPath => "res://ActsFromThePast/monsters/bronze_orb/bronze_orb.tscn";

    private const string BEAM = "BEAM";
    private const string SUPPORT_BEAM = "SUPPORT_BEAM";
    private const string STASIS = "STASIS";
    private int _bobIndex;
    public int BobIndex
    {
        get => _bobIndex;
        set
        {
            AssertMutable();
            _bobIndex = value;
        }
    }
    
    private bool _usedStasis;
    private bool UsedStasis
    {
        get => _usedStasis;
        set
        {
            AssertMutable();
            _usedStasis = value;
        }
    }
    
    private bool _spawnAnimPending;
    public bool SpawnAnimPending
    {
        get => _spawnAnimPending;
        set
        {
            AssertMutable();
            _spawnAnimPending = value;
        }
    }


    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        _usedStasis = false;

        if (SpawnAnimPending)
        {
            var visuals = NCombatRoom.Instance?.GetCreatureNode(Creature)?.Visuals;
            var sprite = visuals?.GetNodeOrNull<Sprite2D>("Visuals");
            if (sprite != null)
                sprite.Visible = false;
        }

        StartBobAnimation();
    }
    
    private void StartBobAnimation()
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        var visuals = creatureNode?.Visuals;
        if (visuals == null) return;

        var basePos = visuals.Position;
        var amplitude = 6.0f;
        var duration = 2.16f; // 360 * 6ms = ~2160ms full cycle

        var tween = creatureNode.CreateTween();
        tween.SetLoops();

        if (BobIndex % 2 == 0)
        {
            tween.TweenProperty(visuals, "position:y", basePos.Y - amplitude, duration / 2.0f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            tween.TweenProperty(visuals, "position:y", basePos.Y + amplitude, duration / 2.0f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
        }
        else
        {
            tween.TweenProperty(visuals, "position:y", basePos.Y + amplitude, duration / 2.0f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            tween.TweenProperty(visuals, "position:y", basePos.Y - amplitude, duration / 2.0f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var beamState = new MoveState(
            BEAM,
            BeamMove,
            new AbstractIntent[] { new SingleAttackIntent(BeamDamage) }
        );

        var supportBeamState = new MoveState(
            SUPPORT_BEAM,
            SupportBeamMove,
            new AbstractIntent[] { new DefendIntent() }
        );

        var stasisState = new MoveState(
            STASIS,
            StasisMove,
            new AbstractIntent[] { new CardDebuffIntent() }
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        beamState.FollowUpState = moveBranch;
        supportBeamState.FollowUpState = moveBranch;
        stasisState.FollowUpState = moveBranch;

        states.Add(beamState);
        states.Add(supportBeamState);
        states.Add(stasisState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        int num = rng.NextInt(100);

        if (!UsedStasis && num >= 25)
        {
            UsedStasis = true;
            return STASIS;
        }

        if (num >= 70 && !LastTwoMoves(stateMachine, SUPPORT_BEAM))
        {
            return SUPPORT_BEAM;
        }

        if (!LastTwoMoves(stateMachine, BEAM))
        {
            return BEAM;
        }

        return SUPPORT_BEAM;
    }

    private static bool LastTwoMoves(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count < 2) return false;
        return log[log.Count - 1].Id == moveId && log[log.Count - 2].Id == moveId;
    }

    private async Task BeamMove(IReadOnlyList<Creature> targets)
    {
        BorderFlashEffect.PlaySky();
        ModAudio.Play("general", "magic_beam_short", -6f);

        var target = targets.FirstOrDefault(t => t.IsAlive);
        var targetPos = target != null
            ? Sts1VfxHelper.GetCreatureCenter(target)
            : Vector2.Zero;
        var orbPos = Sts1VfxHelper.GetCreatureCenter(Creature);

        var laser = SmallLaserEffect.Create(orbPos, targetPos);
        Sts1VfxHelper.Play(laser);

        await Cmd.Wait(0.3f);
        await DamageCmd.Attack(BeamDamage)
            .FromMonster(this)
            .Execute(null);
    }

    private async Task SupportBeamMove(IReadOnlyList<Creature> targets)
    {
        var automaton = CombatState.GetTeammatesOf(Creature)
            .FirstOrDefault(t => t.Monster is BronzeAutomaton && t.IsAlive);

        if (automaton == null)
            return;

        ModAudio.Play("general", "magic_beam_short", -6f);

        var orbPos = Sts1VfxHelper.GetCreatureCenter(Creature);
        var automatonPos = Sts1VfxHelper.GetCreatureCenter(automaton);

        var laser = SmallLaserEffect.Create(orbPos, automatonPos);
        Sts1VfxHelper.Play(laser);

        await Cmd.Wait(0.3f);
        await CreatureCmd.GainBlock(automaton, SupportBlockAmount, ValueProp.Move, null);
    }

    private async Task StasisMove(IReadOnlyList<Creature> targets)
    {
        foreach (var target in targets.Where(t => t.IsAlive))
        {
            var player = target.Player ?? target.PetOwner;

            var drawCards = CardPile.GetCards(player, PileType.Draw)
                .Where(c => c.DeckVersion != null)
                .ToList();

            var discardCards = CardPile.GetCards(player, PileType.Discard)
                .Where(c => c.DeckVersion != null)
                .ToList();

            if (drawCards.Count == 0 && discardCards.Count == 0)
                continue;

            var pool = drawCards.Count > 0 ? drawCards : discardCards;
            pool.StableShuffle(RunRng.CombatCardGeneration);

            var cardToSteal = pool.FirstOrDefault(c => c.Rarity == CardRarity.Rare)
                              ?? pool.FirstOrDefault(c => c.Rarity == CardRarity.Uncommon)
                              ?? pool.FirstOrDefault(c => c.Rarity == CardRarity.Common)
                              ?? pool.FirstOrDefault();

            if (cardToSteal == null)
                continue;

            await CardPileCmd.RemoveFromCombat(cardToSteal, false);

            // Show stolen card on the orb
            var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
            if (creatureNode != null && LocalContext.IsMine(cardToSteal))
            {
                var specialNode = creatureNode.GetSpecialNode<Marker2D>("%StolenCardPos");
                if (specialNode != null)
                {
                    var ncard = NCard.Create(cardToSteal);
                    specialNode.AddChildSafely(ncard);
                    ncard.Position = ncard.Position + ncard.Size * 0.5f;
                    ncard.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
                }
            }

            var stasis = (StasisPower)ModelDb.Power<StasisPower>().ToMutable();
            await stasis.Capture(cardToSteal, target);
            await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), stasis, Creature, 1, Creature, null);
        }
    }
}