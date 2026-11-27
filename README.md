# AI for Games

**Module:** CS4096 — AI for Games
**Engine:** Unity 6000.6.0f1
**Render Pipeline:** Universal Render Pipeline (URP)

## Overview

A group-developed survival horror prototype featuring procedural
environment generation, sensory-based enemy AI, grid-based
pathfinding and dynamic gameplay systems.

The intended gameplay involves exploring a forest, entering caves,
collecting keys and avoiding creatures with different sensory
capabilities.

## Contributors

| Contributor | Development Branches |
|---|---|
| [Miriam-Codes](https://github.com/Miriam-Codes) | [Hearing Monster AI (Final Demonstration)](https://github.com/MuireannEgan/AI-for-games/tree/feature/hearing-monster-ai), [Procedural Generation](https://github.com/MuireannEgan/AI-for-games/tree/feature/procedural-generation), [Cave Environment](https://github.com/MuireannEgan/AI-for-games/tree/feature/cave-environment) |
| [MuireannEgan](https://github.com/MuireannEgan) | [Main] (https://github.com/MuireannEgan/AI-for-games) |
| [NoviceTechIE-ttv](https://github.com/NoviceTechIE-ttv) | [GameDirector/ProgressManager Script/Object Folder](https://github.com/MuireannEgan/AI-for-games/tree/GameDirector-ProgressManager/Assets/Scripts/GameDirector) |
| [maciekLawinski](https://github.com/maciekLawinski) | [Vision] (https://github.com/MuireannEgan/AI-for-games/tree/vision/Assets/Scripts/AI/Monsters/Vision) |

## Running the Project

Individual contribution details, source files and testing
instructions are available in the respective development branches.

For individual assessment, follow the instructions provided
by each contributor.

## Project Status

The individual systems have been developed, but the complete
intended gameplay loop is not fully integrated.

## Asset Attribution

[Third-Party Asset Credits](Assets/Documentation/THIRD_PARTY_ASSETS.md)


>
>
>
# Individual Contribution - Monster AI and scent detection

**Module:** CS4096 — AI for Games
**University:** University of Limerick
**Developer :** Muireann Egan (24429988)
>
## Overview

My individual contribution to this project focused on developing a reusable monster AI framework, custom pathfinding and a scent based monster 

>
## Individual Contributions
- Reusable monster AI framework
- Finite state machine
- Utility-based decision-making system
- Monster memory and last known position tracking
- Custom grid-based A* pathfinding
- Player scent emission and scent decay
- Hound movement and behaviour
>
>
## Running the project
1. Clone the git repository
2. Open Unity Hub
3. Add the cloned project using the add project option
4. Open the project using Unity 6000.6.0f1
5. Open the Forest_Level scene
6. Press play
7. Enter a dungeon and hope its the hound one
8. hint : the hound always spawns in the second dungeon
>
>
##Testing Individual Features
>
### Patrol behaviour
Watch the hound from a distance before it detects the player's scent. The hound will patrol its assigned area.
>
### Scent detection
Move around the dungeon to generate a scent trail. When the hound detects the trail it will either investigate or chase the player.
>
### Monster memory
Due to the fact i was unable to implement the fountains you will need to manually turn off the script "ScentEmitter" for "playerCapsule" as seen in the heirarchy on the left. You can now watch as the hound will go to your last known area and either investigate or return to its patrol.
>
###Pathfinding 
Move around obstacles and through different areas of the dungeon and watch how the hound chases.
>
###Utility-Based decision making
Open the unity console to observe the state transitions between patrol, investgate, chase, search and return
>
##Relevant Source Files
| Script | Purpose |
|---|---|
| MonsterAI.cs | Controls monster behaviour and state transitions|
| DecisionMaker.cs | Selects the most appropriate action |
| Calculator.cs | Calculates utility scores |
| MonsterMemory.cs | Stores player's last known location |
|MonsterMovement.cs | Handles custom pathfinding and movement |
| ScentPerception.cs | Detects player scent  |
| ScentEmitter.cs | Generates the player's scent trail |
| ScentTrailManager.cs | Manages scent points and their decay times |
| ScentPoint.cs | represents individual scent points | 
>
### Debugging
The unity console can be used to observe state transitions, generated navigation grids and calculated paths during gameplay.
>
###Known Limitations
The custom pathfinding system may experience some difficulties navigating close to certain obstacles.


