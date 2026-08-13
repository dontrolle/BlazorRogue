using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BlazorRogue.World;

namespace BlazorRogue.Entities;

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
class Configuration
{
    static readonly string MonsterFileName = Path.Combine(
        AppContext.BaseDirectory,
        "Data",
        "monsters.json"
    );
    static readonly string HeroesFileName = Path.Combine(
        AppContext.BaseDirectory,
        "Data",
        "heroes.json"
    );
    static readonly string FloorSetsFileName = Path.Combine(
        AppContext.BaseDirectory,
        "Data",
        "floorsets.json"
    );
    static readonly string WallSetsFileName = Path.Combine(
        AppContext.BaseDirectory,
        "Data",
        "wallsets.json"
    );
    static readonly string DecorationsFileName = Path.Combine(
        AppContext.BaseDirectory,
        "Data",
        "decorations.json"
    );
    static readonly string LevelsFileName = Path.Combine(
        AppContext.BaseDirectory,
        "Data",
        "levels.json"
    );

    readonly Dictionary<string, MoveableType> monsterTypes = [];
    public IReadOnlyDictionary<string, MoveableType> MonsterTypes => monsterTypes;

    readonly Dictionary<string, MoveableType> heroTypes = [];
    public IReadOnlyDictionary<string, MoveableType> HeroTypes => heroTypes;

    readonly Dictionary<string, StaticDecorativeObjectType> staticDecorativeObjectTypes = [];
    public IReadOnlyDictionary<string, StaticDecorativeObjectType> StaticDecorativeObjectTypes =>
        staticDecorativeObjectTypes;

    readonly HashSet<string> floorSetIds = [];

    readonly List<TileSet> standardFloorSets = [];
    public IEnumerable<TileSet> StandardFloorSets => standardFloorSets.AsReadOnly();

    readonly List<TileSet> specialFloorSets = [];
    public IEnumerable<TileSet> SpecialFloorSets => specialFloorSets.AsReadOnly();

    readonly HashSet<string> wallSetIds = [];

    readonly List<TileSet> dungeonWallSets = [];
    public IEnumerable<TileSet> DungeonWallSets => dungeonWallSets.AsReadOnly();

    readonly List<TileSet> caveWallSets = [];
    public IEnumerable<TileSet> CaveWallSets => caveWallSets.AsReadOnly();

    readonly Dictionary<int, LevelConfiguration> levels = [];
    public IReadOnlyDictionary<int, LevelConfiguration> Levels => levels.AsReadOnly();

    const string DefaultStaticDecorationImgFolder = "uf_terrain";

    public void Parse() // Task async
    {
        var options = new JsonDocumentOptions { AllowTrailingCommas = true };

        ParseDataFile(options, HeroesFileName, "heroes", e => ParseMoveableType(e, heroTypes));
        ParseDataFile(
            options,
            MonsterFileName,
            "monsters",
            e => ParseMoveableType(e, monsterTypes)
        );
        ParseDataFile(options, FloorSetsFileName, "uf_floor_sets", ParseFloorSetType);
        ParseDataFile(options, WallSetsFileName, "uf_wall_sets", ParseWallSetType);
        ParseDataFile(
            options,
            DecorationsFileName,
            "static_decorations",
            ParseStaticDecorativeType
        );
        ParseDataFile(options, LevelsFileName, "levels", ParseLevelConfiguration);
        foreach (var level in levels)
        {
            if (!MapGeneratorFactory.IsKnown(level.Value.MapGeneratorId))
            {
                throw new InvalidOperationException(
                    $"Level '{level.Value.Id}' references unknown map generator id '{level.Value.MapGeneratorId}'."
                );
            }
        }
    }

    /// <summary>
    /// Parse JSON data file. Expects a single type of assets in the file, and a named root property given in <paramref name="rootProperty"/>
    /// with an array of elements inside parsable by <paramref name="parseElement"/>.
    /// </summary>
    /// <param name="options">JsonDocumentOptions for JsonDocument.Parse().</param>
    /// <param name="fileName">Name of data file to parse.</param>
    /// <param name="rootProperty">Root property in JSON file to look for.</param>
    /// <param name="parseElement">Parse function for each element.</param>
    static void ParseDataFile(
        JsonDocumentOptions options,
        string fileName,
        string rootProperty,
        Action<JsonElement> parseElement
    )
    {
        using var jsonFile = File.OpenRead(fileName);
        using var doc = JsonDocument.Parse(jsonFile, options);
        var root = doc.RootElement.GetProperty(rootProperty);
        foreach (var element in root.EnumerateArray())
        {
            parseElement(element);
        }
    }

    void ParseLevelConfiguration(JsonElement element)
    {
        int number,
            height,
            width;
        string id,
            name,
            mapGeneratorId;
        SettingsMap settingsMap;

        number = GetRequiredInt(element, "no");
        height = GetRequiredInt(element, "height");
        width = GetRequiredInt(element, "width");
        id = GetRequiredString(element, "id");
        name = GetRequiredString(element, "name");
        var mapGeneratorElement = element.GetProperty("map_generator");
        mapGeneratorId = GetRequiredString(mapGeneratorElement, "generator_id");
        settingsMap = ParseSettingsMap(mapGeneratorElement.GetProperty("parameters"));

        var levelConfig = new LevelConfiguration(
            number,
            id,
            name,
            height,
            width,
            mapGeneratorId,
            settingsMap
        );

        if (!levels.TryAdd(number, levelConfig))
        {
            throw new InvalidOperationException(
                $"Found another level numbered : {number}. Level numbers should be assigned uniquely."
            );
        }
    }

