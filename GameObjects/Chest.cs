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

        public int Content { get; set; }

        public enum ChestState
        {
            Closed,
            OpenFull,
            OpenEmpty
        }

        public ChestState State = ChestState.Closed;

        public Chest(
            int x, 
            int y,
            string id,
            int content) : base(x, y, References.Configuration.StaticDecorativeObjectTypes[id].Name, useableComponent: new UseableComponent(Use))
        {
            Id = id;
            Content = content;
        }

        private static void Use(GameObject go)
        {
            if (go is Chest chest)
            {
                switch (chest.State)
                {
                    case ChestState.Closed:
                        if (chest.Content == 0)
                        {
                            chest.State = ChestState.OpenEmpty;
                        }
                        else
                        {
                            chest.State = ChestState.OpenFull;
                        }

                        break;
                    case ChestState.OpenFull:
                        // TODO Distribute some money all over the floor?
                        // TODO Put the money directly in the player inventory?
                        chest.Content = 0;
                        chest.State = ChestState.OpenEmpty;

                        break;
                    case ChestState.OpenEmpty:
                        chest.State = ChestState.Closed;

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
                case ChestState.OpenFull:
                    img = sdot.ImageVariants["open_full"];
                    break;
                case ChestState.OpenEmpty:
                    img = sdot.ImageVariants["open_empty"];
                    break;
            }

            map.Decorations[x, y].Add(new Decoration(this, img) { Character = sdot.Character, CharacterColor = sdot.CharacterColor });

            // add a button (without own graphic) to interact with the door
            map.Decorations[x, y].Add(new Decoration(this, null) { OnClick = UseableComponent.Use });
        }
    }
}
