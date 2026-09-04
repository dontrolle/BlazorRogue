using System.Collections.Generic;
using BlazorRogue.Entities;

namespace BlazorRogue.Components;

/// <summary>
/// One lettered inventory slot. A <see cref="ItemKind.UseOnce"/> entry can have <see cref="Count"/>
/// &gt; 1 (identical pickups stack); a <see cref="ItemKind.Equipable"/> entry never stacks - a
/// second copy gets its own letter - but can be independently <see cref="IsEquipped"/>.
/// </summary>
sealed class InventoryEntry(ItemType itemType)
{
    public ItemType ItemType { get; } = itemType;
    public int Count { get; internal set; } = 1;
    public bool IsEquipped { get; internal set; }
}

class InventoryComponent : Component
{
    public int Gold { internal set; get; }

    readonly SortedDictionary<char, InventoryEntry> items = [];
    public IReadOnlyDictionary<char, InventoryEntry> Items => items;

    /// <summary>
    /// Adds one unit of <paramref name="itemType"/>: stacks onto an existing letter for a
    /// <see cref="ItemKind.UseOnce"/> item already held, otherwise assigns the first free a-z
    /// letter. Returns false - no letter assigned, nothing added - when every letter is taken; the
    /// caller is expected to leave the item on the floor in that case.
    /// </summary>
    public bool TryPickUp(ItemType itemType, out char letter)
    {
        if (itemType.Kind == ItemKind.UseOnce)
        {
            foreach (var (existingLetter, entry) in items)
            {
                if (entry.ItemType.Id == itemType.Id)
                {
                    entry.Count++;
                    letter = existingLetter;
                    return true;
                }
            }
        }

        for (char candidate = 'a'; candidate <= 'z'; candidate++)
        {
            if (!items.ContainsKey(candidate))
            {
                items.Add(candidate, new InventoryEntry(itemType));
                letter = candidate;
                return true;
            }
        }

        letter = default;
        return false;
    }

    /// <summary>
    /// Removes the whole entry at <paramref name="letter"/> (dropping it), unequipping it first if
    /// it was equipped. Returns null if there's no entry at that letter.
    /// </summary>
    public ItemType? Remove(char letter)
    {
        if (!items.TryGetValue(letter, out var entry))
        {
            return null;
        }

        if (entry.IsEquipped)
        {
            ToggleEquip(letter);
        }

        _ = items.Remove(letter);
        return entry.ItemType;
    }

    /// <summary>
    /// Uses (<see cref="ItemKind.UseOnce"/>: applies its effect and shrinks the stack by one,
    /// freeing the letter at zero) or toggles equipped (<see cref="ItemKind.Equipable"/>) the entry
    /// at <paramref name="letter"/>. Returns null if there's no entry at that letter.
    /// </summary>
    public ItemType? Use(char letter)
    {
        if (!items.TryGetValue(letter, out var entry))
        {
            return null;
        }

        if (entry.ItemType.Kind == ItemKind.Equipable)
        {
            ToggleEquip(letter);
            return entry.ItemType;
        }

        ApplyEffect(entry.ItemType, sign: 1);
        entry.Count--;
        if (entry.Count <= 0)
        {
            _ = items.Remove(letter);
        }
        return entry.ItemType;
    }

    void ToggleEquip(char letter)
    {
        var entry = items[letter];
        entry.IsEquipped = !entry.IsEquipped;
        ApplyEffect(entry.ItemType, sign: entry.IsEquipped ? 1 : -1);
    }

    // sign flips an Equipable's bonus off again on unequip; meaningless for (one-shot) UseOnce
    // effects, which only ever apply once, on use.
    void ApplyEffect(ItemType itemType, int sign)
    {
        switch (itemType.EffectKind)
        {
            case ItemEffectKind.Heal:
                Owner!.CombatComponent!.Heal(itemType.EffectMagnitude);
                break;
            case ItemEffectKind.ArmourBonus:
                Owner!.CombatComponent!.AdjustEquipmentArmourBonus(sign * itemType.EffectMagnitude);
                break;
            default:
                break;
        }
    }
}