    /// <summary>
    /// Recursively parses a JSON object into a <see cref="SettingsMap"/>. A whole-number JSON
    /// value becomes an int, any other number a double, and a nested object recurses into a
    /// nested SettingsMap - arrays and booleans aren't supported, since map-generator parameters
    /// have no use for them yet.
    /// </summary>
    static SettingsMap ParseSettingsMap(JsonElement element)
    {
        var values = new Dictionary<string, object>();
        foreach (var property in element.EnumerateObject())
        {
            values[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.Number when property.Value.TryGetInt32(out int i) => i,
                JsonValueKind.Number => property.Value.GetDouble(),
                JsonValueKind.String => RequireNonNullString(property.Value, property.Name),
                JsonValueKind.Object => ParseSettingsMap(property.Value),
                var kind => throw new InvalidOperationException(
                    $"Setting '{property.Name}' has unsupported JSON kind '{kind}' - only numbers, strings and nested objects are allowed."
                ),
            };
        }
        return new SettingsMap(values);
    }

    void ParseFloorSetType(JsonElement element)
    {
        string id,
            imgPrefix,
            charFloor,
            charColor;
        bool special;
        var imgFloorList = new List<int>();

        id = GetRequiredString(element, "id");
        special = element.GetProperty("special").GetBoolean();
        imgPrefix = GetRequiredString(element, "img_prefix");
        GetIntArray(element, "img_floor", imgFloorList);
        charFloor = GetRequiredString(element, "character");
        charColor = GetRequiredString(element, "character_color");

        var t = new TileSet(
            id,
            TileType.Floor,
            imgPrefix,
            [.. imgFloorList],
            null,
            character: charFloor,
            characterColor: charColor
        );

        if (!floorSetIds.Add(id))
        {
            throw new InvalidOperationException($"Found another floor-set with id: {id}.");
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

    void ParseWallSetType(JsonElement element)
    {
        string id,
            imgPrefix,
            character,
            charColor;
        string levelType;
        var imgBaseIndexes = new List<int>();
        var imgBaseWeights = new List<double>();
        var imgBaseEdgeSouthIndexes = new List<int>();
        var imgBaseEdgeSouthWeights = new List<double>();
        var imgSimpleEdgeNorthIndexes = new List<int>();
        var imgDecoratedEdgeNorthIndexes = new List<int>();

        id = GetRequiredString(element, "id");
        levelType = GetRequiredString(element, "level_type");
        imgPrefix = GetRequiredString(element, "img_prefix");

        ParseIndexAndWeights(element, "img_base", imgBaseIndexes, imgBaseWeights);
        ParseIndexAndWeights(
            element,
            "img_base_edge_south",
            imgBaseEdgeSouthIndexes,
            imgBaseEdgeSouthWeights
        );
        GetIntArray(element.GetProperty("img_edge_north"), "simple", imgSimpleEdgeNorthIndexes);
        GetIntArray(
            element.GetProperty("img_edge_north"),
            "decorated",
            imgDecoratedEdgeNorthIndexes
        );

        character = GetRequiredString(element, "character");
        charColor = GetRequiredString(element, "character_color");

        var t = new TileSet(
            id,
            TileType.Wall,
            imgPrefix,
            [.. imgBaseIndexes],
            [.. imgBaseWeights],
            [.. imgBaseEdgeSouthIndexes],
            [.. imgBaseEdgeSouthWeights],
            [.. imgSimpleEdgeNorthIndexes],
            [.. imgDecoratedEdgeNorthIndexes],
            character: character,
            characterColor: charColor
        );

        if (!wallSetIds.Add(id))
        {
            throw new InvalidOperationException($"Found another wall-set with id: {id}.");
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
            throw new InvalidOperationException(
                $"Unknown level_type '{levelType}' in wall-set with id: {id}."
            );
        }
    }

    static void GetIntArray(JsonElement element, string property, List<int> listToFill)
    {
        var imgFloorElement = element.GetProperty(property);
        listToFill.AddRange(imgFloorElement.EnumerateArray().Select(no => no.GetInt32()));
    }

    /// <summary>
    /// Get the string value of a required JSON property. Throws if the property's value is JSON null,
    /// since the data files aren't expected to ever supply null for these fields.
    /// </summary>
    static string GetRequiredString(JsonElement element, string property) =>
        RequireNonNullString(element.GetProperty(property), property);

    static string RequireNonNullString(JsonElement stringElement, string propertyNameForError) =>
        stringElement.GetString()
        ?? throw new InvalidOperationException(
            $"Property '{propertyNameForError}' was expected to have a non-null string value."
        );

    static int GetRequiredInt(JsonElement element, string property) =>
        RequireNonNullInt(element.GetProperty(property), property);

    static int RequireNonNullInt(JsonElement intElement, string propertyNameForError)
    {
        try
        {
            return intElement.GetInt32();
        }
        catch
        {
            throw new InvalidOperationException(
                $"Property '{propertyNameForError}' was expected to have a non-null int value."
            );
        }
    }

    static void ParseIndexAndWeights(
        JsonElement element,
        string imgName,
        List<int> imgArray,
        List<double> imgWeightArray
    )
    {
        var indexAndWeights = element.GetProperty(imgName);
        foreach (var indexAndWeight in indexAndWeights.EnumerateArray())
        {
            if (indexAndWeight.GetArrayLength() != 2)
            {
                throw new InvalidOperationException(
                    $"All elements of {imgName} must be tuples of [<index>,<weight>], i.e., JSON arrays of length 2."
                );
            }

            imgArray.Add(indexAndWeight[0].GetInt32());
            imgWeightArray.Add(indexAndWeight[1].GetDouble());
        }
    }

    static void ParseMoveableType(
        JsonElement element,
        Dictionary<string, MoveableType> moveableDictionary
    )
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
            out string characterColor
        );

        var m = new MoveableType(
            id,
            name,
            animationClass,
            character,
            characterColor,
            weaponSkill,
            weaponDamage,
            toughness,
            armour,
            wounds
        );
        moveableDictionary.Add(id, m);
    }

