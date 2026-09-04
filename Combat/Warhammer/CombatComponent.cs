using System;
using BlazorRogue.Components;

namespace BlazorRogue.Combat.Warhammer;

class CombatComponent(
    int weaponSkill,
    int weaponDamage,
    int toughness,
    int armourPoints,
    int wounds
) : Component
{
    public int MaxWounds { get; private set; } = wounds;
    public int WeaponSkill { get; private set; } = weaponSkill;
    public int WeaponDamage { get; private set; } = weaponDamage;
    public int Toughness { get; private set; } = toughness;
    public int ToughnessBonus => Toughness / 10;
    public int ArmourPoints { get; private set; } = armourPoints;

    /// <summary>Armour granted by currently-equipped items - see <see cref="AdjustEquipmentArmourBonus"/>.</summary>
    public int EquipmentArmourBonus { get; private set; }
    public int DamageSoak => ToughnessBonus + ArmourPoints + EquipmentArmourBonus;

    readonly int healthGainedByOneStep = 1;
    const int ChanceToHealInTurn = 33; // every third turn, approximately

    public int Wounds
    {
        get;
        private set
        {
            // we use MaxWounds as an ultimate upper bound - no wounds value can go beyond that.
            // Means temporary higher max wounds must be reflected in the Maxwounds field. Clamped
            // to 0 at the bottom too - a dead Moveable sitting at, say, -8 wounds is meaningless.
            field =
                Math.Clamp(value, 0, MaxWounds);

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

    // Damage soak (toughness bonus + armour) can reduce a hit to nothing, but never turn it into
    // healing - clamp the post-soak amount at zero.
    public void ApplyDamage(int damage) => Wounds -= Math.Max(0, damage - DamageSoak);

    public void HealByMove()
    {
        if (Dice.RollD100() <= ChanceToHealInTurn)
        {
            Heal(healthGainedByOneStep);
        }
    }

    public void Heal(int amount) => Wounds += amount;

    /// <summary>Applied on equip (positive) / unequip (negative) - see <see cref="InventoryComponent"/>.</summary>
    public void AdjustEquipmentArmourBonus(int delta) => EquipmentArmourBonus += delta;

    public void GainAdvantage(int number = 1)
    {
        int adv = Advantage + number;
        Advantage = Math.Clamp(adv, 0, AdvantageCap);
    }

    public void ResetAdvantage() => Advantage = 0;

    public void LooseAdvantage() => GainAdvantage(-1);
}
