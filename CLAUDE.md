# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Pixel Shooter 3D is a Unity 6+ game development template for building pixel-style 3D shooter games. It is a **template/starting point**, not a ready-to-publish game. All scripts use the `PixelShooter3D` namespace.

## Unity Scenes

- `Pixel Shooter 3D/Scenes/Game.unity` — main game scene
- `Pixel Shooter 3D/Scenes/LevelEditor.unity` — level editor (used in Play mode)
- `Pixel Shooter 3D/Scenes/GameExample.unity` — example scene

## Architecture

### Scripts Structure (`Pixel Shooter 3D/Scripts/`)

- **Core/** — Singleton managers: `GameManager` (main controller, level loading, game state, powerups), `SoundManager` (audio), `InputManager` (mouse/touch raycasting), `CameraController` / `CameraFocusController`
- **Game/** — Game entities: `PigController` (player, state machine: Deck/Jumping/OnBelt/Returning/Holding), `BlockController` (destructible block), `BulletController` (homing projectile), `SuperPigController` (powerup character), `LevelEditorManager`, `BlockColorizer` (K-Means color extraction from images)
- **Data/** — `LevelData.cs` (ScriptableObject, JSON-serializable, defines block grid + player deck)
- **UI/** — UI components (TextMesh Pro based)
- **Utils/** — Utility scripts
- **Editor/** — Editor tools including `LevelEditor.cs` and the `AutoLevelGenerator` window (menu: `Tools > Auto Level Generator`)

### Level System

- Levels are stored as JSON in `Resources/Levels/` (Editor/Standalone) and `StreamingAssets/Levels/` (WebGL)
- `levels_manifest.txt` in StreamingAssets lists all levels for WebGL compatibility
- `LevelData` ScriptableObject handles JSON serialization/deserialization
- JSON format: `levelName`, `gridWidth`, `gridHeight`, `rows` (block grid), `deck` (player pigs with color + ammo)

### Key Workflows

- **Setup**: Use menu `Pixel Shooter 3D > Setup Game Scene` or `Setup Level Editor` to auto-configure Manager references
- **Level Editor**: Open `LevelEditor.unity` → Play mode → design grid, palette, player deck → save as JSON
- **Auto Level Generator**: Place images in `Resources/AutomaticLevelGeneratorImages/`, then `Tools > Auto Level Generator` — batch-generates levels from images using K-Means color quantization
- **Image-to-Blocks**: Assign a Read/Write-enabled Texture2D to `BlockColorizer` component, set Max Colors (1-32), adjust Overlay Scale

## AdMob Integration

Uses Ragendom Monetization module (`RagendomAds/`). Key objects: `[Monetization Init]` (initializes via `MonetizationSettings` ScriptableObject). Defaults to Dummy provider in Editor. See `RagendomAds/Documentation/Documentation.html` for full AdMob setup.

## Common Issues

- **Missing references**: Run `Pixel Shooter 3D > Setup Game Scene`
- **Texture not readable**: Enable Read/Write in texture Import Settings, or use "Fix Now" button in BlockColorizer inspector
- **JSON import fails**: Verify JSON matches expected structure (levelName, gridWidth, gridHeight, rows, deck)
- **Level overwrite protection**: Level Editor prompts confirmation if level name exists; use unique names
