# BACKLOG — Sumo RL

The backlog follows the shortest critical path. Tasks may run in parallel when they belong to different team members and do not edit the same Unity assets; dependent tasks still wait for their prerequisites.

Status labels: `▶ ACTIVE` — in progress; `READY` — available after its dependencies; `LATER` — outside the MVP.

## Fast Track

| Milestone | Outcome | Target for two students |
|---|---|---|
| M1 — Playable | Two physics-driven fighters finish and reset a round | Day 1 |
| M2 — Trainable | ML-Agents connects to the environment and completes a smoke training run | Day 2 |
| M3 — Learning | A policy defeats the baseline and self-play is running | Day 3 |
| M4 — Presentable | Metrics, a saved model, instructions, and a demonstration are ready | Day 4 |

These targets assume a working Python/Unity environment and no package compatibility blockers. They describe the intended execution order, not guaranteed deadlines.

## Phase 1 — Vertical Slice

- [ ] **B001 — ▶ ACTIVE — Prepare the existing `RELESUMO` 3D gameplay project.** Create `Assets/_Project` according to the architecture, verify the existing PC/Mobile Universal Renderer assignments, and prepare the prototype scene. **Done when:** the scene renders the arena and two lit 3D test fighters through the active URP renderer without compile errors. **Dependencies:** none. **ML-Agents setup is deferred.**
- [ ] **B002 — READY — Build the 3D arena and boundary API.** Create a raised circular arena on the XZ plane with a configurable radius, floor collider, Y fall threshold, and `IsOutside(position)` check. **Done when:** EditMode tests cover points inside, on the edge, outside the radius, and below the arena. **Dependencies:** B001.
- [ ] **B003 — ACTIVE — Implement the 3D `SumoMotor` and human control.** Use one dynamic `Rigidbody` and `CapsuleCollider` with two-axis planar movement, retained aim direction, dash, shove, brace, stamina/cooldowns, and locked X/Z rotation for MVP stability. **Done when:** keyboard control provides eight movement directions, dash/shove follow aim direction, brace resists both actions, state resets cleanly, and all tunable values remain within configured limits. **Dependencies:** project scripts compile.
- [ ] **B004 — READY — Implement the round lifecycle.** `MatchCoordinator` positions two fighters on the XZ plane, detects radius/fall win/loss/draw conditions, and resets without reloading the scene. **Done when:** 100 scripted resets complete without errors, retained velocity, or incorrect orientation. **Dependencies:** B002, B003.
- [ ] **B005 — READY — Connect `SumoAgent` later.** Map continuous `[move_x, move_z]` and discrete `[dash, shove, brace]` actions to `SumoCommand`, then collect the agreed normalized observation vector. **Done when:** Behavior Parameters matches the action/observation contract and both agents use the same motor API as human control. **Dependencies:** B003, B004; explicitly deferred during the current gameplay cycle.
- [ ] **B006 — READY — Add `RewardPolicy`.** Implement terminal rewards, a time penalty, and bounded radial shaping with centralized parameters. **Done when:** PlayMode tests verify reward totals for win/loss/draw and one shaping step. **Dependencies:** B004, B005.
- [ ] **B007 — READY — Assemble the trainable 3D prefab and scene.** Use two instances of one capsule-based agent prefab, separate team IDs, the same behavior name, and correct 3D collision layers. Add an elevated fixed camera and basic lighting for observation by humans only. **Done when:** the scene autonomously runs 1,000 episodes in accelerated mode without exceptions, NaN values, or fighters tunneling through the arena. **Dependencies:** B005, B006.

### B003 progress — movement, combat, and physics (2026-10-08)

Implementation is complete; Play Mode balancing and reset verification remain before B003 can be closed.

