# AI Agent Framework (VAF)

## Purpose
Template-based dual-agent orchestration framework.

## Contains
- Memory contract
- Role assignment model
- Session lifecycle protocol

This framework contains structure only.  
It must not contain project-specific state.

---

## Quick Start (Step-by-step)

---

## Initialization Modes

VAF supports two initialization approaches:

- **Strict Mode** — maximum control, manual setup
- **Bootstrap Mode** — fast start from a plain-text project description

Choose based on your tolerance for structure vs speed.

---

# Strict Mode (Manual, Deterministic)

Recommended when rigor and predictability matter.

### Step 1 — Assign roles

Edit:

    .memory/ROLE_ASSIGNMENT.md

Example:

    Architect: Claude
    Builder: OpenAI
    QA: Claude

Roles must be explicitly defined before any session starts.

---

### Step 2 — Define minimal architecture

Edit:

    .memory/ARCHITECTURE.md

Minimal example:

    System: SaaS Pricing Optimizer

    Core Layers:
    - Data ingestion
    - Strategy generation
    - Evaluation

    Boundaries:
    - Agents communicate only via .memory
    - Builder does not modify ARCHITECTURE

Architecture should be concise (5–15 lines).
It defines structure, not implementation details.

---

### Step 3 — Create one atomic task

Edit:

    .memory/BACKLOG.md

Example:

    ## Phase 1
    - [ ] Define pricing model inputs and constraints

Rules:
- Only one active atomic task
- Task must be testable
- No vague goals

---

### Step 4 — Start the first session

Run:

    Execute START protocol from SESSION_PROTOCOL.md

Agents now operate strictly within defined roles and structure.

Strict Mode ensures:
- Maximum control
- Clear boundaries
- Minimal drift
- High auditability

---

# Bootstrap Mode (Automatic Initialization)

Recommended when you want speed and exploratory setup.

### Step 1 — Provide project description

Run:

    Bootstrap project using the following description:

    <insert plain-text description here>

Example:

    Build a system that generates and evaluates AI-driven business ideas 
    for the EU market with low capital entry requirements.

---

### Step 2 — Agent performs initialization

The Architect agent must:

1. Assign initial roles (if undefined)
2. Populate ARCHITECTURE.md
3. Create initial BACKLOG with one atomic task
4. Update SNAPSHOT.md
5. Log decisions in DECISIONS.md

The system must still respect:
- Layer separation
- Role assignment contract
- No direct agent-to-agent communication

---

### Bootstrap Mode Trade-offs

Advantages:
- Faster start
- Lower friction
- Useful for exploratory work

Risks:
- Higher architectural drift
- Less predictable role allocation
- Requires later refinement

Bootstrap Mode should be followed by:
- A structural review pass
- Possible migration to Strict Mode

---

## Choosing a Mode

Use Strict Mode when:
- Building production-grade pipelines
- Running repeatable experiments
- Needing auditability

Use Bootstrap Mode when:
- Exploring new concepts
- Rapidly testing hypotheses
- Starting from ambiguity

VAF supports both — discipline is optional, but traceability is not.

## Mental Model

VAF is not a project template.  
It is an orchestration contract between agents.

Think of it as:

- A protocol, not a product
- A process skeleton, not a solution
- A coordination layer, not business logic

VAF separates:

- Structure (framework)
- State (project)
- Roles (assignment file)
- Artifacts (layer outputs)

Agents never communicate directly.  
They communicate only through structured files inside `.memory/`.

If something is not written in `.memory/`, it does not exist.

The framework enforces:
- Explicit roles
- Atomic tasks
- Layer boundaries
- Immutable artifacts
- Batch traceability

---

## When NOT to Use This Framework

Do not use VAF if:

- You are working solo with no role separation needed.
- The task is a one-off prompt.
- There is no need for state persistence across sessions.
- There is no need for auditability.
- The problem does not benefit from layered reasoning.

VAF adds structure and discipline.  
If structure slows you down more than it protects you, do not use it.

This framework is optimized for:
- Iterative agent collaboration
- Repeatable pipelines
- Traceable decision-making
- Controlled experimentation
- Multi-session development

It is intentionally rigid.

Use it when rigor matters more than speed.