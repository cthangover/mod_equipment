# Equipment — gear that changes a character's stats

Equipment lets characters wear things: a weapon, armour, a charm — or an outfit and jewellery in a visual novel. While an item is worn, its bonuses count in the character's stats, so the character sheet, battles, stat checks and conditions all follow at once. Taking it off returns it to the bag.

It needs Stats Engine and Items Base. The slots and the gear of RPG Plus come from the Classic RPG Rules pack; another game describes its own.

## Putting something on

Open the bag, pick the item and press **Equip**, then choose who wears it:

![The inventory in RPG Plus: Leather armour selected, its details (Targeted, Equip type armor, Traits def +2 and hp +6, verb Equip), the Equip button and the target list with the Hero at 42/42.](assets/bag-equip.jpg)

The item leaves the bag and appears in the **Equipment** block of the character sheet. Before and after:

![The Combat and Equipment part of the sheet. Before: Health 42/42, Armour 4, Body empty. After: Health 48/48 (+10), Armour 6 (+2), Body — Leather armour, DEF +2 HP +6, with a Take off button.](assets/sheet-before-after.jpg)

Armour went from 4 to 6 — the armour's `def` +2. Health went from 42 to 48: the base 38 plus the armour's `hp` +6 is 44, and the warrior's 10% health bonus makes it 48.

- **Take off** on the sheet puts the item back into the bag.
- Putting something into a taken slot returns the old item to the bag.
- Only party members can be chosen.

## Making an item wearable

Two keys in the item's own JSON make it gear; the items mod does not need to know about them. The leather armour from Classic RPG Rules:

```json
{
  "Id": "classic/leather_armor",
  "Name": "classic/item/leather_armor/name",
  "ItemType": "TargetUsed",
  "ItemAction": "action/equip",
  "verb": "equipment/verb_equip",
  "Equip": "armor",
  "Traits": [ { "Stat": "def", "Add": 2 }, { "Stat": "hp", "Add": 6 } ]
}
```

- `Equip` — the item's equip type; it decides the slot.
- `Traits` — what it gives while worn, in the trait format of Stats Engine (`Add`, `Percent` or `Formula`).
- `Archetypes` (optional) — who may wear it, for example `["classic/mage"]` for the oak staff.
- `ItemType: TargetUsed` with `ItemAction: action/equip` gives it the Equip button and the target list; `verb` names the button.

## Slots

Slots are entities of kind `equip_slot`. Each lists the equip types it accepts:

```json
{
  "Kind": "equip_slot",
  "Items": [
    { "Id": "weapon", "Name": "classic/slot/weapon", "Order": 1, "Accepts": ["sword", "staff", "dagger"] },
    { "Id": "body", "Name": "classic/slot/body", "Order": 2, "Accepts": ["armor", "robe"] },
    { "Id": "accessory", "Name": "classic/slot/accessory", "Order": 3, "Accepts": ["charm", "ring"] }
  ]
}
```

A slot without `Accepts` takes items whose `Equip` is the slot's own id.

## Items that raise stats for good

`"ItemAction": "action/stat_up"` with a `StatUp` key — for example `{"str": 1}` — permanently changes the stats of the party member it is used on, like a tonic or a book. The item is spent.

## In scenarios

```scenario
action equip.put who=rpg_hero item=classic/wood_sword give=1
action equip.remove who=rpg_hero slot=weapon
when "equipped(rpg_hero, 'classic/wood_sword')" -> :armed
```

- `equip.put` puts an item on from the bag; with `give=1` the item is granted instead and need not be in the bag. RPG Plus gives each class its starting weapon this way. `slot=` picks a slot when several would fit.
- `equip.remove` takes off what is worn in a slot, back into the bag.
- `equipped(who, 'item')` is true while the item is worn.

What is worn is saved with the game.

## If something goes wrong

1. **No one to choose after Equip.** The target list shows party members only; in RPG Plus the hero joins the party on entering the village, after the prologue.
2. **The item cannot be put on.** No slot accepts its `Equip` type, or its `Archetypes` do not include the character's class.
3. **The sheet does not change.** The item's `Traits` name stats the game does not define.
