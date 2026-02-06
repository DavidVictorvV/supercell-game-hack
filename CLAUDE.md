# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity game project for a hackathon: "Daniel in the Lions' Den" - a 2.5D action-adventure game where the player observes and influences events from the biblical story. The player is an observer/guardian entity navigating obstacles and puzzles while the story unfolds dynamically.

## Unity Development Commands

### Opening and Running the Project
- Open the project in Unity Hub or Unity Editor (Unity 2022.3+)
- Main scene: `Assets/Scenes/SampleScene.unity`
- Play mode: Use Unity Editor's Play button or `Ctrl+P` / `Cmd+P`

### Testing
The project uses Unity Test Framework. To run tests:
```bash
# From Unity Editor: Window > General > Test Runner
# Tests are located in Assets/Plugins/GameCreator/Packages/Core/Tests/
```

**Note**: Tests primarily exist for the GameCreator framework itself. Custom game logic tests would need to be added to a separate test assembly.

### Building
- Build via Unity Editor: File > Build Settings
- Target platforms configured in Build Settings
- Build player via: File > Build and Run

## Architecture and Key Systems

### GameCreator 2 Framework
This project is built on **GameCreator 2**, a visual scripting framework. Understanding this is critical:

**Core Concepts:**
- **Instructions**: Executable actions/commands that run sequentially or with custom flow control
  - Base class: `GameCreator.Runtime.VisualScripting.Instruction`
  - Async/await based execution model
  - Located in: `Assets/Plugins/GameCreator/Packages/Core/Runtime/VisualScripting/Instructions/`

- **Conditions**: Boolean checks used for branching logic
  - Base class: `GameCreator.Runtime.VisualScripting.Condition`
  - Support for AND/OR logic branches

- **Events**: Triggers that start instruction sequences
  - Located in: `Assets/Plugins/GameCreator/Packages/Core/Runtime/VisualScripting/Events/`

- **Variables**: Type-safe variable system (Local, Global, List)
  - Support for GameObjects, Numbers, Strings, Colors, etc.
  - Accessed via PropertyGet/PropertySet patterns

**When modifying game logic:**
1. Create custom Instructions by extending `Instruction` class
2. Override `protected abstract Task Run(Args args)` method
3. Use `Args` parameter to access runtime context and data
4. Follow GameCreator's async execution pattern

### Input System
Uses Unity's new Input System with predefined actions in `Assets/InputSystem_Actions.inputactions`:
- Player movement (WASD, gamepad)
- Jump, Crouch, Sprint
- Attack, Interact
- Look/Camera controls

Input actions are mapped both for keyboard and gamepad.

### Third-Party Integrations

**Feel (MoreMountains)**
- Feedback system for juicy game feel (camera shake, screen effects, audio)
- Located in: `Assets/Feel/`
- Use `MMFeedbacks` components for visual/audio polish

**DOTween**
- Animation tweening library in `Assets/Plugins/Demigiant/DOTween/`
- Prefer DOTween for simple animations over Unity's Animator

**Odin Inspector**
- Enhanced Unity Inspector attributes
- Use for custom editor workflows and data visualization

### Project Structure

```
Assets/
├── Plugins/
│   ├── GameCreator/          # Visual scripting framework (core logic)
│   ├── Sirenix/              # Odin Inspector
│   └── Demigiant/            # DOTween
├── Feel/                     # MoreMountains feedback system
├── Scenes/                   # Unity scenes
│   └── SampleScene.unity     # Main game scene
├── Resources/                # Runtime-loaded assets
├── Settings/                 # Project settings
└── InputSystem_Actions.inputactions  # Input configuration
```

### Rendering Pipeline
- **Universal Render Pipeline (URP)** version 17.3.0
- Shader Graph for custom shaders
- Post-processing effects configured per-camera

## Development Workflow

### Adding New Gameplay Features
1. **For visual scripting users**: Create Instructions/Conditions/Events in Unity Editor using GameCreator's visual tools
2. **For C# developers**:
   - Extend `Instruction` for new actions
   - Extend `Condition` for new checks
   - Use GameCreator's property system for variable access

### Level Design Flow (from Plan.md)
The game is divided into 5 zones with story triggers:
1. Zone 1: Entrance/Tutorial
2. Zone 2: Trap Corridor
3. Zone 3: Rest/Story Observation
4. Zone 4: Guards Enter/Multiple Paths
5. Zone 5: Lions' Den/Guardian Role

Each zone uses empty GameObjects as triggers for story events and AI dialogue.

### AI Angel System
Dynamic commentary system planned for the hackathon:
- Tracks player performance and failed attempts
- Context-aware dialogue based on player actions
- Hardcoded dialogue first, AI integration (Neocortex/Mistral) planned for later

## Common Patterns

### Accessing GameObjects and Components
GameCreator uses a property-based system:
```csharp
// Instead of direct GameObject references, use PropertyGet patterns:
PropertyGetGameObject target;
GameObject obj = target.Get(args);
```

### Running Instructions Programmatically
```csharp
InstructionList instructions = new InstructionList();
await instructions.Run(args);
```

### Variable Access
```csharp
// Get variable value
float health = this.m_PropertyGet.Get(args);

// Set variable value
this.m_PropertySet.Set(newValue, args);
```

## Important Notes

- GameCreator uses **async/await** extensively - all custom Instructions must be async
- The `Args` object carries runtime context - always pass it through execution chains
- Visual scripting assets (.asset files) are serialized and should not be manually edited in text editors
- Input actions are configured in the .inputactions file - regenerate C# class after changes
- URP settings are in `Assets/Settings/` - changes to rendering require URP configuration

## Asset Guidelines

- Character models: Kevin Iglesias Human Animations pack
- UI: Artsystack Fantasy RPG GUI
- Audio: Casual Game Sounds, Footsteps Pack
- Use abstract/low-poly visuals for lions and Daniel (per hackathon plan)

## Performance Considerations

- GameCreator's visual scripting has overhead - cache frequently accessed values
- Use object pooling for frequently spawned objects (MMSimpleObjectPooler from Feel)
- URP is optimized for mid-range devices - test on target hardware
