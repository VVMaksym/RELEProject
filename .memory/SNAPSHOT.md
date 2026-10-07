# SNAPSHOT

## Role Assignment

Reference: `.memory/ROLE_ASSIGNMENT.md`

## Current Phase

Phase 1 — Vertical Slice.

## Active Focus

`B001` — prepare the `RELESUMO/Assets/_Project` structure, verify the existing 3D URP renderer, add ML-Agents, and create the initial 3D training scene.

## Current State

- `RELESUMO/` is the canonical Unity 6 Universal 3D project and already contains PC/Mobile URP renderer assets.
- Only the initial `RELESUMO/Assets/Scenes/SampleScene.unity` exists; there is no gameplay code or ML-Agents dependency yet.
- `RELEGame/` is the previous 2D project and is no longer the implementation target.
- The MVP architecture and critical backlog have been updated for 3D.

## Constraints

- 3D simulation on the horizontal XZ plane, two agents, and one circular arena.
- One `Rigidbody` and `CapsuleCollider` per fighter; X/Z rotation is locked and full ragdoll is outside the MVP.
- A fixed elevated camera is presentation-only and does not affect agent observations.
- Build a playable/trainable vertical slice before graphics and extensions.
- Game physics must not depend on the ML-Agents API.

## Last Update

2026-10-07 — architecture/bootstrap planning completed.
