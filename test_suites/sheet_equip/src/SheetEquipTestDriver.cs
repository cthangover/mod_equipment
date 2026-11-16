namespace Cthangover.Mods.Equipment.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using Cthangover.Core.Bootstrap;
    using Cthangover.Core.Characters;
    using Cthangover.Core.Items;
    using Cthangover.Core.Scenes;
    using Cthangover.Core.Settings;
    using Cthangover.Core.UI;
    using Cthangover.Core.Utils;
    using Cthangover.Mods.Interface.View;
    using Cthangover.Mods.Rpg;
    using Cthangover.Mods.Stats;
    using Godot;

    /// <summary>
    /// Plays the character sheet's equipment like a player, through the sheet's own buttons:
    /// a second hero joins the rpg_plus party in the village, the party list appears with two
    /// heroes and switches between them, the empty weapon slot opens the bag's picker (the
    /// swords, not the mage's wand or the armour), a pick puts the sword on and the stats follow,
    /// Replace puts the worn sword back into the bag and the longer one on, an empty trinket slot
    /// shows its message, Take off empties the slot, and the hero's own sheet is unchanged.
    /// <para/>
    /// One step per timer tick: a step either waits (returns without advancing) or acts and checks.
    /// A failed check is recorded and the run goes on. Result line:
    /// <c>[TEST_RESULT] name=sheet_equip passed=N failed=M</c>, exit code 0/1, and a 120 s watchdog.
    /// </summary>
    public partial class SheetEquipTestDriver : Node
    {
        private const float TickSeconds = 0.1f;
        private const float TimeoutSeconds = 120f;
        private const string Village = "rpg_village";
        private const string Hero = "rpg_hero";
        private const string Ally = "sheet_test/ally";
        private const string Short = "sheet_test/short_sword";
        private const string Long = "sheet_test/long_sword";
        private const string Wand = "sheet_test/oak_wand";
        private const string Vest = "sheet_test/leather_vest";
        private const string Weapon = "weapon";
        private const string Body = "body";
        private const string Trinket = "trinket";
        private const string SheetEntry = "sheet";

        private readonly string shotsDir;
        private readonly List<string> failures = new();
        private int passed;
        private int step;
        private int waitTicks;
        private double elapsed;
        private string heroWeaponBefore;
        private string pendingShot;

        /// <summary>Creates the driver; <paramref name="shotsDir"/> null disables screenshots.</summary>
        public SheetEquipTestDriver(string shotsDir)
        {
            this.shotsDir = shotsDir;
            Name = "SheetEquipTestDriver";
            var timer = new Timer { WaitTime = TickSeconds, Autostart = true };
            AddChild(timer);
            timer.Timeout += Tick;
        }

        private static RpgWorldView View => RpgWorldHost.Current?.View;

        private static IInventory Inventory => GameData.Instance.Runtime.Inventory;

        private static CharacterData Roster => GameData.Instance.Runtime.CharacterData;

        private static StatSheetPanel SheetPanel()
            => UiHost.Instance != null && UiHost.Instance.TryGetInstance(SheetEntry, out var control) ? control as StatSheetPanel : null;

        private void Tick()
        {
            if (step < 0)
                return;
            FlushShot();
            elapsed += TickSeconds;
            if (elapsed > TimeoutSeconds)
            {
                Fail($"timeout at step {step} (map {View?.Map?.ID}, dialog running {RpgInput.FindDialogBox()?.IsRunning})");
                Finish();
                return;
            }
            try
            {
                RunStep();
            }
            catch (System.Exception ex)
            {
                Fail($"step {step}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                Finish();
            }
        }

        private void RunStep()
        {
            var panel = SheetPanel();
            switch (step)
            {
                case 0:
                    // The rpg_plus prologue runs first; the village intro dialog is what brings the hero.
                    if (!Boot.IsReady || SkipPrologue())
                        return;
                    if (View?.Map?.ID == Village)
                        Next();
                    return;
                case 1:
                    if (AdvanceDialog() || GodotSceneService.IsTransitioning || !Settle(3))
                        return;
                    Check(Roster.Characters.ContainsKey(Hero), "the village intro put the hero into the party");
                    Check(Roster.Characters.Count == 1, "one hero in the party before the test ally joins");
                    UiRegistry.Toggle(SheetEntry);
                    panel = SheetPanel();
                    Check(panel != null && panel.Visible, "the toolbar entry 'sheet' opens the character sheet");
                    if (panel == null)
                    {
                        Finish();
                        return;
                    }
                    Check(!PartyColumn(panel).Visible, "with one hero the party list is hidden");
                    Shot("one_hero");
                    Next();
                    return;
                case 2:
                    Check(Roster.AddCharacterToParty(Ally), "the test ally joins the party");
                    Inventory.Add(Short, 1);
                    Inventory.Add(Long, 1);
                    Inventory.Add(Wand, 1);
                    Inventory.Add(Vest, 1);
                    heroWeaponBefore = EquipmentService.ItemIn(Hero, Weapon);
                    panel.Select(Hero);
                    Check(PartyColumn(panel).Visible, "with two heroes the party list shows");
                    Check(PartyList(panel).GetChildCount() == 2, $"the list has both heroes, got {PartyList(panel).GetChildCount()}");
                    Check(MemberButton(Hero) != null && MemberButton(Hero).ButtonPressed, "the selected hero is highlighted");
                    Check(MemberButton(Hero).Text.Contains(Tr("stats/sheet/leader")), "the leader is marked in the list");
                    Check(!MemberButton(Ally).Text.Contains(Tr("stats/sheet/leader")), "a companion is not marked as leader");
                    Shot("party_list");
                    Next();
                    return;
                case 3:
                    Click(MemberButton(Ally));
                    Check(panel.SelectedId == Ally, "a click in the list shows the ally's sheet");
                    Check(MemberButton(Ally).ButtonPressed && !MemberButton(Hero).ButtonPressed, "the list highlights the shown member only");
                    Check(SlotButton(Weapon, "Equip") != null, "the empty weapon slot offers Equip");
                    Check(SlotButton(Body, "Equip") != null, "the empty body slot offers Equip");
                    Shot("ally_empty");
                    Next();
                    return;
                case 4:
                    Click(SlotButton(Weapon, "Equip"));
                    Check(Picker(Weapon) != null, "the empty weapon slot opens the picker");
                    Check(PickButton(Short) != null && PickButton(Long) != null, "the picker offers both swords from the bag");
                    Check(PickButton(Wand) == null, "the mage's staff is not offered to a warrior");
                    Check(PickButton(Vest) == null, "armour is not offered for the weapon slot");
                    Shot("picker_weapon");
                    Next();
                    return;
                case 5:
                    Click(PickButton(Short));
                    Next();
                    return;
                case 6:
                    if (!Settle(2))
                        return;
                    Check(EquipmentService.ItemIn(Ally, Weapon) == Short, "the picked sword is worn in the weapon slot");
                    Check(Inventory.CheckCount(Short) == 0, "the worn sword left the bag");
                    Check(Picker(Weapon) == null, "the picker closes after a pick");
                    var sheet = StatsService.Sheet(Ally);
                    Check(sheet.Get("str") - sheet.GetBase("str") == 2, $"the sword's +2 strength reaches the sheet, got {sheet.Get("str") - sheet.GetBase("str")}");
                    Check(SlotButton(Weapon, "TakeOff") != null && SlotButton(Weapon, "Replace") != null, "the worn slot offers Take off and Replace");
                    Shot("equipped");
                    Next();
                    return;
                case 7:
                    Click(SlotButton(Weapon, "Replace"));
                    Check(Picker(Weapon) != null, "Replace opens the same picker");
                    Check(PickButton(Long) != null, "the longer sword is offered as the replacement");
                    Check(PickButton(Short) == null, "the worn sword is not offered: it is not in the bag");
                    Shot("replace_picker");
                    Next();
                    return;
                case 8:
                    Click(PickButton(Long));
                    Next();
                    return;
                case 9:
                    if (!Settle(2))
                        return;
                    Check(EquipmentService.ItemIn(Ally, Weapon) == Long, "the longer sword replaced the shorter one");
                    Check(Inventory.CheckCount(Short) == 1, "the replaced sword is back in the bag");
                    Check(Inventory.CheckCount(Long) == 0, "the new sword left the bag");
                    var swapped = StatsService.Sheet(Ally);
                    Check(swapped.Get("str") - swapped.GetBase("str") == 3, $"only the longer sword counts now (+3), got {swapped.Get("str") - swapped.GetBase("str")}");
                    Shot("replaced");
                    Next();
                    return;
                case 10:
                    Click(SlotButton(Trinket, "Equip"));
                    Check(Picker(Trinket) != null, "the trinket slot opens its picker");
                    Check(PickButton(Short) == null && PickButton(Vest) == null, "nothing from the bag fits the trinket slot");
                    var empty = Tr("equipment/sheet/picker_empty");
                    Check(Picker(Trinket)?.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text == empty) == true, "an empty picker says so");
                    Shot("picker_empty");
                    Next();
                    return;
                case 11:
                    Click(Picker(Trinket)?.FindChild("Cancel", true, false) as Button);
                    Check(Picker(Trinket) == null, "Cancel closes the picker");
                    Click(SlotButton(Trinket, "Equip"));
                    UiRegistry.Toggle(SheetEntry);
                    UiRegistry.Toggle(SheetEntry);
                    Check(SheetPanel()?.Visible == true && Picker(Trinket) == null, "closing and reopening the sheet drops an open picker");
                    Next();
                    return;
                case 12:
                    Click(SlotButton(Weapon, "TakeOff"));
                    Next();
                    return;
                case 13:
                    if (!Settle(2))
                        return;
                    Check(EquipmentService.ItemIn(Ally, Weapon) == null, "Take off empties the weapon slot");
                    Check(Inventory.CheckCount(Long) == 1, "the taken-off sword goes back to the bag");
                    var bare = StatsService.Sheet(Ally);
                    Check(bare.Get("str") == bare.GetBase("str"), "without the sword the strength is the base again");
                    Check(SlotButton(Weapon, "Equip") != null, "the emptied slot offers Equip again");
                    Shot("taken_off");
                    Next();
                    return;
                case 14:
                    Click(MemberButton(Hero));
                    Check(panel.SelectedId == Hero, "switching back shows the hero");
                    Check(EquipmentService.ItemIn(Hero, Weapon) == heroWeaponBefore, "the hero's own weapon is unchanged");
                    Check(!EquipmentService.Wears(Hero, Short) && !EquipmentService.Wears(Hero, Long), "the ally's swords are not on the hero");
                    Check(Picker(Weapon) == null, "switching members closes an open picker");
                    Shot("hero_sheet");
                    Next();
                    return;
                default:
                    Finish();
                    return;
            }
        }

        /// <summary>The party list's buttons, the sheet's own column.</summary>
        private static Control PartyColumn(StatSheetPanel panel) => panel.GetNode<Control>("Window/Margin/Root/Body/Party");

        private static VBoxContainer PartyList(StatSheetPanel panel) => panel.FindChild("PartyList", true, false) as VBoxContainer;

        private static string Tr(string key) => TranslationServer.Translate(key).ToString();

        private static Button MemberButton(string id)
            => SheetPanel()?.FindChild("Member_" + id.Replace('/', '_'), true, false) as Button;

        private static Button SlotButton(string slot, string name)
            => SheetPanel()?.FindChild("Slot_" + slot, true, false)?.FindChild(name, true, false) as Button;

        private static Control Picker(string slot)
            => SheetPanel()?.FindChild("Picker_" + slot, true, false) as Control;

        private static Button PickButton(string itemId)
            => SheetPanel()?.FindChild("Pick_" + itemId.Replace('/', '_'), true, false) as Button;

        private static void Click(Button button)
        {
            if (button == null)
                GameLogger.Log("TEST", "[FAIL] click on a button that is not on the sheet", LogLevel.Error);
            button?.EmitSignal(BaseButton.SignalName.Pressed);
        }

        /// <summary>Waits <paramref name="ticks"/> ticks after the last step change (a redraw is deferred by a frame).</summary>
        private bool Settle(int ticks)
        {
            waitTicks++;
            return waitTicks > ticks;
        }

        /// <summary>Skips the crossroads prologue by taking the first answer: the dialog is never played out here.</summary>
        private static bool SkipPrologue()
        {
            var scene = ((SceneTree)Godot.Engine.GetMainLoop()).Root.GetNodeOrNull<SceneManager>("SceneManager")?.CurrentSceneName;
            if (string.IsNullOrEmpty(scene) || View?.Map != null)
                return false;
            var box = RpgInput.FindDialogBox();
            if (box == null || !box.IsRunning)
                return false;
            if (box.IsWaitingAnswer)
                box.TrySelectVariant(0);
            else
                box.NextAction();
            return true;
        }

        /// <summary>Advances a running dialog with the action key; true while it is still running.</summary>
        private bool AdvanceDialog()
        {
            var box = RpgInput.FindDialogBox();
            if (box == null || !box.IsRunning)
                return false;
            if (box.IsWaitingAnswer)
                box.TrySelectVariant(0);
            else
                box.NextAction();
            return true;
        }

        private void Next()
        {
            step++;
            waitTicks = 0;
        }

        private void Check(bool condition, string what)
        {
            if (condition)
            {
                passed++;
                GameLogger.Log("TEST", $"[PASS] {what}");
            }
            else
            {
                Fail(what);
            }
        }

        private void Fail(string what)
        {
            failures.Add(what);
            GameLogger.Log("TEST", $"[FAIL] {what}", LogLevel.Error);
        }

        /// <summary>Asks for a screenshot of the state the current step leaves; it is taken at the next tick, once the frame has drawn it.</summary>
        private void Shot(string name)
        {
            if (string.IsNullOrEmpty(shotsDir) || DisplayServer.GetName() == "headless")
                return;
            pendingShot = name;
        }

        private void FlushShot()
        {
            if (pendingShot == null)
                return;
            DirAccess.MakeDirRecursiveAbsolute(shotsDir);
            ViewportCapture.SavePng(GetViewport(), shotsDir.TrimEnd('/') + "/sheet_equip_" + pendingShot + ".png");
            pendingShot = null;
        }

        private void Finish()
        {
            step = -1;
            Godot.Engine.TimeScale = 1;
            GameLogger.Log("TEST", $"[TEST_RESULT] name=sheet_equip passed={passed} failed={failures.Count}");
            ApplicationExit.Quit(GetTree(), failures.Count == 0 ? 0 : 1);
        }
    }
}
