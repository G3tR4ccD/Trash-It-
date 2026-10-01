# Trash It!

A first-person digging game in Unity. You carve into a huge mound of trash with a scoop, collect whatever you dig up, and feed it into machines that process it. The pile starts at one billion pieces, and your job is to shrink it.

I'm still building it, so it's a work in progress.

Scene View

<img width="465" height="248" alt="image" src="https://github.com/user-attachments/assets/339c5665-f907-47a7-93ad-e4d03847fafd" />

First Person View

<img width="380" height="236" alt="image" src="https://github.com/user-attachments/assets/2dfe7773-3ebb-483e-944c-698dcb33dd65" />

Digging into the Mountain

<img width="550" height="564" alt="image" src="https://github.com/user-attachments/assets/2186be5d-98b7-4cf6-90fa-a0ebe80fe480" />


## Features

- **Smooth, diggable terrain.** The ground is a grid of density values turned into a mesh with the marching cubes algorithm. Digging lowers the density inside a sphere, then the mesh and its collider rebuild on the spot. The result looks like carved earth, not blocks.
- **Digging.** Hold the dig button and aim at the terrain. A raycast from the screen center finds the spot, and a cooldown paces each scoop. The game counts only the voxels that flipped from solid to air, and that count drives the loot.
- **Loot.** Each dug voxel rolls between two item types, with a 60/40 split.
- **Limited pockets.** Your inventory has a capacity. Whatever doesn't fit gets bagged and dropped in the world, and you can pick it up later.
- **Interaction system.** Anything with the `IInteractable` interface can be used. The prompt on screen shows the actual key bound to the action.
- **Machines.** A machine takes ingredients from your inventory, runs a timer, and hands over the result. It moves through three states: Idle, Processing, and Finished. Items, recipes, and machines are all ScriptableObject assets, so adding new ones needs no code.
- **First-person movement.** Walk, sprint, jump, and mouse look.
- **World counter.** The game tracks how much trash is left in the pile.

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
| `Chunk.cs` | Builds the terrain from voxel densities and handles digging |
| `MarchingTables.cs` | Lookup table that marching cubes uses to pick triangles |
| `PlayerDigging.cs` | Aims, digs, rolls the loot, and drops bags for overflow |
| `Inventory.cs` | Capacity-limited item storage |
| `PlayerInteraction.cs` | Finds what you're looking at and shows the prompt |
| `IInteractable.cs` | The interface anything usable implements |
| `TrashPickup.cs` | A dropped bag of overflow items |
| `Machine.cs` | The Idle, Processing, Finished machine logic |
| `ItemData.cs`, `RecipeData.cs`, `MachineData.cs` | ScriptableObject definitions |
| `ItemGiver.cs` | A test object that hands out an item |
| `Movement.cs` | First-person movement and look |
| `GameManager.cs` | Global counters for coins and remaining trash |

## Running it

1. Install Unity Hub and the Unity version listed in `ProjectSettings/ProjectVersion.txt`.
2. Clone this repo and add the folder in Unity Hub.
3. Open the project's scene and press Play.

## Status

Working: digging, terrain rebuilding, loot, overflow bags, interaction, machines, and movement.

Not built yet:
- Coins are tracked, but nothing earns or spends them yet.
- There's no inventory screen. Right now, item counts show up in the console.
- The world is one 16 by 16 by 16 chunk. Multiple chunks are next.
- Item prices exist on the data assets but aren't used yet.


## What I practiced

- Procedural meshes and the marching cubes algorithm.
- Raycasting for digging and interaction.
- Interfaces, so one interaction system works with any object.
- ScriptableObjects for items, recipes, and machines.
- Coroutines for machine timers.
