using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorRogue.AI;
using BlazorRogue.Combat.Warhammer;

namespace BlazorRogue.GameObjects
{
    public class Chest : GameObject
    {
        private readonly string Id;

        public override string InfoText => $"{References.Configuration.StaticDecorativeObjectTypes[Id].InfoText} ({ChestStateToString(this)})";

        public enum ChestState
        {
            Closed,
            Open
        }

        public ChestState State;

        public Chest(
            int x, 
            int y,
            string id,
            InventoryComponent content) : 
            base(
                x, 
                y, 
                References.Configuration.StaticDecorativeObjectTypes[id].Name,
                useableComponent: new UseableComponent(Use),
                inventoryComponent: content)
        {
            Id = id;
            State =  ChestState.Closed;
        }

        private static string ChestStateToString(Chest chest)
        {
            switch (chest.State)
            {
                case ChestState.Closed:
                    return "Closed";
                case ChestState.Open:
                    return $"{chest.InventoryComponent.Gold} gold";
                default:
                    throw new Exception($"Unknown ChestState: {chest.State}");
            }
        }

        private static void Use(GameObject go)
        {
            // TODO OpenFull and OpenEmpty state -> Open; and derive Full and Empty from Inventory

            if (go is Chest chest)
            {
                if (chest.InventoryComponent == null)
                {
                    throw new Exception("Chests should have an inventory.");
                }

                switch (chest.State)
                {
                    case ChestState.Closed:
                        chest.State = ChestState.Open;
                        break;
                    case ChestState.Open:
                        if(chest.InventoryComponent.Gold > 0)
                        {
                            References.Map.Player.InventoryComponent.Gold += chest.InventoryComponent.Gold;
                            chest.InventoryComponent.Gold = 0;
                        }
                        else
                        {
                            chest.State = ChestState.Closed;
                        }
                        break;
                }
            }
            else
            {
                throw new Exception($"{nameof(Chest.Use)} called with {nameof(GameObject)} not of type {nameof(Chest)}.");
            }
        }

        public override void Render(Map map)
        {
            var sdot = References.Configuration.StaticDecorativeObjectTypes[Id];

            var img = "";
            switch (State)
            {
                case ChestState.Closed:
                    img = sdot.ImageVariants["closed"];
                    break;
                case ChestState.Open:
                    if(InventoryComponent.Gold > 0)
                    {
                        img = sdot.ImageVariants["open_full"];
                    }
                    else
                    {
                        img = sdot.ImageVariants["open_empty"];
                    }

                    break;
            }

            map.Decorations[x, y].Add(new Decoration(this, img) { Character = sdot.Character, CharacterColor = sdot.CharacterColor, OnUse = UseableComponent.Use });

            //// add a separate decoration for onUse (without own graphic) to interact with the door
            //map.Decorations[x, y].Add(new Decoration(this, null) { OnUse = UseableComponent.Use });
        }
    }
}
