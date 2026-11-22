namespace Cthangover.Mods.Equipment
{
    using System;
    using System.Collections.Generic;
    using Cthangover.Core.Items;
    using Cthangover.Mods.Stats;
    using StorageInventory = Cthangover.Core.Items.Inventory;

    public static partial class EquipmentService
    {
        /// <summary>Equips this exact group or world occurrence, preserving per-item properties and provenance.</summary>
        public static bool EquipStack(string who, string stackId, string slot = null)
        {
            var character = StatsService.Resolve(who);
            if (character == null || Inventory is not StorageInventory owner)
            {
                return false;
            }

            var selected = owner.Stack(stackId);
            if (selected == null || selected.Container != "player" || !CanEquip(character, selected.ItemId, slot, out _))
            {
                return false;
            }

            var target = slot == null ? PickSlot(character, selected.ItemId) : Slot(slot);
            if (target == null)
            {
                return false;
            }

            var tracked = TryStorageSlot(character, target.ID, out _, out _);
            var legacyReturn = tracked ? null : ItemIn(character, target.ID);
            if (!owner.TryEquipStack("equip/" + Guid.NewGuid().ToString("N"), selected.Id, character, target.ID, legacyReturn, out _))
            {
                return false;
            }

            Changed(character);
            return true;
        }

        private static IReadOnlyDictionary<string, string> StorageWorn(string character)
        {
            var legacy = Find(character);
            var worn = legacy == null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : new Dictionary<string,
                string>(legacy, StringComparer.OrdinalIgnoreCase);
            if (Inventory is not StorageInventory owner || character == null)
            {
                return worn;
            }

            var state = owner.CaptureStorage();
            foreach (var grid in state.Grids.Values)
            {
                if (!string.Equals(grid.EquipmentCharacter, character, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                worn.Remove(grid.EquipmentSlot);
                foreach (var row in state.Stacks)
                {
                    if (row.Container == grid.Id)
                    {
                        worn[grid.EquipmentSlot] = row.ItemId;
                    }
                }
            }

            return worn;
        }

        private static bool TryStorageSlot(string character, string slot, out StorageInventory owner, out InventoryStackData group)
        {
            owner = Inventory as StorageInventory;
            group = null;
            if (owner == null || character == null || slot == null)
            {
                return false;
            }

            var state = owner.CaptureStorage();
            foreach (var grid in state.Grids.Values)
            {
                if (!string.Equals(grid.EquipmentCharacter, character, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(grid.EquipmentSlot, slot, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var row in state.Stacks)
                {
                    if (row.Container == grid.Id)
                    {
                        group = row;
                        break;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
