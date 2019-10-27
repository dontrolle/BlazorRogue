using System;
using System.Collections.Generic;

using System.Text.Json;
using System.Text.Json.Serialization;

using System.IO;
using System.Threading.Tasks;

using BlazorRogue.Entities;

namespace BlazorRogue
{
    public class Game
    {
        const int Width = 38;
        const int Height = 30;

        public DungeonGenerator DungeonGenerator { get; private set; } 
        public Map Map { get; private set; }
        public static SoundManager SoundManager { get; set; }
        public FightingSystem FightingSystem { get; private set; }
        
        public Game()
        {
            DungeonGenerator = new DungeonGenerator(Width, Height, this);
            FightingSystem = new FightingSystem(this);
            var config = new Configuration();
            config.Parse(); 
            // TODO pass as async to GenerateMap below 
            // TODO Use monsters in DungeonGenerator
            // TODO Deprecate old classes for monsters and ctor in Monster
            Map = DungeonGenerator.GenerateMap();
        }
    }

    public class Configuration
    {
        const string MonsterFileName = "Data\\monsters.json";
        public IReadOnlyList<MonsterType> MonsterTypes;

        public void Parse()
        {
            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            };

            // parse Monsters

            var monsterTypes = new List<MonsterType>();

            using (var monsterJson = File.OpenRead(MonsterFileName))
            {
                using (JsonDocument doc = JsonDocument.Parse(monsterJson, options)) // TODO async
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

                        var cc = new CombatComponent(weaponSkill, weaponDamage, toughness, armour, wounds);
                        var newMonsterType = new MonsterType(id, name, cc, animationClass, asciiCharacter, asciiColour);

                        monsterTypes.Add(newMonsterType);
                    }
                }
            }

            MonsterTypes = monsterTypes.AsReadOnly();
        }
    }

}