    static void ParseMoveable(
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
        out string characterColor
    )
    {
        id = GetRequiredString(element, "id");
        name = GetRequiredString(element, "name");
        weaponSkill = element.GetProperty("weaponSkill").GetInt32();
        weaponDamage = element.GetProperty("weaponDamage").GetInt32();
        toughness = element.GetProperty("toughness").GetInt32();
        armour = element.GetProperty("armour").GetInt32();
        wounds = element.GetProperty("wounds").GetInt32();
        animationClass = GetRequiredString(element, "animationClass");
        character = GetRequiredString(element, "character");
        characterColor = GetRequiredString(element, "character_color");
    }

    void ParseStaticDecorativeType(JsonElement element)
    {
        string id = GetRequiredString(element, "id");
        string name = GetRequiredString(element, "name");

        // Yeah, allowing image to be both a singleton, an array, and an object is me playing around :P
        var images = new Dictionary<string, string>();
        const string imagePropertyName = "image";
        var imageProperty = element.GetProperty(imagePropertyName);
        if (imageProperty.ValueKind == JsonValueKind.Object)
        {
            /* Expected format
             * {
             *  "<tag>" : "<imagefilenameWithoutPng>",
             *  ...
             * }
             */

            foreach (var iElem in imageProperty.EnumerateObject())
            {
                images.Add(
                    iElem.Name,
                    RequireNonNullString(iElem.Value, $"{imagePropertyName}.{iElem.Name}")
                );
            }
        }
        else if (imageProperty.ValueKind == JsonValueKind.Array)
        {
            /* Expected format
             * [
             *  "<imagefilenameWithoutPng>",
             *  ...
             * ]
             *
             * Tags will be set to "0", "1", ...
             */

            int counter = 0;
            foreach (var iElem in imageProperty.EnumerateArray())
            {
                images.Add($"{counter}", RequireNonNullString(iElem, imagePropertyName));
                counter++;
            }
        }
        else if (imageProperty.ValueKind == JsonValueKind.String)
        {
            /* Expected format
             *  "<imagefilenameWithoutPng>"
             *
             * Tag will be set to ""
             */

            images.Add("", RequireNonNullString(imageProperty, imagePropertyName));
        }
        else
        {
            throw new InvalidOperationException(
                $"{imagePropertyName} should be either a single string, an array of strings, or an object with string, string pairs."
            );
        }

        string infoText = GetRequiredString(element, "info_text");
        int verticalOffset = element.GetProperty("vertical_offset").GetInt32();
        string character = GetRequiredString(element, "character");
        string characterColor = GetRequiredString(element, "character_color");

        string imgFolder = DefaultStaticDecorationImgFolder;
        if (element.TryGetProperty("img_folder", out var imgFolderElement))
        {
            imgFolder = RequireNonNullString(imgFolderElement, "img_folder");
        }

        bool blocking = false;
        if (element.TryGetProperty("blocking", out var blockingElement))
        {
            blocking = blockingElement.GetBoolean();
        }

        bool makeCoveringOffsetDecsTransparent = false;
        if (
            element.TryGetProperty(
                "makeCoveringOffsetDecsTransparent",
                out var makeCoveringOffsetDecsTransparentElement
            )
        )
        {
            makeCoveringOffsetDecsTransparent =
                makeCoveringOffsetDecsTransparentElement.GetBoolean();
        }

        var dec = new StaticDecorativeObjectType(
            id,
            name,
            images,
            infoText,
            verticalOffset,
            character,
            characterColor,
            blocking,
            imgFolder,
            makeCoveringOffsetDecsTransparent
        );
        staticDecorativeObjectTypes.Add(id, dec);
    }
}
