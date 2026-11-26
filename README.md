# Procedural Environment Generation and Hearing-Based Enemy AI

**Module:** CS4096 — AI for Games  
**University:** University of Limerick  
**Developer:** Miriam Mohammed ([Miriam-Codes](https://github.com/Miriam-Codes))  
**Engine:** Unity 6000.6.0f1  
**Render Pipeline:** Universal Render Pipeline (URP)

> **Recommended branch for individual assessment:** `feature/hearing-monster-ai`
>
> [Download the playable Unity project (ZIP)](https://github.com/MuireannEgan/AI-for-games/archive/refs/heads/feature/hearing-monster-ai.zip)

## Quick Navigation

| Running the demonstration | Reviewing the implementation |
|---|---|
| [Installation](#getting-started) | [Individual contributions](#individual-contributions) |
| [Controls](#controls) | [Individual source files](#individual-source-files) |
| [Testing guide](#testing-guide) | [Shared systems](#shared-systems-and-dependencies) |
| [Known limitations](#known-limitations) | [Development history](#development-history) |
| [Download and source code](#source-code-and-download) | [Asset attribution](#asset-attribution) |

## Overview

This branch contains the complete Unity project configured to demonstrate my individual contributions to a group-developed survival horror prototype. The player explores a procedurally generated forest, enters a cave, retrieves a key and attempts to escape a hearing-based creature.

Shared systems and third-party assets are included as dependencies; their inclusion does not imply individual authorship.

## Individual Contributions

### Procedural Environment Generation

- Seeded forest generation, with a central altar and three cave entrances selected using shuffled candidates and minimum separation constraints.
- Catmull–Rom spline-based paths, variable-width meshes, vegetation spacing and path clearance.
- Normal, autumn and winter environments, with seasonal assets, lighting, fog and post-processing.
- Seed and season persistence across scene transitions; restarting begins a new generation session.

### Cave Environment

- Reusable handcrafted cave interior, built from modular dungeon assets to control room connectivity and encounter geometry.
- Room and corridor construction, props, collision configuration, entrance alignment and player spawn setup.
- Trigger-based transitions between forest and cave scenes.

### Hearing-Based Enemy AI

- Event-driven noise perception with distance-based stimulus confidence and integration with shared AI memory and decision-making.
- Movement-dependent noise events for walking, running, jumping, landing and crouching.
- Monster prefab configuration, animation, audio, alert scream and attack behaviour.

The hearing system uses the following detection model (for a positive effective radius and a stimulus within range):

```text
Effective radius = min(hearing radius, noise radius)
Confidence = clamp(1 - distance / effective radius, 0, 1)
```

### Player Stealth and Gameplay Integration

- Crouching reduces movement speed, controller height and footstep noise; overhead clearance is checked before standing.
- Prolonged crouching increases AI-detectable breathing noise. Two audio clips crossfade to communicate increasing detection risk without forcing the player to stand.
- Scene fades, successful escape and game-over screens, and restart functionality.

Default script parameters (Inspector values may override these):

| Parameter | Default |
|---|---:|
| Crouching speed | 1.4 Unity units/s |
| Breathing delay | 8 seconds |
| Breathing buildup duration | 12 seconds |
| Maximum breathing noise radius | 12 Unity units |
| Breathing noise emission interval | 1.5 seconds |

## Getting Started

**Requirements:** Unity Hub, Unity 6000.6.0f1 (URP), keyboard and mouse. Required packages are specified in the project configuration.

1. [Download the branch ZIP](https://github.com/MuireannEgan/AI-for-games/archive/refs/heads/feature/hearing-monster-ai.zip) and extract it, or clone the branch using the command below.
2. In Unity Hub, select Add project from disk and choose the folder containing `Assets`, `Packages` and `ProjectSettings`.
3. Open with Unity 6000.6.0f1 and allow assets and packages to import.
4. Open `Assets/Scenes/Forest_Level.unity` and enter Play Mode.

```bash
git clone --branch feature/hearing-monster-ai --single-branch https://github.com/MuireannEgan/AI-for-games.git
```

Both forest and cave scenes are registered in the build scene configuration.

## Controls

| Input | Action |
|---|---|
| W, A, S, D / Arrow keys | Movement |
| Mouse | Look around |
| Left Shift | Sprint |
| Space | Jump |
| Left Ctrl or C (hold) | Crouch |
| Mouse click | Ending screen buttons |

Keys are collected by walking into them.

Cave entrances and exits use trigger-based interactions.

## Testing Guide

Start from `Forest_Level` unless otherwise specified. The following steps describe the expected demonstration behaviour.

| Test | Steps and expected behaviour |
|---|---|
| Procedural forest | Locate the central altar and three cave entrances. Follow the curved paths and inspect vegetation placement and clearance. |
| Seeded generation | Stop Play Mode, disable **Use Random Seed** on `ForestGenerator` and enter a fixed seed. Repeat with the same settings to compare layouts. Seasonal assets may differ; re-enable random seeds afterwards. |
| Cave environment | Enter an accessible cave through its entrance trigger. Explore rooms and corridors; test jumping, wall and floor collisions. Inspect `Cave_Level` directly in the editor if needed. |
| Hearing perception | Walk, sprint and jump near the creature, then stop or crouch. Compare the responses. Stopping prevents new footsteps but does not immediately clear AI memory. |
| Progressive breathing | Hold Left Ctrl or C for longer than eight seconds using the default settings. Listen to the increasing breathing intensity; observe enemy behaviour within effective hearing range. Release crouch and listen to the audio fade. |
| Escape | Collect the cave key, avoid the creature and return to the exit. The shared progression system registers the key; the exit should trigger the successful escape screen. |
| Game over and restart | Allow the creature to catch the player. Observe the attack and game-over screen, then select **Try Again**. Observe the new forest and seasonal variation across restarts. |

**Debugging:** Enable **Log Noise Events** on `HearingNoiseEmitter` to view emitted noise radii in the Unity Console. Detection depends on distance, emitted noise radius, hearing range and event timing.

## Individual Source Files

The following files contain my individual implementations or project configuration work.

### Procedural Generation

| File | Responsibility |
|---|---|
| [ForestGenerator.cs](Assets/Scripts/ProceduralGeneration/ForestGenerator.cs) | Coordinates forest generation, seed and seasonal state |
| [ForestGenerationContext.cs](Assets/Scripts/ProceduralGeneration/ForestGenerationContext.cs) | Shared generation configuration and context |
| [ForestAssetDatabase.cs](Assets/Scripts/ProceduralGeneration/ForestAssetDatabase.cs) | Environmental asset categorisation and weighted selection |
| [LandmarkGenerator.cs](Assets/Scripts/ProceduralGeneration/LandmarkGenerator.cs) | Altar placement and constrained cave generation |
| [PathGenerator.cs](Assets/Scripts/ProceduralGeneration/PathGenerator.cs) | Catmull–Rom interpolation and path mesh generation |
| [TerrainGenerator.cs](Assets/Scripts/ProceduralGeneration/TerrainGenerator.cs) | Terrain generation |
| [VegetationGenerator.cs](Assets/Scripts/ProceduralGeneration/VegetationGenerator.cs) | Vegetation placement |
| [VegetationClearance.cs](Assets/Scripts/ProceduralGeneration/VegetationClearance.cs) | Path clearance calculations |
| [VegetationSpacing.cs](Assets/Scripts/ProceduralGeneration/VegetationSpacing.cs) | Vegetation separation |
| [SeasonalEnvironmentStyler.cs](Assets/Scripts/ProceduralGeneration/SeasonalEnvironmentStyler.cs) | Seasonal ground and path material configuration |

### Hearing-Based AI

| File | Responsibility |
|---|---|
| [HearingPerception.cs](Assets/Scripts/AI/Monsters/Hearing/HearingPerception.cs) | Noise detection, stimulus confidence and detection persistence |
| [HearingNoiseEmitter.cs](Assets/Scripts/AI/Monsters/Hearing/HearingNoiseEmitter.cs) | Footstep, movement, jumping and landing noise events |
| [HearingMonsterAnimator.cs](Assets/Scripts/AI/Monsters/Hearing/HearingMonsterAnimator.cs) | Monster animation and audio integration |
| [HearingMonsterEncounter.cs](Assets/Scripts/AI/Monsters/Hearing/HearingMonsterEncounter.cs) | Alert scream, attack conditions and defeat behaviour |
| [HearingMonster.prefab](Assets/Characters/Monsters/Hearing/Prefabs/HearingMonster.prefab) | Configured hearing-based enemy |

### Player and Scene Transitions

| File | Responsibility |
|---|---|
| [PlayerCrouchBreathing.cs](Assets/Scripts/Player/PlayerCrouchBreathing.cs) | Crouching, movement adjustments, breathing stress and audio feedback |
| [DungeonPortal.cs](Assets/Scripts/SceneTransitions/DungeonPortal.cs) | Trigger-based scene transitions and cave exit logic |
| [ScreenTransition.cs](Assets/Scripts/SceneTransitions/ScreenTransition.cs) | Scene fades, ending screens and restart functionality |

### Environment and Configuration

| File | Contribution |
|---|---|
| [Forest_Level.unity](Assets/Scenes/Forest_Level.unity) | Forest environment and procedural generator configuration |
| [Cave_Level.unity](Assets/Scenes/Cave_Level.unity) | Cave construction, player setup and encounter integration |
| [AltarPlaceholder.prefab](Assets/Environment/Altar/Prefabs/AltarPlaceholder.prefab) | Altar asset and landmark configuration |
| [CavePlaceholder.prefab](Assets/Environment/Caves/Prefabs/CavePlaceholder.prefab) | Cave entrance asset and portal configuration |
| [ForestGlobalVolume Profile.asset](Assets/Scenes/Forest_Level/ForestGlobalVolume%20Profile.asset) | Forest post-processing configuration |
| [EditorBuildSettings.asset](ProjectSettings/EditorBuildSettings.asset) | Forest and cave scene registration |
| [TagManager.asset](ProjectSettings/TagManager.asset) | Environmental collision layer configuration |

The scenes and project configuration files also contain contributions from other team members.

## Shared Systems and Dependencies

The demonstration uses the team's base monster AI, utility-based decision-making and memory, grid-based A* pathfinding, Game Director and key progression systems. These are not claimed as my individual implementations. The first-person controller is provided by Unity Starter Assets; my stealth functionality is implemented separately.

## Known Limitations

- Hearing does not model acoustic occlusion by walls or materials.
- Elevated surfaces can introduce navigation limitations.
- Cave interiors are handcrafted; vegetation clearance does not account for every mesh footprint.
- Greedy cave placement may fail under restrictive distance settings.
- Standing immediately resets breathing stress.
- Evaluation was primarily qualitative playtesting.
- The full intended three-key, multi-cave progression is not fully integrated.

## Development History

My contributions were developed across three feature branches.

| Branch | Development Focus |
|---|---|
| [feature/procedural-generation](https://github.com/MuireannEgan/AI-for-games/tree/feature/procedural-generation) | Procedural forest, landmarks, paths, vegetation and seasonal environments |
| [feature/cave-environment](https://github.com/MuireannEgan/AI-for-games/tree/feature/cave-environment) | Cave construction, collision configuration and scene transitions |
| [feature/hearing-monster-ai](https://github.com/MuireannEgan/AI-for-games/tree/feature/hearing-monster-ai) | Hearing AI, stealth mechanics and final playable individual demonstration |

The procedural generation and cave environment branches are retained as evidence of earlier development stages.

## Asset Attribution

Third-party assets retain their respective licences.

Full source links, creator information, usage and modification details are available in:

[Third-Party Asset Documentation](Assets/Documentation/THIRD_PARTY_ASSETS.md)

Imported artwork, animations and audio are not claimed as original assets.

## Source Code and Download

**Recommended version for individual assessment:**

[Download the Hearing Monster AI branch as a ZIP](https://github.com/MuireannEgan/AI-for-games/archive/refs/heads/feature/hearing-monster-ai.zip)

[View branch commit history](https://github.com/MuireannEgan/AI-for-games/commits/feature/hearing-monster-ai/)

[Return to the main project](https://github.com/MuireannEgan/AI-for-games/tree/main)

---

Developed for CS4096 — AI for Games  
University of Limerick, Autumn 2026
