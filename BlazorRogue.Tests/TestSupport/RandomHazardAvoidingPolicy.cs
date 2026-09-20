using BlazorRogue.GameObjects;
using BlazorRogue.World;

namespace BlazorRogue.Tests.TestSupport;

/// <summary>
/// Supplies the next <see cref="PlayerAction"/> for a <see cref="HeadlessPlayDriver"/> to take,
/// given the current <see cref="Map"/> state - the seam a play-balance sweep (issue #88) would
/// implement its own smarter policy against instead of <see cref="RandomHazardAvoidingPolicy"/>.
/// </summary>
interface IPlayerPolicy
{
    PlayerAction NextAction(Map map);
}

/// <summary>
/// The built-in default policy: picks uniformly at random among the player's currently-legal
/// actions - stepping onto an open, non-lethal adjacent tile; attacking whatever moveable occupies
/// an adjacent tile; or picking up an item standing underfoot - never a <see cref="Direction"/>
/// <see cref="Map.PeekLethalLiquidStep"/> flags as walking into instakill liquid (lava). Falls back
/// to waiting in place (<see cref="Direction.None"/>) on the rare turn nothing else is legal (e.g.
/// boxed in on every side by blocked edges).
/// </summary>
sealed class RandomHazardAvoidingPolicy(Random? random = null) : IPlayerPolicy
{
    static readonly Direction[] MoveDirections =
    [
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West,
        Direction.NorthEast,
        Direction.NorthWest,
        Direction.SouthEast,
        Direction.SouthWest,
    ];

    readonly Random random = random ?? new Random();

    public PlayerAction NextAction(Map map)
    {
        var actions = LegalActions(map);
        return actions.Count == 0
            ? new PlayerAction.Move(Direction.None)
            : actions[random.Next(actions.Count)];
    }

    static List<PlayerAction> LegalActions(Map map)
    {
        var player = map.Player;
        bool flying = Map.IsFlying(player);
        var actions = new List<PlayerAction>();

        foreach (var direction in MoveDirections)
        {
            var (dx, dy) = direction.ToDelta();
            int destX = player.X + dx;
            int destY = player.Y + dy;

            if (destX < 0 || destY < 0 || destX >= map.Width || destY >= map.Height)
            {
                continue;
            }

            if (!flying && map.IsMovementBlockedAcrossEdge(player.X, player.Y, destX, destY))
            {
                continue;
            }

            bool hasAttackTarget = map.Moveables.Any(m =>
                !ReferenceEquals(m, player)
                && m.X == destX
                && m.Y == destY
                && m.CombatComponent != null
            );
            if (hasAttackTarget)
            {
                actions.Add(new PlayerAction.Move(direction));
                continue;
            }

            if (map.IsBlocked(destX, destY) || map.PeekLethalLiquidStep(direction, out _))
            {
                continue;
            }

            actions.Add(new PlayerAction.Move(direction));
        }

        if (map.GameObjectByCoord[player.X, player.Y].OfType<Item>().Any())
        {
            actions.Add(new PlayerAction.PickUp());
        }

        return actions;
    }
}
