using System;
using BlazorRogue.AI;
using BlazorRogue.Combat.Warhammer;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;

namespace BlazorRogue;

class Moveable : GameObject
{
    public string AnimationClass { get; protected set; }
    public string Id { get; }
    public string AsciiCharacter { get; }
    public string AsciiColour { get; }

    public Moveable(
        int x,
        int y,
        AIComponent? aIComponent,
        MoveableType monsterType,
        InventoryComponent? inventoryComponent = null
    )
        : base(
            x,
            y,
            monsterType.Name,
            aIComponent,
            new CombatComponent(
                monsterType.WeaponSkill,
                monsterType.WeaponDamage,
                monsterType.Toughness,
                monsterType.Armour,
                monsterType.Wounds
            ),
            inventoryComponent: inventoryComponent
        )
    {
        InvisibleOutsideFov = true;
        Blocking = true;
        AnimationClass = monsterType.AnimationClass;
        Id = monsterType.Id;
        AsciiCharacter = monsterType.AsciiCharacter;
        AsciiColour = monsterType.AsciiColour;
        InfoText = Name;

        // Note, can't block light due to the way Moveables are treated in Map
    }

    public Moveable(
        Tuple<int, int> coord,
        AIComponent? aIComponent,
        MoveableType monsterType,
        InventoryComponent? inventoryComponent = null
    )
        : this(coord.Item1, coord.Item2, aIComponent, monsterType, inventoryComponent) { }

    public override void Render(Map map)
    {
        if (AnimationClass == null)
        {
            throw new InvalidOperationException("AnimationClass not set.");
        }

        // Wounds only ever reach zero by way of Kill(), so this is simply "is a corpse". Dead
        // monsters are removed from the map, which leaves the dead player as the case in practice.
        bool isDead = CombatComponent is not null && CombatComponent.Wounds <= 0;

        map.MoveableDecorations[X, Y]
            .Add(
                new Decoration(this, null)
                {
                    AnimationClass = AnimationClass,
                    AnimationPaused = isDead,
                    Character = AsciiCharacter,
                    CharacterColor = AsciiColour,
                }
            );
    }

    public override void Move(int xDelta, int yDelta)
    {
        base.Move(xDelta, yDelta);
        References.SoundManager.PlayWalkSound();
    }
}
