using System.Collections.Generic;

namespace BlazorRogue.Entities;

class LevelConfiguration(
    int number,
    string id,
    string name,
    int height,
    int width,
    string generatorId,
    Dictionary<string, string> generatorParameters
)
{
    public int Number { get; } = number;
    public string Id { get; } = id;
    public string Name { get; } = name;
    public int Height { get; } = height;
    public int Width { get; } = width;
    public string MapGeneratorId { get; } = generatorId;
    readonly Dictionary<string, string> mapGeneratorParameters = generatorParameters;
    public IReadOnlyDictionary<string, string> MapGeneratorParameters =>
        mapGeneratorParameters.AsReadOnly();
}
