using System;

namespace BlazorRogue.Combat.Warhammer
{
    public class CombatComponent : Component
    {
        private const int AdvantageCap = 8;

        private int advantage;
        private int wounds;

        public int Wounds
        {
            get { return wounds; }
            private set
            {
                wounds = value;
                if (wounds <= 0)
                {
                    Owner.Kill();
                }
            }
        }

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
            Wounds = wounds;
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
                System.Diagnostics.Debug.WriteLine($"{Owner.Name} now has {Advantage} Advantage");
            }
        }

        public void ApplyDamage(int damage)
        {
            Wounds -= damage - ToughnessBonus - ArmourPoints;
            System.Diagnostics.Debug.WriteLine($"{Owner.Name} now has {Wounds}W");
        }

        public void GainAdvantage(int number = 1)
        {
            var adv = Advantage + number;
            Advantage = Math.Min(adv, AdvantageCap);
        }

        public void ResetAdvantage()
        {
            Advantage = 0;
        }

        public void LooseAdvantage()
        {
            Advantage -= 1;
        }
    }
}
