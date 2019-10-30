using System.Collections.Generic;

using System.Text.Json;

using System.IO;

using BlazorRogue.Entities;

namespace BlazorRogue
{
    public class Configuration
    {
        // TODO Ensure that read files are from output dir
        const string MonsterFileName = "Data\\monsters.json";
        const string HeroesFileName = "Data\\heroes.json";

        public Dictionary<string, MonsterType> monsterTypes = new Dictionary<string, MonsterType>();
        public IReadOnlyDictionary<string, MonsterType> MonsterTypes => monsterTypes;

        public Dictionary<string, MonsterType> heroTypes = new Dictionary<string, MonsterType>();
        public IReadOnlyDictionary<string, MonsterType> HeroTypes => heroTypes;

        public void Parse() // Task async
        {
            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            };

            // parse monster and hero types

            using (var monsterJson = File.OpenRead(MonsterFileName))
            {
                using (JsonDocument doc = JsonDocument.Parse(monsterJson, options)) // TODO await ... ParseAsync
                {
                    var root = doc.RootElement.GetProperty("monsters");
                    foreach (JsonElement element in root.EnumerateArray())
                    {
                        string id, name, animationClass, asciiCharacter, asciiColour;
                        int weaponSkill, weaponDamage, toughness, armour, wounds;
                        ParseMoveable(element, out id, out name, out weaponSkill, out weaponDamage, out toughness, out armour, out wounds, out animationClass, out asciiCharacter, out asciiColour);

                        var newMonsterType = new MonsterType(id, name, animationClass, asciiCharacter, asciiColour, weaponSkill, weaponDamage, toughness, armour, wounds);

                        monsterTypes.Add(id, newMonsterType);
                    }
                }
            }

            using (var heroesJson = File.OpenRead(HeroesFileName))
            {
                using (JsonDocument doc = JsonDocument.Parse(heroesJson, options)) // TODO await ... ParseAsync
                {
                    var root = doc.RootElement.GetProperty("heroes");
                    foreach (JsonElement element in root.EnumerateArray())
                    {
                        string id, name, animationClass, asciiCharacter, asciiColour;
                        int weaponSkill, weaponDamage, toughness, armour, wounds;
                        ParseMoveable(element, out id, out name, out weaponSkill, out weaponDamage, out toughness, out armour, out wounds, out animationClass, out asciiCharacter, out asciiColour);

                        var h = new MonsterType(id, name, animationClass, asciiCharacter, asciiColour, weaponSkill, weaponDamage, toughness, armour, wounds);

                        heroTypes.Add(id, h);
                    }
                }
            }
        }

        private static void ParseMoveable(JsonElement element, out string id, out string name, out int weaponSkill, out int weaponDamage, out int toughness, out int armour, out int wounds, out string animationClass, out string asciiCharacter, out string asciiColour)
        {
            id = element.GetProperty("id").GetString();
            name = element.GetProperty("name").GetString();
            weaponSkill = element.GetProperty("weaponSkill").GetInt32();
            weaponDamage = element.GetProperty("weaponDamage").GetInt32();
            toughness = element.GetProperty("toughness").GetInt32();
            armour = element.GetProperty("armour").GetInt32();
            wounds = element.GetProperty("wounds").GetInt32();
            animationClass = element.GetProperty("animationClass").GetString();
            asciiCharacter = element.GetProperty("asciiCharacter").GetString();
            asciiColour = element.GetProperty("asciiColour").GetString();
        }
    }

}
