namespace BlazorRogue.World.Generation;

/// <summary>
/// Identifies what kind of room a <see cref="Room"/> is - see <see cref="Room.Type"/>. Intended
/// for callers that want to choose something (e.g. a floor/wall tileset) based on room shape,
/// without the carving code itself needing to know anything about tilesets.
/// </summary>
enum RoomType
{
    Rectangular,
    Overlaid,
    Cave,
    Circular,
}
