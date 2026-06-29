# 3D Final Project Game Course

This repository contains my Unity 3D final project prototype. The game is a low-poly nature survival and collection level where the player explores a terrain scene, collects coins, picks up weapons, talks to NPCs, avoids enemies, and clears the level by completing the main objective.

## Game Concept

The player is dropped into a low-poly forest environment and must search the area for coins while surviving wandering enemies. Weapons can be collected and equipped so the player can shoot enemies. NPCs provide guidance about the objective and the level uses UI feedback, audio, lighting, and post-processing to make the scene feel like a playable game prototype instead of a blank test scene.

## Main Objective

Clear the level by completing both goals:

- Collect 20 coins.
- Defeat 5 enemies.

When both goals are complete, the game shows a level clear message and loads the restart scene. If the player loses all health after enemy contact, the game also loads the restart scene.

## Current Scenes

- `Assets/Scenes/StartScene.unity` - start menu with a play button.
- `Assets/Scenes/MainScene.unity` - main playable level.
- `Assets/Scenes/RestartScene.unity` - restart flow after winning or losing.

## Controls

- Move: `WASD` or left stick.
- Look/Camera: mouse or right stick.
- Jump: `Space`.
- Sprint: `Left Shift`.
- Talk to NPC: `E`.
- Shoot equipped weapon: `F`.
- Open/close inventory: `Y`.
- Switch acquired guns: `1`, `2`, or `3`.

## Current Gameplay Systems

- Third-person controllable player with follow camera.
- Low-poly terrain environment with trees, grass, props, lighting, sky, and global volume post-processing.
- Coin collectibles that update the coin counter.
- Five weapon pickups: three revolvers and two shotguns.
- Weapon equip system that places the selected weapon in the player's hand.
- Inventory system that tracks unique acquired guns and shows the chest count.
- Gun switching for the first three acquired guns using number keys.
- Shooting system with bullets and simple recoil.
- Enemy objects with wandering AI.
- Enemy damage and player health tracking.
- Enemy defeat tracking separate from coin tracking.
- NPC dialogue and interaction prompts.
- Day/night cycle with different ambience behavior.
- HUD with objective text, coin progress, enemy progress, weapon status, health icons, compass, prompts, and dialogue.
- Win condition: collect 20 coins and defeat 5 enemies.
- Lose condition: health reaches zero.
- Restart scene and restart button.

## Checkpoint 1 Progress

- Playable 3D world/level: complete.
- Player and camera setup: complete.
- At least three interactive/gameplay objects: complete.
- Interaction system and UI feedback: complete.
- Documentation and playability: updated in this README.

## Checkpoint 2 Progress

- Five or more gameplay objects: coins, weapon pickups, enemies, NPCs, and player resources.
- Three or more interaction systems: collect coins, collect/equip weapons, shoot enemies, talk to NPCs.
- Progress tracking: coin counter, enemy counter, health, weapon status.
- Challenge systems: wandering enemies, enemy damage/health loss, required enemy defeats, required coin target.
- Clear goal: collect 20 coins and defeat 5 enemies.
- Completion state: level clear message and restart scene.
- Audio/visual polish: day/night ambience, background music, terrain details, imported low-poly assets, lighting, and post-processing.

## Known Issues

- The project is still a prototype, so enemy behavior and combat feedback are simple.
- Some objects are runtime-spawned by scripts when the main scene starts.
- UI uses Unity legacy `Text` components for fast checkpoint implementation.
- The current win flow loads the restart scene after a short delay instead of showing a polished end screen.

## Included Audio

The project includes these audio files in `Assets/Audio` for ambience, music, and placeholder sound design:

- `leberch-atmospheric-documentary-509386.mp3`
- `capaholiczsfx-forest-daytime-446356.mp3`
- `eryliaa-night-forest-with-frogs-and-crickets-for-sleep-451153.mp3`

## External Assets and Resources

- Low Poly Environment Nature Free:
  `https://assetstore.unity.com/packages/3d/environments/low-poly-environment-nature-free-lowpoly-medieval-fantasy-series-187052`
- Starter Assets Third Person URP:
  `https://assetstore.unity.com/packages/essentials/starter-assets-thirdperson-urp-196526`
- Imported placeholder weapon models in `Assets/Collectibles`.
- Imported placeholder enemy/NPC/animal models in `Assets/Enemies`, `Assets/NPC`, and `Assets/Animals`.

## Repository

GitHub repository:

`https://github.com/BrianTruong23/3d-final-project-game-course`
