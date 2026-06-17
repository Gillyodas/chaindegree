---
name: behavior-rules
description: Defines core behavioral rules for tool prioritization, skill usage, and activity logging.
---

# Behavior Rules Skill

This skill ensures Antigravity follows specific operational constraints requested by the user.

## Instructions

1. **Tool Hierarchy**:
   - Always attempt to use built-in tools (`view_file`, `list_dir`, `grep_search`, etc.) before resorting to `run_command`.
   - Use `run_command` only when a specific shell command is required (e.g., `dotnet`, `npm`, `git`).

2. **Skill Maximization**:
   - Proactively search for and read relevant `SKILL.md` files in `.agent/skills/` before performing code changes.
   - Align all code modifications with the patterns defined in those skills.

3. **Logging**:
   - Maintain a mental log of all tools and skills used during the turn.
   - Summarize these in the final response.

4. **System Brain Maintenance**:
   - The file `SYSTEM_BRAIN.md` in the root directory acts as the central map and documentation of the system's architecture, classes, and methods.
   - **MANDATORY**: After any task that involves adding, modifying, or deleting significant files, classes, or public methods, you MUST update `SYSTEM_BRAIN.md` to reflect these changes before concluding the task. This ensures the system brain is always up-to-date and prevents duplicated efforts.

5. **Version Control (Git)**:
   - **MANDATORY**: You MUST commit your changes to git after completing EVERY individual task in your `task.md` list.
   - Use conventional commits format (e.g., `feat: ...`, `fix: ...`, `docs: ...`, `chore: ...`).
   - This ensures that changes can be cleanly reverted if any issues occur.

## Usage
- Read this skill when starting a new task to refresh the behavioral constraints.
