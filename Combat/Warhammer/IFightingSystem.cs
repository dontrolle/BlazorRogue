namespace BlazorRogue.Combat.Warhammer;

interface IFightingSystem
{
    AttackResult CloseCombatAttack(CombatComponent attacker, CombatComponent defender);
}
