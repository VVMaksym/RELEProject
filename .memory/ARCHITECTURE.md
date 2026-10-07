# ARCHITECTURE — Sumo RL

## 1. System Goal

A Unity environment for training and comparing two RL agents. Each agent controls a 2D sumo fighter, tries to remain inside a circular arena, and attempts to push the opponent out.

## 2. Architectural Priorities

1. Reach a complete `round start → agent actions → winner → reset` loop as quickly as possible.
2. Keep game physics separate from ML-Agents so the game can be validated without training.
3. Make experiments reproducible: configuration, seed, model version, and metrics are stored outside the scene.
4. Keep ragdoll physics, joints, animation, and multiple arenas out of the MVP.

## 3. Technical Context

- Unity `6000.4.0f1`, Universal Render Pipeline 2D.
- Physics: `Rigidbody2D`, `Collider2D`, zero gravity, top-down view.
- RL: Unity ML-Agents; the package has not yet been added to `Packages/manifest.json`.
- Training: Python trainer (PPO), first against a simple scripted opponent, then through self-play.
- Main MVP scene: `Assets/_Project/Scenes/SumoTraining.unity`.

## 4. Runtime Components

| Component | Responsibility | Must not |
|---|---|---|
| `MatchCoordinator` | Starts/resets rounds, determines win/loss/draw, and ends episodes | Move fighters or calculate ML observations |
| `ArenaBoundary` | Stores the center/radius, detects when a fighter leaves the arena, and provides normalized edge distance | Assign rewards |
| `SumoMotor` | Converts movement commands into force/rotation/shove and controls cooldowns | Know about `Agent`, rewards, or match results |
| `SumoAgent` | ML-Agents adapter for observations, actions, heuristic input, and terminal rewards | Reset the scene or directly move transforms |
| `RewardPolicy` | Single source for dense/terminal reward coefficients | Determine physics or the winner |
| `ScriptedOpponent` | Simple baseline opponent for validation and the initial curriculum | Be part of the final learned policy |
| `MatchHUD` | Displays score, timer, mode, and result | Affect the simulation |

`SumoAgent` and `ScriptedOpponent` submit the same `SumoCommand` to `SumoMotor`. Manual control, the scripted baseline, and the neural policy therefore exercise the same physics.

## 5. Episode Contract

### Start

- Two fighters are placed symmetrically around the center with small randomized position and rotation offsets.
- Linear velocity, angular velocity, cooldowns, and accumulated state are cleared.
- Evaluation mode uses an explicitly configured random seed.

### Termination

- Win: the opponent's center leaves the arena radius.
- Loss: the agent's own center leaves the arena radius.
- Draw: both fighters leave during the same physics step, or the time limit expires.
- `MatchCoordinator` is the single source of truth for episode termination.

## 6. Agent Contract

### Normalized Observations

- the agent's local linear velocity `(x, y)` and angular velocity;
- local vector to the arena center and normalized distance to the edge;
- opponent's local relative position and velocity;
- opponent's facing direction in local space;
- shove readiness and normalized remaining time.

Observations do not contain world-space coordinates that would tie the policy to a specific arena orientation.

### Actions

- continuous: forward/backward movement `[-1, 1]`;
- continuous: rotation `[-1, 1]`;
- continuous: shove/dash strength `[0, 1]`, with cooldown enforced by `SumoMotor`.

### Rewards

- `+1` for a win, `-1` for a loss, and a small penalty for a draw;
- a small penalty per decision step to discourage inactivity;
- bounded shaping for increasing the opponent's radial distance while decreasing the agent's own;
- no persistent reward for contact alone, preventing exploitation of collisions without progress.

All coefficients live in `RewardPolicy` or configuration rather than being scattered across MonoBehaviours.

## 7. File Structure

```text
RELEGame/
├─ Assets/_Project/
│  ├─ Scenes/                 # SumoTraining.unity
│  ├─ Prefabs/                # Arena, SumoAgent
│  ├─ Scripts/
│  │  ├─ Core/                # MatchCoordinator, shared contracts
│  │  ├─ Gameplay/            # ArenaBoundary, SumoMotor
│  │  ├─ Agents/              # SumoAgent, ScriptedOpponent
│  │  └─ UI/                  # MatchHUD
│  ├─ Settings/               # physics/material/reward assets
│  └─ Tests/{EditMode,PlayMode}/
└─ Training/
   ├─ Configs/                # PPO and self-play YAML
   ├─ Results/                # gitignored run output
   └─ Models/                 # selected .onnx checkpoints
```

## 8. Module Boundaries and Invariants

- Core/Gameplay does not depend on the ML-Agents API; dependencies point from `Agents` toward gameplay contracts.
- Only `MatchCoordinator` changes round state; a guard flag prevents duplicate simultaneous termination.
- Fighters move through `Rigidbody2D` during physics steps, never through direct `Transform` movement during a round.
- Arena geometry, forces, mass, cooldowns, and reward coefficients are configurable without code changes.
- Training and evaluation modes use the same prefab and physics.

## 9. MVP Definition of Done

The MVP is complete when two agents can run at least 1,000 automated episodes without hangs or reset errors, a trained policy consistently defeats a random baseline opponent, and a new training run can be reproduced by following the repository instructions.

## 10. Out of Scope for the MVP

Ragdoll stickmen, individual limb control, networked play, complex attacks, multiple arena types, production art/audio, and mobile optimization. These may be added only after baseline training is stable.
