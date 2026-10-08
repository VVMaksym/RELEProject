# ARCHITECTURE — Sumo RL

## 1. System Goal

A Unity environment for training and comparing two RL agents. Each agent controls a 3D sumo fighter, tries to remain inside a circular arena, and attempts to push the opponent out.

## 2. Architectural Priorities

1. Reach a complete `round start → agent actions → winner → reset` loop as quickly as possible.
2. Keep game physics separate from ML-Agents so the game can be validated without training.
3. Make experiments reproducible: configuration, seed, model version, and metrics are stored outside the scene.
4. Keep ragdoll physics, joints, animation, and multiple arenas out of the MVP.

## 3. Technical Context

- Canonical Unity project: `RELESUMO/`, created from the Unity 6 Universal 3D template with URP `17.4.0`.
- The existing `PC_Renderer` and `Mobile_Renderer` assets provide the 3D rendering setup; B001 only needs to verify the active quality/render-pipeline assignment.
- Physics: `Rigidbody`, `CapsuleCollider`, gravity enabled, with gameplay on the horizontal XZ plane and Y as the vertical axis.
- Presentation: a fixed elevated perspective camera for the MVP; camera placement does not affect simulation coordinates or observations.
- RL: Unity ML-Agents; the package has not yet been added to `Packages/manifest.json`.
- Training: Python trainer (PPO), first against a simple scripted opponent, then through self-play.
- Main MVP scene: `Assets/_Project/Scenes/SumoTraining.unity`.

## 4. Runtime Components

| Component | Responsibility | Must not |
|---|---|---|
| `MatchCoordinator` | Starts/resets rounds, determines win/loss/draw, and ends episodes | Move fighters or calculate ML observations |
| `ArenaBoundary` | Stores the center/radius and fall threshold, projects positions onto XZ, detects ring-outs, and provides normalized edge distance | Assign rewards |
| `SumoMotor` | Converts a planar move vector plus dash/shove/brace flags into physics; owns aim direction, stamina, cooldowns, contact state, and upright constraints | Know about `Agent`, rewards, or match results |
| `SumoAgent` | ML-Agents adapter for observations, actions, heuristic input, and terminal rewards | Reset the scene or directly move transforms |
| `RewardPolicy` | Single source for dense/terminal reward coefficients | Determine physics or the winner |
| `ScriptedOpponent` | Simple baseline opponent for validation and the initial curriculum | Be part of the final learned policy |
| `MatchHUD` | Displays score, timer, mode, and result | Affect the simulation |

`SumoAgent` and `ScriptedOpponent` submit the same `SumoCommand` to `SumoMotor`. Manual control, the scripted baseline, and the neural policy therefore exercise the same physics.

## 5. Episode Contract

### Start

- Two fighters are placed symmetrically around the center with small randomized XZ position and yaw offsets.
- Linear velocity, angular velocity, cooldowns, and accumulated state are cleared; both fighters are restored to an upright orientation.
- Evaluation mode uses an explicitly configured random seed.

### Termination

- Win: the opponent's center projected onto XZ leaves the arena radius or falls below the configured Y threshold.
- Loss: the agent's own center projected onto XZ leaves the arena radius or falls below the configured Y threshold.
- Draw: both fighters leave during the same physics step, or the time limit expires.
- `MatchCoordinator` is the single source of truth for episode termination.

## 6. Agent Contract

### Normalized Observations (future ML integration)

- own position relative to the arena center `(x, z)`;
- own planar velocity `(vx, vz)`;
- current/most-recent movement direction as `aimDir (ax, az)`;
- opponent relative position `(dx, dz)` and relative velocity `(dvx, dvz)`;
- distance to the opponent and distance to the nearest arena edge;
- opponent-contact flag;
- normalized stamina, dash cooldown, shove cooldown, and brace state;
- normalized time remaining when the match uses a timer.

Gameplay exposes this state without depending on ML-Agents. Normalization and sensor collection belong to the later agent adapter.

### Actions

- continuous: planar movement `[move_x, move_z]`, each input in `[-1, 1]` and the resulting vector clamped to unit length;
- discrete: `dash`, `shove`, and `brace`, each in `{0, 1}`;
- dash and shove use the current or most recent non-zero movement direction (`aimDir`);
- brace has no direction, blocks the fighter's own dash/shove, slows movement, and increases resistance to incoming physics.

### Rewards

- `+1` for a win, `-1` for a loss, and a small penalty for a draw;
- a small penalty per decision step to discourage inactivity;
- bounded shaping for increasing the opponent's radial distance while decreasing the agent's own;
- no persistent reward for contact alone, preventing exploitation of collisions without progress.

All coefficients live in `RewardPolicy` or configuration rather than being scattered across MonoBehaviours.

## 7. File Structure

```text
RELESUMO/
├─ Assets/_Project/
│  ├─ Scenes/                 # SumoTraining.unity
│  ├─ Prefabs/                # Arena, SumoAgent
│  ├─ Scripts/
│  │  ├─ Core/                # MatchCoordinator, shared contracts
│  │  ├─ Gameplay/            # ArenaBoundary, SumoMotor
│  │  ├─ Agents/              # SumoAgent, ScriptedOpponent
│  │  └─ UI/                  # MatchHUD
│  ├─ Settings/               # 3D renderer, physics/material/reward assets
│  └─ Tests/{EditMode,PlayMode}/
└─ Training/
   ├─ Configs/                # PPO and self-play YAML
   ├─ Results/                # gitignored run output
   └─ Models/                 # selected .onnx checkpoints
```

## 8. Module Boundaries and Invariants

- Core/Gameplay does not depend on the ML-Agents API; dependencies point from `Agents` toward gameplay contracts.
- Only `MatchCoordinator` changes round state; a guard flag prevents duplicate simultaneous termination.
- Fighters move through `Rigidbody` during `FixedUpdate`, never through direct `Transform` movement during a round.
- The MVP locks X/Z body rotation so agents remain upright while retaining Y-axis rotation and full positional movement; a learned balance controller belongs to the post-MVP ragdoll scope.
- Arena geometry, forces, mass, cooldowns, and reward coefficients are configurable without code changes.
- Training and evaluation modes use the same prefab and physics.

## 9. MVP Definition of Done

The MVP is complete when two agents can run at least 1,000 automated episodes without hangs or reset errors, a trained policy consistently defeats a random baseline opponent, and a new training run can be reproduced by following the repository instructions.

## 10. Out of Scope for the MVP

Ragdoll stickmen, learned balance, individual limb control, networked play, complex attacks, multiple arena types, production art/audio, and mobile optimization. These may be added only after baseline training is stable.
