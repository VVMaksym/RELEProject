# BACKLOG — Sumo RL

The backlog follows the shortest critical path. Only one task is active at a time; the next task starts only after the current task satisfies its acceptance criteria.

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

- [ ] **B001 — ▶ ACTIVE — Prepare the project skeleton and ML-Agents dependency.** Create `Assets/_Project` according to the architecture, add a compatible ML-Agents package, and add an empty `SumoTraining` scene to Build Settings. **Done when:** Unity opens without compile errors and the `Agent` type is available to a script. **Dependencies:** none.
- [ ] **B002 — READY — Build the arena and boundary API.** Create a visible circular arena with a configurable radius and an `IsOutside(position)` check. **Done when:** EditMode tests cover points inside, on the edge, and outside. **Dependencies:** B001.
- [ ] **B003 — READY — Implement `SumoMotor` and heuristic control.** Use one dynamic `Rigidbody2D` with forward/backward movement, rotation, and a shove with cooldown. **Done when:** a human can control the fighter and acceleration/cooldown remain within configured limits. **Dependencies:** B001.
- [ ] **B004 — READY — Implement the round lifecycle.** `MatchCoordinator` positions two fighters, detects win/loss/draw, and resets without reloading the scene. **Done when:** 100 scripted resets complete without errors or retained velocity. **Dependencies:** B002, B003.
- [ ] **B005 — READY — Connect `SumoAgent`.** Implement a fixed observation vector, three actions, and heuristic mapping through `SumoCommand`. **Done when:** Behavior Parameters shows the expected observation/action sizes and both agents move through the same motor API. **Dependencies:** B003, B004.
- [ ] **B006 — READY — Add `RewardPolicy`.** Implement terminal rewards, a time penalty, and bounded radial shaping with centralized parameters. **Done when:** PlayMode tests verify reward totals for win/loss/draw and one shaping step. **Dependencies:** B004, B005.
- [ ] **B007 — READY — Assemble the trainable prefab and scene.** Use two instances of one agent prefab, separate team IDs, the same behavior name, and correct collision layers. **Done when:** the scene autonomously runs 1,000 episodes in accelerated mode without exceptions or NaN values. **Dependencies:** B005, B006.

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

- [ ] Ragdoll/joints and individual limb control.
- [ ] Visual stickmen, animations, VFX, and sound.
- [ ] A curriculum with varied masses, forces, arena sizes, and domain randomization.
- [ ] A tournament between multiple policies/algorithms and a reward-shaping ablation study.

## Scope Change Rule

If a task is not required for M1–M4, it moves to Phase 4. No new mechanic is added until B010 demonstrates that the basic environment can learn.
