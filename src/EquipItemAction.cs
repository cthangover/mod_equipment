namespace Cthangover.Mods.Equipment
{
    using Cthangover.Core.Items;
    using Cthangover.Core.Settings;
    using Cthangover.Mods.Items;

    /// <summary>
    /// The item action <c>"ItemAction": "action/equip"</c>. With the item type
    /// <c>TargetUsed</c> the inventory's own "apply to whom" picker chooses the party member,
    /// and the inventory takes the item away once this returns true — so gear needs no
    /// equipment screen of its own to be put on. The replaced item comes back to the bag.
    /// </summary>
    public class EquipItemAction : ITargetedItemAction, IInventoryStackAction
    {
        /// <summary>Registered action key used by item blueprints.</summary>
        public string ID
        {
            get
            {
                return "action/equip";
            }
        }

        /// <summary>Equipment requires an explicit character target, so untargeted use refuses.</summary>
        public bool UseAction(IItem item)
        {
            return false;
        }

        /// <summary>Transfers the exact selected occurrence or group unit; the panel does not spend another equal blueprint.</summary>
        public bool UseStack(Inventory inventory, string stackId, CharacterInfoData target)
        {
            return target != null && ReferenceEquals(GameData.Instance.Runtime.Inventory, inventory)
                && EquipmentService.EquipStack(target.ID, stackId);
        }

        /// <summary>Legacy blueprint-only callers use the existing character eligibility rules.</summary>
        public bool UseOn(IItem item, CharacterInfoData target)
        {
            if (item == null || target == null)
                return false;
            // The inventory removes the applied item itself, so the action must not take it too.
            return EquipmentService.Equip(target.ID, item.ID, null, fromInventory: false);
        }
    }
}
