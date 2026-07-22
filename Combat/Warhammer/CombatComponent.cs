using System;

namespace BlazorRogue.Combat.Warhammer
{
  public class CombatComponent : Component
  {
    // TODO: AdvantageCap=0 ie, disable Advantage - at least for now
    private const int AdvantageCap = 0;

    private int advantage;
    private int wounds;
    private int HealthGainedByOneStep = 1;

    public int Wounds
    {
      get { return wounds; }
      private set
      {
        // we use MaxWounds as an ultimate upper bound - no wounds value can go beyond that. 
        // Means temporary higher max wounds must be reflected in the Maxwounds field.
        wounds = Math.Min(value, MaxWounds);

        if (Owner != null){
          System.Diagnostics.Debug.WriteLine($"{Owner.Name} now has {wounds}W");
        }

        if (wounds <= 0)
        {
          Owner!.Kill();
        }
      }
    }

    public int MaxWounds {get; private set;}
    public int WeaponSkill { get; private set; }
    public int WeaponDamage { get; private set; }
    public int Toughness { get; private set; }
    public int ToughnessBonus => Toughness / 10;
    public int ArmourPoints { get; private set; }

    public CombatComponent(int weaponSkill, int weaponDamage, int toughness, int armourPoints, int wounds)
    {
      WeaponSkill = weaponSkill;
      WeaponDamage = weaponDamage;
      Toughness = toughness;
      ArmourPoints = armourPoints;
      MaxWounds = wounds;      
      this.wounds = wounds;
    }

    public int Advantage
    {
      get
      {
        return advantage;
      }

      private set
      {
        advantage = value;
        System.Diagnostics.Debug.WriteLine($"{Owner!.Name} now has {Advantage} Advantage");
      }
    }

    public bool IsStarving { get; internal set; } = false;

    public void ApplyDamage(int damage)
    {
      Wounds -= damage - ToughnessBonus - ArmourPoints;
    }

    public void HealByMove()
    {
      Wounds += HealthGainedByOneStep;
    }

    public void GainAdvantage(int number = 1)
    {
      var adv = Advantage + number;
      Advantage = Math.Clamp(adv, 0, AdvantageCap);
    }

    public void ResetAdvantage()
    {
      Advantage = 0;
    }

    public void LooseAdvantage()
    {
      GainAdvantage(-1);
    }
  }
}
