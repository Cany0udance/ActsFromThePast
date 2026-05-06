using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Minigames;

public class MatchAndKeepMinigame
{
    private readonly TaskCompletionSource _completionSource = new();

    public Player Owner { get; }

    /// <summary>12 shuffled card instances for display.</summary>
    public CardModel[] Cards { get; }

    /// <summary>Pair index (0–5) for each of the 12 cards. Two cards match if they share a pair index.</summary>
    public int[] PairIndices { get; }

    /// <summary>The 6 canonical card models, indexed by pair index.</summary>
    public CardModel[] Canonicals { get; }

    public int MaxAttempts { get; }
    public int ActIndex { get; }

    public MatchAndKeepMinigame(Player owner, Rng rng, int attempts, int actIndex)
    {
        Owner = owner;
        MaxAttempts = attempts;
        ActIndex = actIndex;

        Cards = new CardModel[12];
        PairIndices = new int[12];
        Canonicals = new CardModel[6];

        GenerateCards(rng);
        ShuffleCards(rng);
    }

    private void GenerateCards(Rng rng)
    {
        var characterPool = Owner.Character.CardPool
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .ToList();

        var cursePool = ModelDb.CardPool<CurseCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.CanBeGeneratedByModifiers)
            .ToList();

        Canonicals[0] = rng.NextItem(characterPool.Where(c => c.Rarity == CardRarity.Rare));
        Canonicals[1] = rng.NextItem(characterPool.Where(c => c.Rarity == CardRarity.Uncommon));
        Canonicals[2] = rng.NextItem(characterPool.Where(c => c.Rarity == CardRarity.Common));

        Canonicals[3] = ActsFromThePastConfig.RebalancedMode
            ? (CardModel)ModelDb.Card<Guilty>()
            : rng.NextItem(cursePool);

        Canonicals[4] = ActsFromThePastConfig.RebalancedMode
            ? (CardModel)ModelDb.Card<Guilty>()
            : rng.NextItem(cursePool);

        Canonicals[5] = rng.NextItem(characterPool.Where(c =>
            c.Rarity == CardRarity.Basic &&
            !c.Tags.Contains(CardTag.Strike) &&
            !c.Tags.Contains(CardTag.Defend)));

        // Create 2 instances per canonical
        for (int i = 0; i < 6; i++)
        {
            Cards[i * 2] = Owner.RunState.CreateCard(Canonicals[i], Owner);
            Cards[i * 2 + 1] = Owner.RunState.CreateCard(Canonicals[i], Owner);
            PairIndices[i * 2] = i;
            PairIndices[i * 2 + 1] = i;
        }
    }

    private void ShuffleCards(Rng rng)
    {
        for (int i = 11; i > 0; i--)
        {
            int j = rng.NextInt(i + 1);
            (Cards[i], Cards[j]) = (Cards[j], Cards[i]);
            (PairIndices[i], PairIndices[j]) = (PairIndices[j], PairIndices[i]);
        }
    }

    public void Complete()
    {
        if (_completionSource.Task.IsCompleted) return;
        _completionSource.SetResult();
    }

    public void ForceEnd()
    {
        if (_completionSource.Task.IsCompleted) return;
        _completionSource.TrySetCanceled();
    }

    public async Task PlayMinigame()
    {
        if (!LocalContext.IsMe(Owner))
            return;
        
        NMatchAndKeepScreen.ShowScreen(this);
        await _completionSource.Task;
    }
}