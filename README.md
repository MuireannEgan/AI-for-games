\# Procedural Environment Generation and Hearing-Based Enemy AI



\*\*Module:\*\* CS4096 — AI for Games  

\*\*University:\*\* University of Limerick  

\*\*Developer:\*\* Miriam Mohammed (\[Miriam-Codes](https://github.com/Miriam-Codes))  

\*\*Engine:\*\* Unity 6000.6.0f1  

\*\*Render Pipeline:\*\* Universal Render Pipeline (URP)



\## Overview



This branch contains the final playable demonstration of my individual contributions to a group-developed survival horror prototype.



The player explores a procedurally generated forest, enters a cave, retrieves a key and attempts to escape a hearing-based creature.



My contributions focus on procedural environment generation, cave construction, hearing-based enemy perception, player stealth mechanics and gameplay integration.



This branch contains the complete Unity project, including shared systems developed by other team members. It is the recommended branch for assessing and testing my individual contributions.



\## Individual Contributions



\### Procedural Environment Generation



\- Seeded procedural forest generation.

\- Constrained placement of three cave entrances.

\- Central altar and landmark placement.

\- Catmull–Rom spline-based path generation.

\- Variable-width path meshes.

\- Vegetation placement, spacing and path clearance.

\- Normal, autumn and winter environments.

\- Seed and season persistence between scene transitions.

\- Environmental lighting, fog and post-processing.

\- Asset sourcing, organisation and integration.



\### Cave Environment



\- Construction of a reusable cave interior.

\- Modular room and corridor configuration.

\- Environmental prop and furniture placement.

\- Wall, floor and roof configuration.

\- Collision setup and correction.

\- Player spawn configuration.

\- Cave entrance alignment.

\- Forest-to-cave and cave-to-forest transitions.

\- Integration of third-party dungeon assets.



\### Hearing-Based Enemy AI



\- Event-driven noise perception.

\- Distance-based stimulus detection and confidence.

\- Player noise emission based on movement.

\- Different noise radii for walking, running, jumping, landing and crouching.

\- Monster animation and audio integration.

\- Alert scream and attack behaviour.

\- Integration with the shared AI architecture and grid-based navigation system.



\### Player Stealth and Gameplay Integration



\- Crouching with reduced movement speed and noise.

\- Character controller and camera height adjustment.

\- Overhead clearance checks before standing.

\- Progressive breathing stress during prolonged crouching.

\- Dynamic breathing audio feedback.

\- Scene transition fades.

\- Game-over and successful escape screens.

\- Restart functionality with a new forest generation session.



\## Technical Implementation



\### Procedural Generation



The forest is generated using a seeded random number generator and spatial constraints.



Cave entrance positions are selected from shuffled candidate locations using minimum distance requirements. This prevents landmarks from being placed too close together or too close to the central altar.



Catmull–Rom interpolation generates curved paths through intermediate waypoints. Vegetation placement uses spacing and clearance calculations to reduce overlap and obstruction of generated routes.



The generator supports three seasonal environments:



\- Normal

\- Autumn

\- Winter



The active seed and season are preserved during scene transitions. Restarting begins a new generation session and advances the seasonal cycle.



\### Cave Environment



The cave interior is handcrafted using modular dungeon assets.



A static interior was selected to maintain control over room connectivity, collision geometry and encounter space within the available development time.



The procedural forest connects to this reusable environment through trigger-based scene transitions.



\### Hearing Perception



Player actions generate noise events containing a world position and noise radius.



The hearing system determines whether the monster is within the effective detection radius.



&#x20;   Effective radius = min(hearing radius, noise radius)



For detected stimuli, confidence is calculated using:



&#x20;   Confidence = clamp(1 - distance / effective radius, 0, 1)



The latest detected position, confidence and timestamp are made available to the shared AI system.



Noise generation is separated from audible audio playback, allowing perception parameters to be adjusted independently.



\### Progressive Breathing



Crouching initially reduces movement speed and footstep noise. However, prolonged crouching gradually increases breathing intensity and AI-detectable noise.



Two breathing audio clips are crossfaded to communicate increasing detection risk to the player.



Default script parameters:



| Parameter | Default Value |

|---|---:|

| Crouching speed | 1.4 Unity units/s |

| Breathing delay | 8 seconds |

| Breathing buildup duration | 12 seconds |

| Maximum breathing noise radius | 12 Unity units |

| Breathing noise emission interval | 1.5 seconds |



These values can be adjusted through the Unity Inspector.



The mechanic discourages indefinite hiding without forcibly removing player control.



\## Individual Source Files



The following files contain my individual implementations or project configuration work.



\### Procedural Generation



| File | Responsibility |

|---|---|

| \[ForestGenerator.cs](Assets/Scripts/ProceduralGeneration/ForestGenerator.cs) | Coordinates forest generation, seed and seasonal state |

| \[ForestGenerationContext.cs](Assets/Scripts/ProceduralGeneration/ForestGenerationContext.cs) | Shared generation configuration and context |

| \[ForestAssetDatabase.cs](Assets/Scripts/ProceduralGeneration/ForestAssetDatabase.cs) | Environmental asset categorisation and weighted selection |

| \[LandmarkGenerator.cs](Assets/Scripts/ProceduralGeneration/LandmarkGenerator.cs) | Altar placement and constrained cave generation |

| \[PathGenerator.cs](Assets/Scripts/ProceduralGeneration/PathGenerator.cs) | Catmull–Rom interpolation and path mesh generation |

| \[TerrainGenerator.cs](Assets/Scripts/ProceduralGeneration/TerrainGenerator.cs) | Terrain generation |

| \[VegetationGenerator.cs](Assets/Scripts/ProceduralGeneration/VegetationGenerator.cs) | Vegetation placement |

| \[VegetationClearance.cs](Assets/Scripts/ProceduralGeneration/VegetationClearance.cs) | Path clearance calculations |

| \[VegetationSpacing.cs](Assets/Scripts/ProceduralGeneration/VegetationSpacing.cs) | Vegetation separation |

| \[SeasonalEnvironmentStyler.cs](Assets/Scripts/ProceduralGeneration/SeasonalEnvironmentStyler.cs) | Seasonal ground and path material configuration |



\### Hearing-Based AI



| File | Responsibility |

|---|---|

| \[HearingPerception.cs](Assets/Scripts/AI/Monsters/Hearing/HearingPerception.cs) | Noise detection, stimulus confidence and detection persistence |

| \[HearingNoiseEmitter.cs](Assets/Scripts/AI/Monsters/Hearing/HearingNoiseEmitter.cs) | Footstep, movement, jumping and landing noise events |

| \[HearingMonsterAnimator.cs](Assets/Scripts/AI/Monsters/Hearing/HearingMonsterAnimator.cs) | Monster animation and audio integration |

| \[HearingMonsterEncounter.cs](Assets/Scripts/AI/Monsters/Hearing/HearingMonsterEncounter.cs) | Alert scream, attack conditions and defeat behaviour |

| \[HearingMonster.prefab](Assets/Characters/Monsters/Hearing/Prefabs/HearingMonster.prefab) | Configured hearing-based enemy |



\### Player and Scene Transitions



| File | Responsibility |

|---|---|

| \[PlayerCrouchBreathing.cs](Assets/Scripts/Player/PlayerCrouchBreathing.cs) | Crouching, movement adjustments, breathing stress and audio feedback |

| \[DungeonPortal.cs](Assets/Scripts/SceneTransitions/DungeonPortal.cs) | Trigger-based scene transitions and cave exit logic |

| \[ScreenTransition.cs](Assets/Scripts/SceneTransitions/ScreenTransition.cs) | Scene fades, ending screens and restart functionality |



\### Environment and Configuration



| File | Contribution |

|---|---|

| \[Forest\_Level.unity](Assets/Scenes/Forest\_Level.unity) | Forest environment and procedural generator configuration |

| \[Cave\_Level.unity](Assets/Scenes/Cave\_Level.unity) | Cave construction, player setup and encounter integration |

| \[AltarPlaceholder.prefab](Assets/Environment/Altar/Prefabs/AltarPlaceholder.prefab) | Altar asset and landmark configuration |

| \[CavePlaceholder.prefab](Assets/Environment/Caves/Prefabs/CavePlaceholder.prefab) | Cave entrance asset and portal configuration |

| \[ForestGlobalVolume Profile.asset](Assets/Scenes/Forest\_Level/ForestGlobalVolume%20Profile.asset) | Forest post-processing configuration |

| \[EditorBuildSettings.asset](ProjectSettings/EditorBuildSettings.asset) | Forest and cave scene registration |

| \[TagManager.asset](ProjectSettings/TagManager.asset) | Environmental collision layer configuration |



The scenes and project configuration files also contain contributions from other team members.



\## Shared Systems and Dependencies



The following systems were developed by other team members and are included as dependencies of the playable demonstration:



\- Base monster AI architecture.

\- Utility-based decision-making and memory.

\- Grid-based A\* pathfinding and movement.

\- Game Director.

\- Key spawning, collection and progression.



My hearing perception and encounter systems integrate with the shared AI architecture.



The first-person controller is provided by Unity Starter Assets, with additional player stealth functionality implemented separately.



Third-party models, textures, animations and audio are credited in the asset documentation.



\## Getting Started



\### Requirements



\- Unity Hub

\- Unity Editor 6000.6.0f1

\- Universal Render Pipeline (URP)

\- Keyboard and mouse



The required Unity packages are specified in the project configuration.



\### Installation



1\. Download the branch as a ZIP using the link below.

2\. Extract the archive.

3\. Open Unity Hub.

4\. Select Add project from disk.

5\. Select the extracted project folder containing Assets, Packages and ProjectSettings.

6\. Open the project with Unity 6000.6.0f1.

7\. Allow Unity to import assets and resolve packages.

8\. Open `Assets/Scenes/Forest\_Level.unity`.

9\. Enter Play Mode.



Alternatively, clone the branch using:



&#x20;   git clone --branch feature/hearing-monster-ai --single-branch https://github.com/MuireannEgan/AI-for-games.git



Both Forest\_Level and Cave\_Level are registered in the project's build scene configuration.



\## Controls



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



\## Testing Guide



\### Test 1 — Procedural Forest



1\. Open Forest\_Level and enter Play Mode.

2\. Locate the central altar.

3\. Explore the three generated cave entrances.

4\. Follow the curved paths between landmarks.

5\. Observe vegetation placement and environmental clearance.



Expected behaviour:



\- Cave entrances are distributed around the central altar.

\- Generated paths connect the landmarks.

\- Vegetation is positioned using spacing and clearance constraints.



\### Test 2 — Seeded Generation



1\. Stop Play Mode.

2\. Locate the ForestGenerator component in the Inspector.

3\. Disable Use Random Seed.

4\. Enter a fixed seed value.

5\. Enter Play Mode and observe the layout.

6\. Repeat using the same seed.



The same seed and generation configuration should reproduce the same spatial layout. Seasonal assets may differ as the seasonal cycle advances.



Re-enable Use Random Seed to restore normal layout variation between new sessions.



\### Test 3 — Cave Environment



1\. Start from Forest\_Level.

2\. Follow a generated path towards an accessible cave.

3\. Walk into the entrance trigger.

4\. Explore the cave rooms and corridors.

5\. Test movement and jumping around environmental geometry.



The cave interior can also be inspected directly by opening Cave\_Level in the Unity Editor.



\### Test 4 — Hearing-Based Enemy



1\. Enter the cave containing the hearing-based creature.

2\. Walk within its hearing range.

3\. Compare walking with sprinting and jumping.

4\. Stop moving and observe its response.

5\. Crouch and compare the response to quieter movement.



The creature responds to emitted noise events. Stopping movement prevents new footstep noise but does not immediately clear previously detected stimuli from AI memory.



For debugging, enable Log Noise Events on the HearingNoiseEmitter component to display emitted noise radii in the Unity Console.



\### Test 5 — Progressive Breathing



1\. Hold Left Ctrl or C to crouch.

2\. Remain crouched for longer than eight seconds.

3\. Listen to the increasing breathing intensity.

4\. Observe the creature's response when within effective hearing range.

5\. Release the crouch input and observe the breathing audio fading out.



Detection depends on distance, breathing noise radius, hearing range and noise emission timing.



\### Test 6 — Escape



1\. Explore the cave.

2\. Locate and collect the key.

3\. Avoid the hearing-based creature.

4\. Proceed to the cave exit.



Expected behaviour:



\- The shared progression system registers the collected key.

\- Reaching the exit after collecting the key triggers the successful escape screen.



\### Test 7 — Game Over and Restart



1\. Allow the creature to detect and catch the player.

2\. Observe the attack animation and game-over screen.

3\. Select Try Again.

4\. Observe the newly generated forest.



Restarting resets the forest generation session and advances the seasonal cycle.



Repeated restarts demonstrate the normal, autumn and winter environments.



\## Known Limitations



\- Hearing detection uses distance rather than geometry-aware acoustic propagation.

\- Elevated surfaces may introduce navigation limitations.

\- The cave interior is handcrafted rather than procedurally generated.

\- Vegetation clearance does not account for every individual mesh footprint.

\- Greedy cave placement may fail under restrictive distance settings.

\- Standing immediately resets breathing stress.

\- Evaluation was primarily conducted through qualitative playtesting.

\- The complete intended three-key, multi-cave progression is not fully integrated.



This branch demonstrates the procedural environment and individual hearing-based encounter rather than the complete intended group game.



\## Development History



My contributions were developed across three feature branches.



| Branch | Development Focus |

|---|---|

| \[feature/procedural-generation](https://github.com/MuireannEgan/AI-for-games/tree/feature/procedural-generation) | Procedural forest, landmarks, paths, vegetation and seasonal environments |

| \[feature/cave-environment](https://github.com/MuireannEgan/AI-for-games/tree/feature/cave-environment) | Cave construction, collision configuration and scene transitions |

| \[feature/hearing-monster-ai](https://github.com/MuireannEgan/AI-for-games/tree/feature/hearing-monster-ai) | Hearing AI, stealth mechanics and final playable individual demonstration |



The procedural generation and cave environment branches are retained as evidence of earlier development stages.



\## Asset Attribution



Third-party assets retain their respective licences.



Full source links, creator information, usage and modification details are available in:



\[Third-Party Asset Documentation](Assets/Documentation/THIRD\_PARTY\_ASSETS.md)



Imported artwork, animations and audio are not claimed as original assets.



\## Source Code and Download



\*\*Recommended version for individual assessment:\*\*



\[Download the Hearing Monster AI branch as a ZIP](https://github.com/MuireannEgan/AI-for-games/archive/refs/heads/feature/hearing-monster-ai.zip)



\[View individual commit history](https://github.com/MuireannEgan/AI-for-games/commits/feature/hearing-monster-ai/)



\[Return to the main project](https://github.com/MuireannEgan/AI-for-games/tree/main)



\---



Developed for CS4096 — AI for Games  

University of Limerick, Autumn 2026