- [x] Added a controller-independent `SumoCommand` contract: continuous two-axis movement plus discrete dash, shove, and brace actions.
- [x] Added configurable keyboard controls for two local fighters and a gamepad adapter with radial stick deadzone.
- [x] Implemented retained aim direction: dash and shove use the current or most recent movement direction.
- [x] Implemented semi-physical planar movement with acceleration, braking, inertia, speed resistance, and reduced air control.
- [x] Kept fighters upright by locking Rigidbody X/Z rotation while allowing controlled Y-axis facing.
- [x] Changed dash into a physics velocity impulse with duration, cooldown, stamina cost, and one impact per opponent per dash.
- [x] Implemented shove as a forward overlap check followed by a directional physics impulse and a small lift impulse.
- [x] Implemented brace: reduced movement, increased Rigidbody mass, reduced incoming explicit impulses, and shorter post-hit control suppression.
- [x] Added stamina regeneration, cooldown/readiness values, grounded/contact state, planar velocity, and aim state for later observations and UI.
- [x] Added clean motor-state reset support and dash/shove hit events for later match and reward systems.
- [x] Fixed the gamepad button namespace/import issue that had caused Unity scripts and character control components to appear missing.
- [x] Added camera-relative human movement for keyboard and gamepad: left/right follow the screen and forward moves away from the camera; removed Fighter 2 inversion.
- [x] Verified the gameplay assembly builds successfully: `0 errors`, `0 warnings`.
- [ ] In Unity Play Mode, verify movement, collision response, dash, shove, brace, airborne control, and repeated resets for both fighters.
- [ ] Tune both fighters to an initial `Dash Velocity Change` of approximately `7`; the scene may retain the old serialized value `11` after field migration.
- [ ] Tune Rigidbody `Linear Damping` to approximately `0.5–1.0` if the current value `2` removes too much momentum.
- [ ] Close B003 after the Play Mode checks pass and all gameplay values feel symmetric for both fighters.

## Phase 2 — First Training

- [ ] **B008 — READY — Add a random/scripted baseline.** The opponent moves toward its target with limited noise and uses the same `SumoMotor`. **Done when:** the baseline completes matches and provides a stable comparison target. **Dependencies:** B007.
- [ ] **B009 — READY — Create a minimal PPO configuration and smoke run.** A short run validates the Unity-to-Python connection, checkpoints, and TensorBoard metrics. **Done when:** the trainer completes the configured number of steps and exports an `.onnx` model. **Dependencies:** B007.
- [ ] **B010 — READY — Train locomotion and ring-out behavior against the baseline.** Tune only critical physics and reward parameters. **Done when:** evaluation with fixed seeds shows at least a 70% win rate across 100 matches against the random baseline. **Dependencies:** B008, B009.
- [ ] **B011 — READY — Enable self-play.** Configure a snapshot pool and symmetric team IDs without changing gameplay code. **Done when:** training creates snapshots, policy-versus-older-policy evaluation matches finish, and win rate shows no technical side bias. **Dependencies:** B010.

## Phase 3 — Evaluation and Submission

- [ ] **B012 — READY — Automate evaluation.** Use fixed seeds and 100+ matches to record win/loss/draw, mean duration, and ring-out rate. **Done when:** one run produces CSV/JSON output suitable for plotting. **Dependencies:** B010.
- [ ] **B013 — READY — Add a minimal HUD and launch modes.** Support training without UI, AI-vs-AI demonstration, and Human-vs-AI. **Done when:** configuration selects the mode and the HUD displays the timer, score, and winner. **Dependencies:** B007.
- [ ] **B014 — READY — Record experiments.** Store the trainer configuration, seed, commit, selected `.onnx` model, and baseline/self-play plots. **Done when:** each model can be unambiguously matched to its configuration and results. **Dependencies:** B011, B012.
- [ ] **B015 — READY — Write reproduction instructions and a demo script.** Document setup, training, evaluation, inference, and known limitations. **Done when:** another person can launch the demo from the README without verbal guidance. **Dependencies:** B013, B014.

## Phase 4 — LATER, Only After the MVP

- [ ] Full 3D ragdoll/joints, learned balance, and individual limb control.
- [ ] Rigged 3D characters, animations, VFX, and sound.
- [ ] A curriculum with varied masses, forces, arena sizes, and domain randomization.
- [ ] A tournament between multiple policies/algorithms and a reward-shaping ablation study.

## Scope Change Rule

If a task is not required for M1–M4, it moves to Phase 4. No new mechanic is added until B010 demonstrates that the basic environment can learn.
