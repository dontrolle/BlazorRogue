using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BlazorRogue.Entities;

namespace BlazorRogue
{
    /// <summary>
    /// Parse and present json-based configuration for rest of game.
    /// </summary>
    /// <remarks>
    /// Designed for tilesets from Ultimate Fantasy set, right now.
    /// Not too hard to generalize, though; simply work with together with generalization of <see cref="TileSet"/> to point only to images for tiles 
    /// and their positions.
    /// 
    /// Allows only a single character + color for a tileset.
    /// </remarks>
    public class Configuration
    {
        // TODO Ensure that read files are from output dir, i.e., built files
        const string MonsterFileName = "Data\\monsters.json";
        const string HeroesFileName = "Data\\heroes.json";
        const string FloorSetsFileName = "Data\\floorsets.json";
        const string WallSetsFileName = "Data\\wallsets.json";
        const string DecorationsFileName = "Data\\decorations.json";

        private readonly Dictionary<string, MoveableType> monsterTypes = new Dictionary<string, MoveableType>();
        public IReadOnlyDictionary<string, MoveableType> MonsterTypes => monsterTypes;

        private readonly Dictionary<string, MoveableType> heroTypes = new Dictionary<string, MoveableType>();
        public IReadOnlyDictionary<string, MoveableType> HeroTypes => heroTypes;

        private readonly Dictionary<string, StaticDecorativeObjectType> staticDecorativeObjectTypes = new Dictionary<string, StaticDecorativeObjectType>();
        public IReadOnlyDictionary<string, StaticDecorativeObjectType> StaticDecorativeObjectTypes => staticDecorativeObjectTypes;

        private readonly HashSet<string> floorSetIds = new HashSet<string>();

        private readonly List<TileSet> standardFloorSets = new List<TileSet>();
        public IEnumerable<TileSet> StandardFloorSets => standardFloorSets.AsReadOnly();

        private readonly List<TileSet> specialFloorSets = new List<TileSet>();
        public IEnumerable<TileSet> SpecialFloorSets => specialFloorSets.AsReadOnly();

        private readonly HashSet<string> wallSetIds = new HashSet<string>();

        private readonly List<TileSet> dungeonWallSets = new List<TileSet>();
        public IEnumerable<TileSet> DungeonWallSets => dungeonWallSets.AsReadOnly();

        private readonly List<TileSet> caveWallSets = new List<TileSet>();
        public IEnumerable<TileSet> CaveWallSets => caveWallSets.AsReadOnly();

        public void Parse() // Task async
        {
            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            };

            ParseDataFile(options, HeroesFileName, "heroes", e => ParseMoveableType(e, heroTypes));
            ParseDataFile(options, MonsterFileName, "monsters", e => ParseMoveableType(e, monsterTypes));
            ParseDataFile(options, FloorSetsFileName, "uf_floor_sets", ParseFloorSetType);
            ParseDataFile(options, WallSetsFileName, "uf_wall_sets", ParseWallSetType);
            ParseDataFile(options, DecorationsFileName, "static_decorations", ParseStaticDecorativeType);
        }

        /// <summary>
        /// Parse JSON data file. Expects a single type of assets in the file, and a named root property given in <paramref name="rootProperty"/> 
        /// with an array of elements inside parsable by <paramref name="parseElement"/>.
        /// </summary>
        /// <param name="options">JsonDocumentOptions for JsonDocument.Parse().</param>
        /// <param name="fileName">Name of data file to parse.</param>
        /// <param name="rootProperty">Root property in JSON file to look for.</param>
        /// <param name="parseElement">Parse function for each element.</param>
        private static void ParseDataFile(JsonDocumentOptions options, string fileName, string rootProperty, Action<JsonElement> parseElement)
        {
            using var jsonFile = File.OpenRead(fileName);
            using JsonDocument doc = JsonDocument.Parse(jsonFile, options);
            var root = doc.RootElement.GetProperty(rootProperty);
            foreach (JsonElement element in root.EnumerateArray())
            {
                parseElement(element);
            }
        }

