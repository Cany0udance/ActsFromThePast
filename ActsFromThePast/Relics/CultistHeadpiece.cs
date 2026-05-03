using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class CultistHeadpiece : CustomRelicModel
{
    private static readonly LocString CultistBanter =
        new LocString("monsters", "DAMP_CULTIST.moves.INCANTATION.banter");

    private static readonly string[] CultistSfx =
    {
        "event:/sfx/enemy/enemy_attacks/cultists/cultists_buff_damp",
        "event:/sfx/enemy/enemy_attacks/cultists/cultists_buff_calcified"
    };

    public override RelicRarity Rarity => RelicRarity.Event;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner.Creature.CombatState.RoundNumber != 1)
            return;

        Flash();
        TalkCmd.Play(CultistBanter, Owner.Creature, Owner.Character.SpeechBubbleColor);
        var sfx = CultistSfx[Rng.Chaotic.NextInt(CultistSfx.Length)];
        SfxCmd.Play(sfx);
    }
}