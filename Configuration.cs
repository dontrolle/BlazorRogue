﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BlazorRogue.Entities;

namespace BlazorRogue
{
    public class Configuration
    {
        // TODO Ensure that read files are from output dir, i.e., built files
        const string MonsterFileName = "Data\\monsters.json";
        const string HeroesFileName = "Data\\heroes.json";
        const string FloorSetsFileName = "Data\\floorsets.json";

        private Dictionary<string, MoveableType> monsterTypes = new Dictionary<string, MoveableType>();
        public IReadOnlyDictionary<string, MoveableType> MonsterTypes => monsterTypes;

        private Dictionary<string, MoveableType> heroTypes = new Dictionary<string, MoveableType>();
        public IReadOnlyDictionary<string, MoveableType> HeroTypes => heroTypes;

        private Dictionary<string, FloorSet> standardFloorSets = new Dictionary<string, FloorSet>();
        public IReadOnlyDictionary<string, FloorSet> StandardFloorSets => standardFloorSets;

        private Dictionary<string, FloorSet> specialFloorSets = new Dictionary<string, FloorSet>();
        public IReadOnlyDictionary<string, FloorSet> SpecialFloorSets => specialFloorSets;

        public void Parse() // Task async
        {
            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            };

            ParseDataFile(options, HeroesFileName, "heroes", e => ParseMoveableType(e, heroTypes));
            ParseDataFile(options, MonsterFileName, "monsters", e => ParseMoveableType(e, monsterTypes));
            ParseDataFile(options, FloorSetsFileName, "uf_floor_sets", ParseFloorSetType);
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
            using (var jsonFile = File.OpenRead(fileName))
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonFile, options)) // TODO await ... ParseAsync
                {
                    var root = doc.RootElement.GetProperty(rootProperty);
                    foreach (JsonElement element in root.EnumerateArray())
                    {
                        parseElement(element);
                    }
                }
            }
        }

        private void ParseFloorSetType(JsonElement element)
        {
            string id, imgPrefix, charFloor, charColor;
            bool special;
            var imgFloorList = new List<int>();

            id = element.GetProperty("id").GetString();
            special = element.GetProperty("id").GetBoolean();
            imgPrefix = element.GetProperty("img_prefix").GetString();
            var imgFloorElement = element.GetProperty("img_floor");
            foreach (var no in imgFloorElement.EnumerateArray())
            {
                imgFloorList.Add(no.GetInt32());
            }

            charFloor = element.GetProperty("char_floor").GetString();
            charColor = element.GetProperty("char_color").GetString();


            // TODO Pickup - to match better what I do in DungeonGenerator - consider making
            //      two hashsets (or just arrays/lists) instead, with Tuple<img_prefix, [ img_indexes ], char_floor, char_color>
            //      that's actually easier for me to refactor with.
            var f = new FloorSet(id, imgPrefix, imgFloorList.ToArray(), charFloor, charColor);
            if(special)
            {
                specialFloorSets.Add(id, f);
            }
            else
            {
                standardFloorSets.Add(id, f);
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
