# SNAPSHOT

## Role Assignment

Reference: `.memory/ROLE_ASSIGNMENT.md`

## Current Phase

Phase 1 — Vertical Slice.

## Active Focus

`B001` — prepare the `Assets/_Project` structure, add ML-Agents, and create the initial training scene.

## Current State

- Unity `6000.4.0f1`, URP 2D template.
- Only the initial `SampleScene` exists; there is no gameplay code or ML-Agents dependency yet.
- The MVP architecture and critical backlog have been defined.

## Constraints

- Top-down 2D, two agents, one circular arena.
- One `Rigidbody2D` per fighter; ragdoll is outside the MVP.
- Build a playable/trainable vertical slice before graphics and extensions.
- Game physics must not depend on the ML-Agents API.

## Last Update

2026-10-07 — architecture/bootstrap planning completed.