        private void ParseFloorSetType(JsonElement element)
        {
            string id, imgPrefix, charFloor, charColor;
            bool special;
            var imgFloorList = new List<int>();

            id = element.GetProperty("id").GetString();
            special = element.GetProperty("special").GetBoolean();
            imgPrefix = element.GetProperty("img_prefix").GetString();
            var imgFloorElement = element.GetProperty("img_floor");
            foreach (var no in imgFloorElement.EnumerateArray())
            {
                imgFloorList.Add(no.GetInt32());
            }

            charFloor = element.GetProperty("character").GetString();
            charColor = element.GetProperty("char_color").GetString();

            var t = new TileSet(id, TileType.Floor, imgPrefix, imgFloorList.ToArray(), null, charFloor, charColor);

            if (!floorSetIds.Add(id))
            {
                throw new Exception($"Found another floor-set with id: {id}.");
            }

            if (special)
            {
                specialFloorSets.Add(t);
            }
            else
            {
                standardFloorSets.Add(t);
            }
        }

        private void ParseWallSetType(JsonElement element)
        {
            string id, imgPrefix, character, charColor;
            string levelType;
            var imgWallWithoutFrontList = new List<int>();
            var imgWallWeightsList = new List<double>();

            id = element.GetProperty("id").GetString();
            levelType = element.GetProperty("level_type").GetString();
            imgPrefix = element.GetProperty("img_prefix").GetString();

            ParseIndexAndWeights(element, "img_base", imgWallWithoutFrontList, imgWallWeightsList);

            character = element.GetProperty("character").GetString();
            charColor = element.GetProperty("character_color").GetString();

            var t = new TileSet(id, TileType.Wall, imgPrefix, imgWallWithoutFrontList.ToArray(), imgWallWeightsList.ToArray(), character, charColor);

            if (!wallSetIds.Add(id))
            {
                throw new Exception($"Found another wall-set with id: {id}.");
            }

            if (levelType == "dungeon")
            {
                dungeonWallSets.Add(t);
            }
            else if (levelType == "cave")
            {
                caveWallSets.Add(t);
            }
            else
            {
                throw new Exception($"Unknown level_type '{levelType}' in wall-set with id: {id}.");
            }
        }

        private static void ParseIndexAndWeights(JsonElement element, string imgName, List<int> imgArray, List<double> imgWeightArray)
        {
            var indexAndWeights = element.GetProperty(imgName);
            foreach (var indexAndWeight in indexAndWeights.EnumerateArray())
            {
                if (indexAndWeight.GetArrayLength() != 2)
                {
                    throw new Exception($"All elements of {imgName} must be tuples of [<index>,<weight>], i.e., JSON arrays of length 2.");
                }

                imgArray.Add(indexAndWeight[0].GetInt32());
                imgWeightArray.Add(indexAndWeight[1].GetDouble());
            }
        }

        private static void ParseMoveableType(JsonElement element, Dictionary<string, MoveableType> moveableDictionary)
        {
            ParseMoveable(
                element, 
                out string id, 
                out string name, 
                out int weaponSkill, 
                out int weaponDamage, 
                out int toughness, 
                out int armour, 
                out int wounds, 
                out string animationClass, 
                out string character, 
                out string characterColor);

            var m = new MoveableType(id, name, animationClass, character, characterColor, weaponSkill, weaponDamage, toughness, armour, wounds);
            moveableDictionary.Add(id, m);
        }

        private static void ParseMoveable(
            JsonElement element, 
            out string id, 
            out string name, 
            out int weaponSkill, 
            out int weaponDamage, 
            out int toughness, 
            out int armour, 
            out int wounds, 
            out string animationClass, 
            out string character, 
            out string characterColor)
        {
            id = element.GetProperty("id").GetString();
            name = element.GetProperty("name").GetString();
            weaponSkill = element.GetProperty("weaponSkill").GetInt32();
            weaponDamage = element.GetProperty("weaponDamage").GetInt32();
            toughness = element.GetProperty("toughness").GetInt32();
            armour = element.GetProperty("armour").GetInt32();
            wounds = element.GetProperty("wounds").GetInt32();
            animationClass = element.GetProperty("animationClass").GetString();
            character = element.GetProperty("character").GetString();
            characterColor = element.GetProperty("character_color").GetString();
        }

        private void ParseStaticDecorativeType(JsonElement element)
        {
            string id = element.GetProperty("id").GetString();
            string name = element.GetProperty("name").GetString();
            string image = element.GetProperty("image").GetString();
            string infoText = element.GetProperty("info_text").GetString();
            int verticalOffset = element.GetProperty("vertical_offset").GetInt32();
            string character = element.GetProperty("character").GetString();
            string characterColor = element.GetProperty("character_color").GetString();

            var dec = new StaticDecorativeObjectType(id, name, image, infoText, verticalOffset, character, characterColor);
            staticDecorativeObjectTypes.Add(id, dec); 
        }
    }
}
