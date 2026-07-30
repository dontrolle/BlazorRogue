using System;

namespace BlazorRogue.Combat.Warhammer;

class CombatComponent(int weaponSkill, int weaponDamage, int toughness, int armourPoints, int wounds) : Component
{
    public int MaxWounds { get; private set; } = wounds;
    public int WeaponSkill { get; private set; } = weaponSkill;
    public int WeaponDamage { get; private set; } = weaponDamage;
    public int Toughness { get; private set; } = toughness;
    public int ToughnessBonus => Toughness / 10;
    public int ArmourPoints { get; private set; } = armourPoints;
    public int DamageSoak => ToughnessBonus + ArmourPoints;

    readonly int healthGainedByOneStep = 1;

    public int Wounds
    {
        get;
        private set
        {
            // we use MaxWounds as an ultimate upper bound - no wounds value can go beyond that. 
            // Means temporary higher max wounds must be reflected in the Maxwounds field.
            field = Math.Min(value, MaxWounds);

            if (Owner != null)
            {
                System.Diagnostics.Debug.WriteLine($"{Owner.Name} now has {field}W");
            }

            if (field <= 0)
            {
                Owner!.Kill();
            }
        }
    } = wounds;

    // TODO: AdvantageCap=0 ie, disable Advantage - at least for now
    const int AdvantageCap = 0;

    public int Advantage
    {
        get;

        private set
        {
            field = value;
            System.Diagnostics.Debug.WriteLine($"{Owner!.Name} now has {Advantage} Advantage");
        }
    }

    public bool IsStarving { get; internal set; }

    public void ApplyDamage(int damage) => Wounds -= damage - DamageSoak;

    public void HealByMove() => Wounds += healthGainedByOneStep;

    public void GainAdvantage(int number = 1)
    {
        int adv = Advantage + number;
        Advantage = Math.Clamp(adv, 0, AdvantageCap);
    }

    public void ResetAdvantage() => Advantage = 0;

    public void LooseAdvantage() => GainAdvantage(-1);
}
