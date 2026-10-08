# DECISIONS

## 2026-10-07 — D001: Top-down 2D with one physics body [SUPERSEDED BY D006]

**Decision:** The MVP uses one dynamic `Rigidbody2D` per sumo fighter, without ragdolls or joints.

**Reason:** This minimizes the action space and the time required to obtain the first learning policy.

**Impact:** The stickman initially serves as a visual representation of one solid body; limb control is deferred.

**Status:** Superseded before implementation when the team changed the project to 3D.

## 2026-10-07 — D002: Gameplay is isolated from ML-Agents

**Decision:** `SumoMotor`, arena rules, and the match lifecycle do not depend on `Agent`; `SumoAgent` is an adapter.

**Reason:** The same physics must support human input, a scripted baseline, training, and inference.

**Impact:** All controllers submit the same `SumoCommand`; testing does not require the Python trainer.

## 2026-10-07 — D003: Baseline before self-play

**Decision:** First train and evaluate the policy against a random/scripted opponent, then enable self-play.

**Reason:** This makes it faster to distinguish environment or reward defects from instability caused by two simultaneously changing policies.

**Impact:** A small `ScriptedOpponent` is required, but it provides a measurable initial objective.

## 2026-10-07 — D004: Centralized rewards

**Decision:** One `RewardPolicy` defines terminal rewards and bounded radial shaping.

**Reason:** Reward tuning is an experiment and must remain visible and reproducible.

**Impact:** Contact alone does not generate rewards; every coefficient change is recorded with its run.

## 2026-10-07 — D005: Speed before presentation

**Decision:** HUD, art, audio, and complex mechanics are implemented only after the trainable vertical slice.

**Reason:** The project's primary risk is correct episode termination and policy learning, not graphics.

**Impact:** The backlog follows a strict M1 → M4 critical path; new scope moves to Phase 4.

## 2026-10-07 — D006: 3D simulation with a constrained single body

**Decision:** The MVP uses a 3D horizontal XZ arena and one dynamic `Rigidbody` with a `CapsuleCollider` per fighter. Gravity is enabled, yaw is free, and X/Z body rotation is locked.

**Reason:** This provides genuine 3D presentation and collision while preserving a small, stable action space and avoiding the training cost of balance and ragdoll control.

**Impact:** Arena, motor, observations, boundary checks, and tests use 3D APIs. Full ragdoll control remains post-MVP.

## 2026-10-07 — D007: `RELESUMO/` is the canonical Unity project

**Decision:** Continue implementation in the new `RELESUMO/` Universal 3D project. Treat `RELEGame/` as the superseded 2D prototype.

**Reason:** `RELESUMO/` already contains the correct 3D URP renderer and scene template, avoiding an unnecessary conversion of the previous 2D project.

**Impact:** All architecture paths and backlog implementation work target `RELESUMO/`. The legacy project's later removal is recorded in D008.

## 2026-10-07 — D008: Remove the legacy 2D project

**Decision:** Keep only the `RELESUMO/` Universal 3D project in the repository.

**Reason:** The team confirmed that all future development will use the 3D Unity template, so retaining the superseded 2D project would create ambiguity and repository noise.

**Impact:** Documentation, implementation, tests, training configuration, and future automation must resolve Unity paths exclusively from `RELESUMO/`.

## 2026-10-08 — D009: Hybrid movement and combat action contract

**Decision:** Replace forward/turn control with a clamped planar `[move_x, move_z]` vector and three discrete flags: dash, shove, and brace. Dash and shove follow the current or most recent movement direction.

**Reason:** The same compact contract supports eight-direction keyboard movement, full analog controller movement, and later ML control without changing gameplay physics.

**Impact:** `SumoCommand` and `SumoMotor` expose gameplay state for future observations, but the current cycle contains no ML-Agents integration. Brace is directionless, disables offensive actions while active, slows movement, and improves physical resistance.
