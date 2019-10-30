using System.Collections.Generic;

using System.Text.Json;

using System.IO;

using BlazorRogue.Entities;

namespace BlazorRogue
{
    public class Configuration
    {
        const string MonsterFileName = "Data\\monsters.json";
        public Dictionary<string, MonsterType> monsterTypes = new Dictionary<string, MonsterType>();
        public IReadOnlyDictionary<string, MonsterType> MonsterTypes => monsterTypes;

        public void Parse() // Task async
        {
            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            };

            // parse Monsters

            using (var monsterJson = File.OpenRead(MonsterFileName))
            {
                using (JsonDocument doc = JsonDocument.Parse(monsterJson, options)) // TODO await ... ParseAsync
                {
                    var root = doc.RootElement;
                    foreach (JsonElement element in root.GetProperty("monsters").EnumerateArray())
                    {
                        var id = element.GetProperty("id").GetString();
                        var name = element.GetProperty("name").GetString();
                        var weaponSkill = element.GetProperty("weaponSkill").GetInt32();
                        var weaponDamage = element.GetProperty("weaponDamage").GetInt32();
                        var toughness = element.GetProperty("toughness").GetInt32();
                        var armour = element.GetProperty("armour").GetInt32();
                        var wounds = element.GetProperty("wounds").GetInt32();
                        var animationClass = element.GetProperty("animationClass").GetString();
                        var asciiCharacter = element.GetProperty("asciiCharacter").GetString();
                        var asciiColour = element.GetProperty("asciiColour").GetString();

                        var newMonsterType = new MonsterType(id, name, animationClass, asciiCharacter, asciiColour, weaponSkill, weaponDamage, toughness, armour, wounds);

                        monsterTypes.Add(id, newMonsterType);
                    }
                }
            }
        }
    }

}
