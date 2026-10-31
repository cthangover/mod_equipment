namespace Cthangover.Mods.Equipment
{
    using System;
    using System.Globalization;
    using System.Linq;
    using Cthangover.Core.UI.Theming;
    using Cthangover.Core.Utils;
    using Cthangover.Mods.Interface.Theming;
    using Cthangover.Mods.Stats;
    using Godot;

    /// <summary>
    /// The Equipment block of the character sheet: one row per slot with what is worn there
    /// (picture, name, what it gives), a Take off button for a worn item and a Replace button
    /// next to it; an empty slot has an Equip button. Both open the same picker under the row:
    /// the bag's items that fit that slot for the shown character (<see cref="EquipmentService.BagCandidates"/>),
    /// one button each. Picking one goes through <see cref="EquipmentService.Equip"/>, which
    /// returns the replaced item to the bag and tells the stats, so the sheet redraws itself.
    /// <para/>
    /// The picker is state of this block (which slot is open, for whom). The sheet rebuilds the
    /// block on every redraw, so the block rebuilds only its own container when the picker opens
    /// or closes, and the open picker is dropped when another character is shown or the sheet is opened again.
    /// </summary>
    public class EquipmentSheetSection : ISheetSection
    {
        private VBoxContainer host;
        private string hostFor;
        private string openSlot;

        /// <inheritdoc />
        public string Id => "equipment";

        /// <inheritdoc />
        public int Order => 10;

        /// <inheritdoc />
        public string TitleKey => "equipment/sheet/title";

        /// <inheritdoc />
        public bool AppliesTo(string characterId) => EquipmentService.Slots.Count > 0;

        /// <inheritdoc />
        public void Build(VBoxContainer container, string characterId)
        {
            host = container;
            if (!string.Equals(hostFor, characterId, StringComparison.OrdinalIgnoreCase))
            {
                openSlot = null;
                hostFor = characterId;
            }
            foreach (var slot in EquipmentService.Slots)
            {
                container.AddChild(SlotRow(characterId, slot));
                if (IsOpen(slot.ID))
                    container.AddChild(Picker(characterId, slot));
            }
            container.AddChild(UiKit.Hint(Tr("equipment/sheet/hint")));
        }

        /// <inheritdoc />
        public void Reset() => openSlot = null;

        private bool IsOpen(string slotId) => openSlot != null && string.Equals(openSlot, slotId, StringComparison.OrdinalIgnoreCase);

        private void Toggle(string slotId)
        {
            openSlot = IsOpen(slotId) ? null : slotId;
            Redraw();
        }

        private void Close()
        {
            openSlot = null;
            Redraw();
        }

        /// <summary>Rebuilds only this block's rows, keeping the sheet's own layout.</summary>
        private void Redraw()
        {
            if (host == null || hostFor == null)
                return;
            foreach (var child in host.GetChildren())
            {
                host.RemoveChild(child);
                child.QueueFree();
            }
            Build(host, hostFor);
        }

        private Control SlotRow(string characterId, EquipSlotInfo slot)
        {
            var row = UiKit.Row(UiTokens.Space3);
            row.Name = "Slot_" + slot.ID;
            var name = UiKit.Label(Tr(slot.Name ?? slot.ID), UiTokens.FontBody, UiTokens.Body);
            name.CustomMinimumSize = new Vector2(170, 0);
            row.AddChild(name);

            var itemId = EquipmentService.ItemIn(characterId, slot.ID);
            var item = EquipmentService.Item(itemId);
            if (item == null)
            {
                row.AddChild(UiKit.Label("—", UiTokens.FontBody, UiTokens.Faint));
                var equip = UiKit.Button(Tr("equipment/sheet/equip"));
                equip.Name = "Equip";
                equip.Pressed += () => Toggle(slot.ID);
                row.AddChild(equip);
                return row;
            }

            var icon = string.IsNullOrEmpty(item.Sprite) ? null : TextureUtils.LoadFromModGroup("items", item.Sprite);
            if (icon != null)
            {
                var picture = UiKit.Icon(icon, 32);
                picture.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
                row.AddChild(picture);
            }
            var label = UiKit.Label(Tr(item.Name), UiTokens.FontBodyLarge, UiTokens.InkBright);
            label.TooltipText = string.IsNullOrEmpty(item.Description) ? string.Empty : Tr(item.Description);
            label.MouseFilter = Control.MouseFilterEnum.Pass;
            row.AddChild(label);
            var traits = ExtraData.Traits(item.ExtraProperties, item.ID).Where(t => t.Add != 0 || t.Percent != 0).ToList();
            if (traits.Count > 0)
                row.AddChild(UiKit.Label(string.Join("  ", traits.Select(TraitText)), UiTokens.FontSmall, UiTokens.SuccessText));

            var replace = UiKit.Button(Tr("equipment/sheet/replace"));
            replace.Name = "Replace";
            replace.Pressed += () => Toggle(slot.ID);
            row.AddChild(replace);

            var off = UiKit.Button(Tr("equipment/sheet/take_off"));
            off.Name = "TakeOff";
            off.Pressed += () =>
            {
                openSlot = null;
                EquipmentService.Unequip(characterId, slot.ID);
            };
            row.AddChild(off);
            return row;
        }

        private Control Picker(string characterId, EquipSlotInfo slot)
        {
            var frame = new PanelContainer { Name = "Picker_" + slot.ID, MouseFilter = Control.MouseFilterEnum.Stop };
            frame.AddThemeStyleboxOverride("panel", UiKit.Frame(UiTokens.RadiusMd, UiTokens.Surface2, UiTokens.Line));
            var margin = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            foreach (var side in new[] { "margin_left", "margin_top", "margin_right", "margin_bottom" })
                margin.AddThemeConstantOverride(side, UiTokens.Space3);
            frame.AddChild(margin);
            var column = UiKit.Column(UiTokens.Space2);
            margin.AddChild(column);

            column.AddChild(UiKit.SectionTitle(string.Format(Tr("equipment/sheet/picker_title"), Tr(slot.Name ?? slot.ID)), UiTokens.FontBody));
            var candidates = EquipmentService.BagCandidates(characterId, slot.ID);
            if (candidates.Count == 0)
                column.AddChild(UiKit.Hint(Tr("equipment/sheet/picker_empty")));
            foreach (var itemId in candidates)
            {
                var candidate = Candidate(characterId, slot.ID, itemId);
                if (candidate != null)
                    column.AddChild(candidate);
            }

            var cancel = UiKit.Button(Tr("equipment/sheet/picker_cancel"));
            cancel.Name = "Cancel";
            cancel.Pressed += Close;
            column.AddChild(cancel);
            return frame;
        }

        private Button Candidate(string characterId, string slotId, string itemId)
        {
            var item = EquipmentService.Item(itemId);
            if (item == null)
                return null;
            var text = Tr(item.Name);
            var traits = ExtraData.Traits(item.ExtraProperties, item.ID).Where(t => t.Add != 0 || t.Percent != 0).ToList();
            if (traits.Count > 0)
                text += "   " + string.Join("  ", traits.Select(TraitText));

            var button = UiKit.Button(text);
            button.Name = "Pick_" + item.ID.Replace('/', '_');
            button.Alignment = HorizontalAlignment.Left;
            button.CustomMinimumSize = new Vector2(0, 40);
            button.TooltipText = string.IsNullOrEmpty(item.Description) ? string.Empty : Tr(item.Description);
            var icon = string.IsNullOrEmpty(item.Sprite) ? null : TextureUtils.LoadFromModGroup("items", item.Sprite);
            if (icon != null)
            {
                button.Icon = icon;
                button.ExpandIcon = true;
            }
            button.Pressed += () => Put(characterId, slotId, item.ID);
            return button;
        }

        private void Put(string characterId, string slotId, string itemId)
        {
            openSlot = null;
            if (!EquipmentService.Equip(characterId, itemId, slotId))
                GameLogger.Log("EQUIPMENT", $"sheet: '{itemId}' was not put into '{slotId}' of '{characterId}'", LogLevel.Warning);
        }

        private static string TraitText(TraitInfo trait)
        {
            var def = StatsService.Definition(trait.Stat);
            var stat = def == null ? trait.Stat : Tr(string.IsNullOrEmpty(def.Short) ? def.Name : def.Short);
            return trait.Percent != 0
                ? $"{stat} {(trait.Percent > 0 ? "+" : "")}{trait.Percent.ToString("0.#", CultureInfo.InvariantCulture)}%"
                : $"{stat} {(trait.Add > 0 ? "+" : "")}{trait.Add.ToString("0.#", CultureInfo.InvariantCulture)}";
        }

        private static string Tr(string key) => TranslationServer.Translate(key).ToString();
    }
}
