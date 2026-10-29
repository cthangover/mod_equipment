# Equipment — equipo que cambia las estadísticas del personaje

Equipment permite a los personajes llevar cosas: un arma, una armadura, un amuleto — o un atuendo y joyas en una novela visual. Mientras un objeto está equipado, sus bonos cuentan en las estadísticas del personaje, así que la hoja de personaje, los combates, las pruebas de estadísticas y las condiciones siguen al instante. Al quitarlo vuelve a la bolsa.

Necesita Stats Engine e Items Base. Las ranuras y el equipo de RPG Plus vienen del paquete Classic RPG Rules; otro juego describe las suyas.

## Cómo ponerse algo

Abre la bolsa, elige el objeto y pulsa **Equipar**, después escoge quién lo lleva:

![El inventario de RPG Plus: Leather armour seleccionada, sus detalles (Targeted, tipo de equipo armor, rasgos def +2 y hp +6, verbo Equip), el botón de equipar y la lista de objetivos con el héroe a 42/42.](assets/bag-equip.jpg)

El objeto sale de la bolsa y aparece en el bloque **Equipo** de la hoja de personaje. Antes y después:

![La parte de Combate y Equipo de la hoja. Antes: Health 42/42, Armour 4, Body vacía. Después: Health 48/48 (+10), Armour 6 (+2), Body — Leather armour, DEF +2 HP +6, con un botón para quitar.](assets/sheet-before-after.jpg)

La armadura pasó de 4 a 6 — el `def` de la armadura, +2. La salud pasó de 42 a 48: la base 38 más el `hp` de la armadura, +6, da 44, y el bono de salud del 10% del guerrero lo deja en 48.

- **Quitar** en la hoja devuelve el objeto a la bolsa.
- Poner algo en una ranura ocupada devuelve el objeto anterior a la bolsa.
- Solo se puede elegir a miembros del grupo.

## Convertir un objeto en equipo

Dos claves en el JSON del propio objeto lo convierten en equipo; el mod de objetos no necesita saberlo. La armadura de cuero de Classic RPG Rules:

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

- `Equip` — el tipo de equipo del objeto; decide la ranura.
- `Traits` — qué da mientras se lleva, en el formato de rasgos de Stats Engine (`Add`, `Percent` o `Formula`).
- `Archetypes` (opcional) — quién puede llevarlo, por ejemplo `["classic/mage"]` para el báculo de roble.
- `ItemType: TargetUsed` con `ItemAction: action/equip` le dan el botón de equipar y la lista de objetivos; `verb` nombra el botón.

## Las ranuras

Las ranuras son entidades de tipo `equip_slot`. Cada una enumera los tipos de equipo que acepta:

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

Una ranura sin `Accepts` toma los objetos cuyo `Equip` es el propio id de la ranura.

## Objetos que suben estadísticas para siempre

`\"ItemAction\": \"action/stat_up\"` con una clave `StatUp` — por ejemplo `{"str": 1}` — cambia para siempre las estadísticas del miembro del grupo en que se usa, como una poción o un libro. El objeto se consume.

## En los escenarios

```scenario
action equip.put who=rpg_hero item=classic/wood_sword give=1
action equip.remove who=rpg_hero slot=weapon
when "equipped(rpg_hero, 'classic/wood_sword')" -> :armed
```

- `equip.put` equipa un objeto desde la bolsa; con `give=1` el objeto se concede sin más y no hace falta que esté en la bolsa. RPG Plus da así a cada clase su arma inicial. `slot=` elige una ranura cuando varias servirían.
- `equip.remove` quita lo equipado en una ranura, de vuelta a la bolsa.
- `equipped(who, 'item')` es verdad mientras el objeto esté equipado.

Lo equipado se guarda con la partida.

## Si algo sale mal

1. **Tras equipar no hay a quién elegir.** La lista de objetivos muestra solo miembros del grupo; en RPG Plus el héroe se une al grupo al entrar en la aldea, después del prólogo.
2. **El objeto no se puede poner.** Ninguna ranura acepta su tipo `Equip`, o sus `Archetypes` no incluyen la clase del personaje.
3. **La hoja no cambia.** Los `Traits` del objeto nombran estadísticas que el juego no define.
