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

        public Dictionary<string, MoveableType> monsterTypes = new Dictionary<string, MoveableType>();
        public IReadOnlyDictionary<string, MoveableType> MonsterTypes => monsterTypes;

        public Dictionary<string, MoveableType> heroTypes = new Dictionary<string, MoveableType>();
        public IReadOnlyDictionary<string, MoveableType> HeroTypes => heroTypes;

        public void Parse() // Task async
        {
            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            };

            ParseMoveableTypes(options, HeroesFileName, "heroes", heroTypes);

            ParseMoveableTypes(options, MonsterFileName, "monsters", monsterTypes);

            // "uf_floor_sets"
        }

        private static void ParseMoveableTypes(JsonDocumentOptions options, string fileName, string rootProperty, Dictionary<string, MoveableType> moveableDictionary)
        {
            using (var jsonFile = File.OpenRead(fileName))
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonFile, options)) // TODO await ... ParseAsync
                {
                    var root = doc.RootElement.GetProperty(rootProperty);
                    foreach (JsonElement element in root.EnumerateArray())
                    {
                        ParseMoveableType(element, moveableDictionary);
                    }
                }
            }
        }


        private void ParseMonsterTypes(JsonDocumentOptions options, string fileName, string rootProperty, Dictionary<string, MoveableType> moveableDictionary)
        {
            using (var jsonFile = File.OpenRead(fileName))
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonFile, options)) // TODO await ... ParseAsync
                {
                    var root = doc.RootElement.GetProperty(rootProperty);
                    foreach (JsonElement element in root.EnumerateArray())
                    {
                        ParseMoveableType(element, moveableDictionary);
                    }
                }
            }
        }

        private static void ParseMoveableType(JsonElement element, Dictionary<string, MoveableType> moveableDictionary)
        {
            string id, name, animationClass, asciiCharacter, asciiColour;
            int weaponSkill, weaponDamage, toughness, armour, wounds;
            ParseMoveable(element, out id, out name, out weaponSkill, out weaponDamage, out toughness, out armour, out wounds, out animationClass, out asciiCharacter, out asciiColour);

            var m = new MoveableType(id, name, animationClass, asciiCharacter, asciiColour, weaponSkill, weaponDamage, toughness, armour, wounds);

            moveableDictionary.Add(id, m);
        }

        private void ParseMonsterType(JsonElement element, Dictionary<string, MoveableType> moveableDictionary)
        {
            string id, name, animationClass, asciiCharacter, asciiColour;
            int weaponSkill, weaponDamage, toughness, armour, wounds;
            ParseMoveable(element, out id, out name, out weaponSkill, out weaponDamage, out toughness, out armour, out wounds, out animationClass, out asciiCharacter, out asciiColour);

            var h = new MoveableType(id, name, animationClass, asciiCharacter, asciiColour, weaponSkill, weaponDamage, toughness, armour, wounds);

            moveableDictionary.Add(id, h);
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
