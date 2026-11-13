# VikingsAscensionAPI

Small API DLL that lets other Valheim mods give, remove and read **VikingsAscension** XP and levels, read its guilds and add rune sockets to items.

- Reference `VikingsAscensionAPI.dll` and merge it into your mod with ILRepack.
- VikingsAscension is **not** required. Without it, every call returns 0 and nothing breaks.
- VikingsAscension is found at runtime, so updating it does not require rebuilding your mod.
- The only game reference is `assembly_valheim` (for `ItemDrop.ItemData`), which every Valheim mod already has. No Unity or BepInEx references.

## Setup

1. Copy `VikingsAscensionAPI.dll` to your project (for example `Libs/`) and add it as a reference.
2. Merge it into your mod in `ILRepack.targets`:
   ```xml
   <InputAssemblies Include="$(OutputPath)VikingsAscensionAPI.dll" />
   ```
   If you do not use ILRepack, ship `VikingsAscensionAPI.dll` next to your plugin DLL.
3. Make VikingsAscension load before your mod:
   ```csharp
   [BepInDependency("radamanto.VikingsAscension", BepInDependency.DependencyFlags.SoftDependency)]
   ```

## Usage

```csharp
using VikingsAscensionAPI;

AscensionAPI.AscensionAddExperience(500);
AscensionAPI.AscensionRemoveExperience(200);
AscensionAPI.AscensionAddLevel(1);
AscensionAPI.AscensionRemoveLevel(1);
int level = AscensionAPI.AscensionGetLevel();
long experience = AscensionAPI.AscensionGetExperience();
```

Call the API on the client of the player who receives the change, on the main thread.

## API reference

All methods are static, live in `VikingsAscensionAPI.AscensionAPI` and act on the **local player**.

| Method | Description | Returns |
|---|---|---|
| `AscensionAddExperience(int amount)` | Gives XP, with the normal level up and floating XP text. | XP actually given. |
| `AscensionRemoveExperience(int amount)` | Removes XP. The player can lose levels. | XP actually removed. |
| `AscensionAddLevel(int levels)` | Gives levels, with the level up effect. | The new level. |
| `AscensionRemoveLevel(int levels)` | Removes levels, down to level 1. At level 1 nothing changes. | The new level. |
| `AscensionGetLevel()` | Reads the current level. Changes nothing. | The current level. |
| `AscensionGetExperience()` | Reads the total XP earned since level 1. Changes nothing. | The total XP (`long`). |

Every method returns **0** when VikingsAscension is missing or disabled, the player is outside the world, or the value is below 1.

## Guilds

Read-only functions for the VikingsAscension guild system, for example a portal that only the owner's guild can use.

| Method | Description | Returns |
|---|---|---|
| `AscensionIsGuildsActive()` | Whether VikingsAscension is installed and guilds are turned on. | `bool` |
| `AscensionGetOwnGuildId()` | Guild of the local player. | Guild id, or 0 without a guild. |
| `AscensionGetPlayerGuildId(string playerId, string playerName)` | Guild of any player. | Guild id, or 0. |
| `AscensionIsGuildMember(int guildId, string playerId, string playerName)` | Whether the player belongs to that guild. | `bool` |
| `AscensionGetGuildName(int guildId)` | Display name of a guild. | Name, or an empty string. |

- **Guild id:** a non-zero `int` that stays the same for the guild's whole life. Save it (for example on the portal) and compare it later.
- **Player:** identified by the platform id (`Steam_76561198...`, the same as `ZNet.PlayerInfo.m_userInfo.m_id.ToString()`) and the character name together. Both must match.
- **On the server** every guild and member is known. **On a client** the local player is always known; any other player (even from the same guild) only while online, because the server never sends platform ids to clients.

```csharp
// When the portal is placed (client of the owner):
int ownerGuild = AscensionAPI.AscensionGetOwnGuildId();

// When someone uses it (client of the user):
bool allowed = ownerGuild != 0 && AscensionAPI.AscensionGetOwnGuildId() == ownerGuild;
```

## Sockets

Functions to give items with rune sockets, for example quest rewards, shop items or boss drops. They work on any item instance, before or after it goes into an inventory.

| Method | Description | Returns |
|---|---|---|
| `AscensionIsLoaded()` | Whether VikingsAscension is installed and enabled. | `bool` |
| `AscensionAddSockets(ItemDrop.ItemData item, int amount)` | Opens empty sockets on the item, up to 10 in total. Ignores the server's Maximum Sockets setting. | Sockets opened, or 0 if the item cannot have sockets. |
| `AscensionAddRune(ItemDrop.ItemData item, string rune, int level)` | Places a rune (level 1 to 9) in the first empty socket. | `false` when there is no empty socket, the rune does not exist, does not fit the item or is already on it. |
| `AscensionGetSocketCount(ItemDrop.ItemData item)` | Reads how many sockets the item has. Changes nothing. | Socket count. |
| `AscensionGetRarity(ItemDrop.ItemData item)` | Reads the rune rarity of the item. Changes nothing. | 0 common, 1 magic, 2 rare, 3 epic, 4 legendary. |

Rune names: `durability`, `lightness`, `resilience`, `selfrepair`, `soulbound`, `laststand`, `adrenaline`, `comfort`, `doublejump`, `return`, `blessing`, `firmness`, `challenge`.

```csharp
var item = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
AscensionAPI.AscensionAddSockets(item, 3);
AscensionAPI.AscensionAddRune(item, "durability", 2);
player.GetInventory().AddItem(item);
```

Equipment that cannot hold sockets (food, materials, ammo, torches and items in the server's socket blacklist) is refused.

## Behavior

- **Max level.** XP and levels past the server's max level are discarded. `AscensionAddExperience` returns 0 at max level and while the player is dead.
- **Losing levels.** When the player goes down a level, passives that no longer fit the passive points are refunded automatically.
- **Level changes.** `AscensionAddLevel` and `AscensionRemoveLevel` start the player at the beginning of the new level (0 XP inside it), like the admin level commands.
- **XP multipliers.** XP given or removed through the API is applied as is. It is not scaled by the VikingsAscension XP multipliers.
- **Load order.** The API resolves VikingsAscension once, on the first call, using `Type.GetType("VikingsAscension.AscensionAPI, VikingsAscension")`, and caches its method delegates. Use the soft dependency above so VikingsAscension loads first. If resolution fails, calls keep returning their default values for the rest of the session; there is no retry timer.
- **Errors.** Calls never throw. If something fails inside VikingsAscension, the call returns 0.

## Troubleshooting

| Problem | Check |
|---|---|
| Every call returns 0 | VikingsAscension is installed on the client, the player is in the world, and your plugin has the `BepInDependency` above. |
| `TypeLoadException` or `FileNotFoundException` for `VikingsAscensionAPI` | The DLL was not merged with ILRepack and is not next to your plugin DLL. |

## Building

Open `VikingsAscensionAPI.sln` in Visual Studio and build the `Release` configuration. The only extra reference, `assembly_valheim.dll`, is in `Libs/`. The output goes to `bin/Release/`, and a prebuilt `VikingsAscensionAPI.dll` is included at the repository root.
