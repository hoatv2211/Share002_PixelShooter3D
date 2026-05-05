# Pixel Shooter 3D

**[Play Online](https://hoatv2211.github.io/Share002_PixelShooter3D/)**

A Unity 6+ game development template for building pixel-style 3D shooter games. This asset is designed as a **template and starting point** — it provides the core systems, architecture, and tools needed to build your own game. Customize, extend, and modify it to create something unique.

## Requirements

- Unity 6 or newer
- TextMesh Pro (included with Unity)

## Quick Setup

1. Open the scene `Pixel Shooter 3D/Scenes/Game.unity`
2. Use menu **Pixel Shooter 3D > Setup Game Scene** to auto-configure GameManager references
3. Press Play to test the default level

## Scenes

| Scene | Description |
|---|---|
| `Scenes/Game.unity` | Main game scene |
| `Scenes/LevelEditor.unity` | Level editor (enter Play mode to use) |
| `Scenes/GameExample.unity` | Example scene |

## Architecture

All scripts use the `PixelShooter3D` namespace.

### Core (`Scripts/Core/`)

| Script | Description |
|---|---|
| `GameManager` | Main game controller — level loading, game state, powerups, player management. Singleton. |
| `SoundManager` | Audio system for playing sound effects. Singleton. |
| `InputManager` | Mouse/touch input, raycasting for pig selection, interaction events. |
| `CameraController` | Camera movement for hand-picker mode navigation. |
| `CameraFocusController` | Smooth camera focus transitions between targets. |

### Game (`Scripts/Game/`)

| Script | Description |
|---|---|
| `PigController` | Player character. State machine: `Deck` → `Jumping` → `OnBelt` → `Returning` → `Holding`. |
| `BlockController` | Destructible block entity. Spawns visual effects on destruction. |
| `BulletController` | Projectile with homing behavior toward blocks. |
| `SuperPigController` | Special powerup character with unique abilities. |
| `LevelEditorManager` | Full-featured level editor with JSON export/import. |
| `BlockColorizer` | Image-to-block color mapping using K-Means clustering. |

### Data (`Scripts/Data/`)

| Script | Description |
|---|---|
| `LevelData` | ScriptableObject containing level structure, block grid, and player configs. Supports JSON serialization. |

## Level System

Levels are stored as JSON in two locations:
- `Resources/Levels/` — for Editor and Standalone builds
- `StreamingAssets/Levels/` — for WebGL builds (with `levels_manifest.txt` for compatibility)

### JSON Format

```json
{
  "levelName": "MyLevel",
  "gridWidth": 10,
  "gridHeight": 10,
  "rows": [...],
  "deck": [
    {
      "pigs": [
        { "color": "#FF0000", "ammo": 5 }
      ]
    }
  ]
}
```

## Level Editor

1. Open `Scenes/LevelEditor.unity` and enter Play mode
2. **Block Grid** — place and remove blocks in a grid layout
3. **Color Palette** — define colors for your level's blocks
4. **Image Import** — import images and auto-extract colors via K-Means clustering
5. **Player Deck Setup** — configure player characters with colors and ammo
6. Enter a unique level name and click **Save** to export JSON

> **Note:** Source images must have **Read/Write Enabled** in Import Settings.

## Automatic Level Generator

Batch-generate levels from images without painting each cell manually.

1. Place images (PNG, JPG, BMP, TGA) into `Resources/AutomaticLevelGeneratorImages/`
2. Open **Tools > Auto Level Generator** from the Unity menu
3. Configure settings (grid size, color count, pig layout, ammo multiplier)
4. Click **Generate Levels**

The tool processes each image by resizing to a random grid size, running K-Means color quantization, and exporting a JSON level file to `Resources/Levels/` with auto-incremented names.

> **Tip:** Use Ammo Multiplier `1.0` for tight difficulty, `1.2`–`1.5` for a more comfortable experience.

## Image to 3D Blocks

1. In the Level Editor scene, select the **BlockColorizer** GameObject
2. Assign a Read/Write-enabled Texture2D to **Source Image**
3. Adjust **Max Colors** (1–32) to control extracted color count
4. Use **Overlay Scale** to adjust how the image maps to the block grid

## AdMob Ads

Includes Ragendom Monetization module for AdMob integration (banners, interstitials, rewarded videos). Uses a **Dummy provider** by default for Editor testing (green banner bar, dark interstitial overlay, blue rewarded video overlay).

Full setup: `RagendomAds/Documentation/Documentation.html`
Video tutorial: https://www.youtube.com/watch?v=nWmyWacoFAc

## Customization

### Adding New Powerups
Extend `GameManager` with your own powerup methods.

### Custom Player States
Add entries to the `PigState` enum in `PigController`:
```csharp
public enum PigState { Deck, Jumping, OnBelt, Returning, Holding, MyCustomState }
```

### Modifying Block Behavior
Extend `BlockController` to override damage, add special effects, or create blocks with unique properties.

### UI
All UI prefabs are in `Prefabs/UI/` and use TextMesh Pro for text rendering.

## Common Issues

| Issue | Solution |
|---|---|
| Missing references after import | Use **Pixel Shooter 3D > Setup Game Scene** |
| Texture not readable error | Enable Read/Write in texture Import Settings, or use "Fix Now" in BlockColorizer inspector |
| JSON import fails | Verify JSON matches the expected structure |
| Level overwrite warning | Use a unique level name to avoid losing previous work |

## Support

- This asset was built with the **HyperCasual Game Engine** 
