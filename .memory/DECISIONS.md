# DECISIONS

## 2026-10-07 — D001: Top-down 2D with one physics body

**Decision:** The MVP uses one dynamic `Rigidbody2D` per sumo fighter, without ragdolls or joints.

**Reason:** This minimizes the action space and the time required to obtain the first learning policy.

**Impact:** The stickman initially serves as a visual representation of one solid body; limb control is deferred.

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
