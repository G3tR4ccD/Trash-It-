# Trash It!

A first-person digging game in Unity. You play a raccoon carving into a huge mountain of trash. You dig it up, process it in machines, sell the goods, and buy upgrades and helpers to dig for you. The pile starts at one billion pieces, and your job is to shrink it.

I'm still building it, so it's a work in progress.

**Play it in the browser:** https://g3tr4ccd.github.io/Trash-It-/

Scene View

<img width="465" height="248" alt="image" src="https://github.com/user-attachments/assets/339c5665-f907-47a7-93ad-e4d03847fafd" />

First Person View

<img width="380" height="236" alt="image" src="https://github.com/user-attachments/assets/2dfe7773-3ebb-483e-944c-698dcb33dd65" />

Digging into the Mountain

<img width="550" height="564" alt="image" src="https://github.com/user-attachments/assets/2186be5d-98b7-4cf6-90fa-a0ebe80fe480" />

## The loop

Dig trash, process it in machines, sell the goods, upgrade, automate.

## Features

- **Smooth, diggable terrain.** The ground is a grid of density values turned into a mesh with the marching cubes algorithm. Digging lowers the density inside a sphere, then the mesh and its collider rebuild on the spot. A cleanup pass removes thin leftover spikes after each dig.
- **A whole mountain.** The mountain is built from a grid of chunks and shaped by a grayscale height map, with a flat floor around it. Chunks share their border voxels, so a dig near an edge changes every chunk it touches.
- **Depth layers.** The mountain has five layers, each with its own material. The deeper you go, the harder the material is to dig. Layers are measured by depth below the original surface, so the deep material stays hidden until you dig down to it.
- **Loot tables per layer.** Each dug voxel rolls on the loot table of its layer. Tables are ScriptableObjects with weighted entries. Shallow layers give mostly cardboard and plastic, glass starts in the middle layers, and metal only turns up in the deepest layer.
- **Limited pockets.** Your inventory has a capacity. Whatever doesn't fit gets bagged and dropped in the world, and you can pick it up later.
- **Item chains.** Everything you dig can be processed into more valuable goods:
  - Cardboard: Soggy Cardboard, Pulp, Cardboard Sheets
  - Plastic: Plastic Scraps, Flakes, Molten Plastic, Plastic Sheets
  - Metal: Metal Scrap, Molten Metal, Metal Sheet
  - Glass: Glass Shard, Molten Glass, Glass Window
- **Machines.** A machine takes ingredients from your inventory, runs a timer, and hands over the result. It moves through three states: Idle, Processing, and Finished. Items, recipes, and machines are ScriptableObject assets, so adding new ones needs no code. The Shredder, Mixer, Smelter, and Press are unlocked in the shop.
- **Shop.** A four-page shop with player upgrades (dig radius, dig reach, backpack size), machine unlocks, helper purchases, and a button to sell your wares. Upgrade costs scale with each purchase.
- **Saving.** The game saves coins, upgrades, inventory, and every chunk you've modified to a JSON file. It autosaves every 30 seconds and when you close the shop.
- **DigBro, the first helper.** DigBro is a digger helper you can buy in the shop. It walks on the mountain by following the ground with raycasts (no NavMesh, so it works on terrain that keeps changing). It finds the nearest face of the mountain, digs it, carries what it finds in its own small backpack, and walks to a drop-off bin to empty it. You can interact with the bin to collect everything DigBro has delivered. Helper digs count against the world's trash counter too.
- **Interaction system.** Anything with the `IInteractable` interface can be used (machines, the shop, the bin, dropped bags). The prompt on screen shows the actual key bound to the action.
- **HUD.** Coins, a backpack bar, and the world counter, which shortens large numbers (for example 1000.0M).
- **First-person movement.** Walk, sprint, jump, and mouse look.

## Controls

| Input | Action |
| --- | --- |
| W A S D (or I J K L) | Move |
| Mouse | Look |
| Space | Jump |
| Shift (or /) | Sprint |
| Hold left mouse button | Dig |
| E (or U) | Interact |

## Scripts

| Script | What it does |
| --- | --- |
| `Chunk.cs` | Builds the terrain from voxel densities, assigns layers and materials, handles digging and spike cleanup |
| `MountainManager.cs` | Creates the grid of chunks from the height map and routes digs to the right chunks |
| `MarchingTables.cs` | Lookup table that marching cubes uses to pick triangles |
| `PlayerDigging.cs` | Aims, digs, and rolls loot from the per-layer tables |
| `LootTable.cs` | Weighted loot table (ScriptableObject) |
| `DiggerHelper.cs` | DigBro: ground-following movement, face finding, sweeping dig, and the trips to the bin |
| `BinInteractable.cs` | The drop-off bin you interact with to collect delivered items |
| `Inventory.cs` | Capacity-limited item storage |
| `ShopManager.cs` | The shop, purchases, upgrade effects, helper spawning, and saving and loading |
| `SellTooltip.cs`, `UpgradeTooltip.cs` | Tooltips for the shop buttons |
| `HUD.cs` | Coins, backpack bar, and the trash counter |
| `PlayerInteraction.cs` | Finds what you're looking at and shows the prompt |
| `IInteractable.cs` | The interface anything usable implements |
| `TrashPickup.cs` | A dropped bag of overflow items |
| `Machine.cs` | The Idle, Processing, Finished machine logic |
| `ItemData.cs`, `ItemDatabase.cs`, `RecipeData.cs`, `MachineData.cs`, `UpgradeData.cs` | ScriptableObject definitions |
| `SaveData.cs` | The data classes written to the save file |
| `ItemGiver.cs` | A test object that hands out an item |
| `Movement.cs` | First-person movement and look |
| `GameManager.cs` | Global counters for coins and remaining trash |

## Running it

**In the browser:** open the link at the top. The first load downloads the whole game, so it can take a moment. Click the page once so the browser hands over the mouse.

**In Unity:**

1. Install Unity Hub and the Unity version listed in `ProjectSettings/ProjectVersion.txt`.
2. Clone this repo and add the folder in Unity Hub.
3. Open the project's scene and press Play.

The web build lives in the `docs` folder and is served with GitHub Pages.

## Status

Working: digging, terrain rebuilding, depth layers, per-layer loot, overflow bags, interaction, machines, the shop, selling, upgrades, saving and loading, and the digger helper with its drop-off bin.

In progress:
- animations and models

Not built yet:

- Automatic builds. Right now I build the web version by hand and copy it into `docs`.

Known issues:
- In the browser build, saves don't survive a page reload yet. Saving works in the editor and in standalone builds.

## What I practiced

- Procedural meshes and the marching cubes algorithm, including one submesh per material.
- Raycasting for digging, interaction, and for helpers following the ground.
- Interfaces, so one interaction system works with any object.
- ScriptableObjects for items, recipes, machines, upgrades, and weighted loot tables.
- Coroutines for machine timers, and C# events to keep the HUD in sync.
- Saving and loading with JSON, including re-applying upgrades on load.
- Building a multi-page UI and wiring it to game logic.
- Simple helper AI without a NavMesh.
- Building for WebGL and hosting it on GitHub Pages.
