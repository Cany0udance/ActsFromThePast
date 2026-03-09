using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class Sts1VfxHelper
{
    public static Vector2 GetCreatureCenter(Creature creature)
    {
        if (creature == null || creature.IsDead)
            return Vector2.Zero;
        
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        return creatureNode?.VfxSpawnPosition ?? Vector2.Zero;
    }

    public static void Play(NSts1Effect effect)
    {
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(effect);
    }
}