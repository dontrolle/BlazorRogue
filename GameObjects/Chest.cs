using System;

namespace BlazorRogue.GameObjects;

class Chest(int x, int y, string id, InventoryComponent content)
    : GameObject(
        x,
        y,
        References.Configuration.StaticDecorativeObjectTypes[id].Name,
        useableComponent: new UseableComponent(Use),
        inventoryComponent: content
    )
{
    readonly string id = id;

    public override string InfoText =>
        $"{References.Configuration.StaticDecorativeObjectTypes[id].InfoText} ({ChestStateToString(this)})";

    internal enum ChestState
    {
        Closed,
        Open,
    }

    public ChestState State = ChestState.Closed;

    static string ChestStateToString(Chest chest) =>
        chest.State switch
        {
            ChestState.Closed => "Closed",
            ChestState.Open => $"{chest.InventoryComponent!.Gold} gold",
            _ => throw new InvalidOperationException($"Unknown ChestState: {chest.State}"),
        };

    static void Use(GameObject go)
    {
        if (go is Chest chest)
        {
            if (chest.InventoryComponent == null)
            {
                throw new InvalidOperationException("Chests should have an inventory.");
            }

            switch (chest.State)
            {
                case ChestState.Closed:
                    chest.State = ChestState.Open;
                    References.SoundManager.PlayDoorSound(true);
                    break;
                case ChestState.Open:
                    if (chest.InventoryComponent.Gold > 0)
                    {
                        References.Map.Player.InventoryComponent!.Gold += chest
                            .InventoryComponent
                            .Gold;
                        chest.InventoryComponent.Gold = 0;
                        References.SoundManager.PlayPickupMoney();
                    }
                    else
                    {
                        chest.State = ChestState.Closed;
                        References.SoundManager.PlayDoorSound(false);
                    }
                    break;
                default:
                    break;
            }
        }
        else
        {
            throw new InvalidOperationException(
                $"{nameof(Use)} called with {nameof(GameObject)} not of type {nameof(Chest)}."
            );
        }
    }

    public override void Render(Map map)
    {
        var sdot = References.Configuration.StaticDecorativeObjectTypes[id];

        string img = "";
        switch (State)
        {
            case ChestState.Closed:
                img = sdot.ImageVariants["closed"];
                break;
            case ChestState.Open:
                img =
                    InventoryComponent!.Gold > 0
                        ? sdot.ImageVariants["open_full"]
                        : sdot.ImageVariants["open_empty"];

                break;
            default:
                break;
        }

        map.Decorations[X, Y]
            .Add(
                new Decoration(this, img, sdot.ImgFolder)
                {
                    Character = sdot.Character,
                    CharacterColor = sdot.CharacterColor,
                    OnUse = UseableComponent!.Use,
                    MakeCoveringOffsetDecsTransparent = sdot.MakeCoveringOffsetDecsTransparent,
                }
            );
        //TODO:Cleanup
        //// add a separate decoration for onUse (without own graphic) to interact with the door
        //map.Decorations[x, y].Add(new Decoration(this, null) { OnUse = UseableComponent.Use });
    }
}
