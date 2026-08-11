namespace BlazorRogue.Combat.Warhammer;

interface IFightingSystem
{
    bool CloseCombatAttack(CombatComponent attacker, CombatComponent defender);
}
