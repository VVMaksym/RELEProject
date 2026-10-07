# SESSION PROTOCOL

## START
Agents must not infer roles outside ROLE_ASSIGNMENT.md.

1. Read:
   - .memory/ROLE_ASSIGNMENT.md
   - .memory/SNAPSHOT.md
   - .memory/ARCHITECTURE.md
   - .memory/BACKLOG.md
   - .memory/GENERATION_BRIEF.md (if generation task is active)

2. Identify:
   - Current phase
   - Active task

---

## BOOTSTRAP MODE

### Trigger Condition

Bootstrap Mode activates automatically if ALL conditions are true:

- ARCHITECTURE.md contains placeholder content or is undefined
- BACKLOG.md contains no active atomic task
- SNAPSHOT.md phase is undefined or marked as "Undefined"
- ROLE_ASSIGNMENT.md contains unassigned roles

If any of the above is false, normal START protocol continues.

---

### Bootstrap Execution Rules

When Bootstrap Mode is triggered, the Architect agent must:

1. Infer minimal system structure from the user-provided project description.
2. Assign initial roles in ROLE_ASSIGNMENT.md.
3. Define minimal architecture in ARCHITECTURE.md (5–15 lines, structural only).
4. Create exactly one atomic task in BACKLOG.md under Phase 1.
5. Activate Phase 1 in SNAPSHOT.md.
6. Log initialization decisions in DECISIONS.md.

The Architect must not:
- Create multiple tasks
- Define implementation details
- Bypass layer separation
- Skip decision logging

After Bootstrap execution:

- SNAPSHOT.md must reflect an active phase
- BACKLOG.md must contain exactly one active task
- ARCHITECTURE.md must contain defined layers
- ROLE_ASSIGNMENT.md must contain explicit assignments

Bootstrap Mode executes once per project lifecycle.
Subsequent sessions follow the normal START protocol.

---

3. If the active task is idea generation:
   - Builder must use GENERATION_BRIEF.md as the sole input context.
   - No implicit domain assumptions are allowed.

4. If the active task produces layer artifacts:
   - The filename must include the Batch ID defined in GENERATION_BRIEF.md.
   - Overwriting previous batch files is prohibited.

5. If BACKLOG contains no active tasks:
   - Architect must define next atomic task
   - Add it to BACKLOG
   - Log rationale in DECISIONS.md
   - Update SNAPSHOT

6. Execute only backlog-defined tasks.

---

## FINISH

1. Update SNAPSHOT
2. Update BACKLOG progress
3. Log architectural decisions if any
4. Summarize session outcome