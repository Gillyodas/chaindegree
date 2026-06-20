# Antigravity Behavior Rules

This document defines the operational rules for the Antigravity AI assistant within this workspace.

## 1. Tool Prioritization
- **Rule**: Prioritize using Antigravity's specialized tools over the terminal (`run_command`) whenever possible.
- **Preferred Tools**:
    - `list_dir` for exploring directories.
    - `view_file` for reading code.
    - `grep_search` for searching text.
    - `replace_file_content` / `multi_replace_file_content` for editing.
- **Terminal Usage**: Only use `run_command` for tasks that cannot be performed by other tools (e.g., running builds, tests, or complex CLI commands like `dotnet ef`).

## 2. Skill Utilization
- **Rule**: Maximize the application of configured skills located in `.agent/skills/`.
- **Process**:
    - Before starting a task, check if any skills match the requirements.
    - Read the `SKILL.md` of relevant skills.
    - Apply the patterns and best practices defined in those skills strictly.

## 3. Usage Logging
- **Rule**: At the end of every task or major interaction, log the tools and skills that were utilized.
- **Format**: Include a "Tools & Skills Used" section in the final response.

---
*Created on 2026-05-08*
