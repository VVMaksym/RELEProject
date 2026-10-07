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
| `SumoMotor` | Converts movement commands into planar force, yaw rotation, and shove impulses; controls cooldowns and upright constraints | Know about `Agent`, rewards, or match results |
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

### Normalized Observations

- the agent's local planar velocity `(x, z)`, vertical velocity, and yaw angular velocity;
- local planar vector to the arena center and normalized distance to the edge;
- opponent's local relative position and velocity in 3D;
- opponent's facing direction in local space;
- shove readiness and normalized remaining time.

Observations do not contain world-space coordinates that would tie the policy to a specific arena orientation.

### Actions

- continuous: forward/backward movement `[-1, 1]`;
- continuous: yaw rotation `[-1, 1]`;
- continuous: shove/dash strength `[0, 1]`, with cooldown enforced by `SumoMotor`.

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
