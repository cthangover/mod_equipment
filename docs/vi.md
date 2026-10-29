# Equipment — trang bị làm thay đổi chỉ số nhân vật

Equipment cho phép nhân vật mang theo đồ: vũ khí, giáp, bùa hộ thân — hay trong tiểu thuyết tương tác là trang phục và trang sức. Khi vật phẩm đang được mặc, phần cộng của nó được tính vào chỉ số nhân vật, nên bảng chỉ số, trận chiến, phép kiểm chỉ số và các điều kiện đều đổi theo ngay. Tháo ra thì món đồ trở lại túi.

Mod cần Stats Engine và Items Base. Các ô và trang bị của RPG Plus đến từ gói Classic RPG Rules; game khác sẽ tự mô tả phần của mình.

## Mặc đồ

Mở túi, chọn món đồ và bấm **Trang bị**, rồi chọn ai sẽ mặc nó:

![Túi đồ trong RPG Plus: chọn Leather armour, chi tiết của nó (Targeted, loại trang bị armor, đặc tính def +2 và hp +6, động từ Trang bị), nút Trang bị và danh sách mục tiêu với anh hùng 42/42.](assets/bag-equip.jpg)

Món đồ rời khỏi túi và xuất hiện trong khối **Trang bị** của bảng chỉ số. Trước và sau:

![Phần chiến đấu và trang bị của bảng chỉ số. Trước: Health 42/42, Armour 4, Body trống. Sau: Health 48/48 (+10), Armour 6 (+2), Body — Leather armour, DEF +2 HP +6, kèm nút Tháo.](assets/sheet-before-after.jpg)

Giáp tăng từ 4 lên 6 — đó là `def` +2 của món giáp. Máu tăng từ 42 lên 48: nền 38 cộng `hp` +6 của món giáp là 44, và phần thưởng 10% máu của chiến binh đưa lên 48.

- **Tháo** trên bảng chỉ số đưa món đồ trở lại túi.
- Mặc món mới vào ô đã có đồ thì món cũ quay về túi.
- Chỉ có thể chọn thành viên trong đội.

## Biến vật phẩm thành trang bị

Hai khóa trong JSON của chính vật phẩm biến nó thành trang bị; mod vật phẩm không cần biết điều đó. Món giáp da từ Classic RPG Rules:

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

- `Equip` — loại trang bị của vật phẩm; nó quyết định ô trang bị.
- `Traits` — tác dụng khi đang mặc, viết theo định dạng đặc tính của Stats Engine (`Add`, `Percent` hoặc `Formula`).
- `Archetypes` (tùy chọn) — ai được mặc nó, ví dụ `["classic/mage"]` cho trục sồi.
- `ItemType: TargetUsed` đi cùng `ItemAction: action/equip` sẽ tạo nút Trang bị và danh sách mục tiêu cho món đồ; `verb` đặt chữ trên nút.

## Các ô trang bị

Các ô là thực thể loại `equip_slot`. Mỗi ô liệt kê những loại trang bị mà nó nhận:

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

Ô không có `Accepts` nhận các vật phẩm mà `Equip` trùng với id của chính ô đó.

## Vật phẩm tăng chỉ số vĩnh viễn

`\"ItemAction\": \"action/stat_up\"` kèm khóa `StatUp` — ví dụ `{"str": 1}` — sẽ thay đổi vĩnh viễn chỉ số của thành viên đội bị dùng lên, giống một lọ thuốc hay một cuốn sách. Món vật phẩm bị tiêu hao.

## Trong kịch bản

```scenario
action equip.put who=rpg_hero item=classic/wood_sword give=1
action equip.remove who=rpg_hero slot=weapon
when "equipped(rpg_hero, 'classic/wood_sword')" -> :armed
```

- `equip.put` mặc một vật phẩm từ túi; có `give=1` thì vật phẩm được cấp thẳng và không cần nằm trong túi. RPG Plus trao vũ khí khởi đầu cho từng lớp theo cách này. Khi nhiều ô đều vừa, dùng `slot=` để chỉ định.
- `equip.remove` tháo món đang mặc trong một ô, trả về túi.
- Khi món đồ còn được mặc, `equipped(who, 'item')` đúng.

Trang bị đang mặc được lưu cùng game.

## Khi có sự cố

1. **Bấm Trang bị xong không có ai để chọn.** Danh sách mục tiêu chỉ có thành viên đội; trong RPG Plus, anh hùng gia nhập đội khi bước vào làng, sau phần mở đầu.
2. **Không mặc được món đồ.** Không ô nào nhận loại `Equip` của nó, hoặc `Archetypes` của nó không gồm lớp của nhân vật.
3. **Bảng chỉ số không đổi.** `Traits` của món đồ nêu những chỉ số mà game không định nghĩa.
