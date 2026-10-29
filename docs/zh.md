# Equipment —— 改变角色属性的装备

Equipment 让角色可以穿戴物品：武器、护甲、护身符——在视觉小说里也可以是服装和首饰。物品穿在身上时，其加成会计入角色属性，因此角色面板、战斗、属性检定和条件都会随之即时变化。脱下后物品回到背包。

它依赖 Stats Engine 和 Items Base。RPG Plus 的栏位和装备来自 Classic RPG Rules 包；其他游戏则自行描述自己的。

## 穿戴物品

打开背包，选中物品并点击**Equip**，然后选择由谁穿戴：

![RPG Plus 的背包：选中 Leather armour，其详情（Targeted、装备类型 armor、特性 def +2 与 hp +6、动词 Equip）、Equip 按钮和目标列表，英雄为 42/42。](assets/bag-equip.jpg)

物品离开背包，出现在角色面板的**装备**区块中。前后对比：

![面板的战斗与装备部分。之前：Health 42/42、Armour 4、Body 为空。之后：Health 48/48（+10）、Armour 6（+2）、Body——Leather armour、DEF +2 HP +6，并有 Take off 按钮。](assets/sheet-before-after.jpg)

护甲从 4 涨到 6——这是护甲的 `def` +2。生命从 42 涨到 48：基础 38 加上护甲的 `hp` +6 是 44，再加上战士 10% 的生命加成，正好 48。

- 面板上的 **Take off** 会把物品放回背包。
- 往已占用的栏位里穿新物品时，旧物品会回到背包。
- 只能选择队伍成员。

## 让物品可以穿戴

物品自身 JSON 里的两个键就能让它成为装备；物品模组无需知道这件事。Classic RPG Rules 的皮甲：

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

- `Equip`——物品的装备类型；由它决定栏位。
- `Traits`——穿戴时提供什么，采用 Stats Engine 的特性格式（`Add`、`Percent` 或 `Formula`）。
- `Archetypes`（可选）——谁能穿戴它，例如橡木法杖的 `["classic/mage"]`。
- `ItemType: TargetUsed` 配合 `ItemAction: action/equip` 会为物品提供装备按钮和目标列表；`verb` 指定按钮文字。

## 栏位

栏位是 `equip_slot` 类型的实体。每个栏位都列出它接受的装备类型：

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

没有 `Accepts` 的栏位接受 `Equip` 等于其自身 id 的物品。

## 永久提升属性的物品

`\"ItemAction\": \"action/stat_up\"` 配合 `StatUp` 键——例如 `{"str": 1}`——会永久改变被使用队员的属性，就像药水或书籍一样。物品会被消耗。

## 在剧本中

```scenario
action equip.put who=rpg_hero item=classic/wood_sword give=1
action equip.remove who=rpg_hero slot=weapon
when "equipped(rpg_hero, 'classic/wood_sword')" -> :armed
```

- `equip.put` 从背包中穿上物品；加了 `give=1` 则改为直接发放物品，无需在背包里。RPG Plus 就是这样给每个职业发初始武器的。当多个栏位都合适时，用 `slot=` 指定栏位。
- `equip.remove` 卸下某栏位中穿戴的物品，放回背包。
- 只要物品还穿在身上，`equipped(who, 'item')` 就为真。

穿戴状态会随游戏一起存档。

## 出问题怎么办

1. **点击装备后没有可选的人。** 目标列表只显示队伍成员；在 RPG Plus 中，英雄在序章之后进入村庄时才会加入队伍。
2. **物品穿不上。** 没有栏位接受它的 `Equip` 类型，或者它的 `Archetypes` 不包含该角色的职业。
3. **面板没有变化。** 物品的 `Traits` 引用了游戏未定义的属性。
