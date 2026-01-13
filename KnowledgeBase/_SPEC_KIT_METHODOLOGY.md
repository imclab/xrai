# Spec-Kit: Spec-Driven Development Methodology

**Source**: https://github.com/github/spec-kit
**Version**: Latest (2026)
**Category**: Development Methodology / AI-Assisted Development

---

## Overview

Spec-Kit is GitHub's open-source toolkit for **Spec-Driven Development** - a methodology where specifications are executable, directly generating working implementations rather than just guiding them.

> "For decades, code has been king — specifications were just scaffolding we built and discarded once the 'real work' of coding began. Spec-Driven Development changes this: specifications become executable."

---

## Core Philosophy

| Traditional Development | Spec-Driven Development |
|------------------------|-------------------------|
| Code is the source of truth | Specs are the source of truth |
| Specs guide, then are discarded | Specs execute and generate |
| "How" comes first | "What" and "Why" come first |
| One-shot code generation | Multi-step refinement |

### Key Principles

1. **Intent-Driven**: Specifications define the "what" before the "how"
2. **Rich Specifications**: Use guardrails and organizational principles
3. **Multi-Step Refinement**: Not one-shot generation
4. **AI-Powered Interpretation**: Leverage advanced AI model capabilities

---

## Workflow

### Standard Flow

```
Constitution → Specify → Clarify → Plan → Tasks → Implement
```

### Slash Commands

| Command | Purpose |
|---------|---------|
| `/speckit.constitution` | Create/update project principles |
| `/speckit.specify` | Define what to build (requirements, user stories) |
| `/speckit.clarify` | Clarify underspecified areas (before plan) |
| `/speckit.plan` | Create technical implementation plan |
| `/speckit.tasks` | Generate actionable task list |
| `/speckit.implement` | Execute all tasks |
| `/speckit.analyze` | Cross-artifact consistency check |
| `/speckit.checklist` | Generate quality checklists |

### Directory Structure

```
project/
├── .specify/
│   ├── memory/
│   │   └── constitution.md      # Project principles
│   ├── scripts/
│   │   ├── common.sh
│   │   ├── create-new-feature.sh
│   │   └── setup-plan.sh
│   ├── specs/
│   │   └── NNN-feature-name/
│   │       ├── spec.md          # Feature specification
│   │       ├── plan.md          # Technical plan
│   │       └── tasks.md         # Task breakdown
│   └── templates/
│       ├── spec-template.md
│       ├── plan-template.md
│       └── tasks-template.md
```

---

## Installation

```bash
# Persistent installation (recommended)
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git

# Initialize in new project
specify init <PROJECT_NAME> --ai claude

# Initialize in existing project
specify init . --ai claude
# or
specify init --here --ai claude
```

### Supported AI Agents

- Claude Code ✅
- Cursor ✅
- Windsurf ✅
- GitHub Copilot ✅
- Gemini CLI ✅
- Amazon Q ⚠️ (limited slash command support)
- Many more...

---

## Templates

### Constitution Template

```markdown
# Project Constitution

## Core Principles
1. [Principle with rationale]
2. [Principle with rationale]

## Quality Standards
- Code coverage: [target]%
- Performance: [metrics]
- Accessibility: [standards]

## Technical Decisions
- Framework: [choice] - [why]
- Architecture: [pattern] - [why]

## Development Practices
- [Practice 1]
- [Practice 2]
```

### Spec Template

```markdown
# [Feature Name] Specification

## Overview
[One paragraph: what is this feature?]

## User Stories
- As a [user], I want [goal] so that [benefit]

## Functional Requirements
### FR1: [Name]
[Description]
**Acceptance Criteria:**
- [ ] Criterion 1
- [ ] Criterion 2

## Non-Functional Requirements
- Performance: [requirements]
- Security: [requirements]

## Out of Scope
- [What we're NOT building]
```

### Plan Template

```markdown
# [Feature] Implementation Plan

## Tech Stack
- [Technology choices with rationale]

## Architecture
[How components interact]

## Files to Create/Modify
| File | Purpose | Changes |
|------|---------|---------|
| path/to/file | [purpose] | [what changes] |

## Implementation Sequence
1. [Step 1]
2. [Step 2]

## Dependencies
- [External dependencies]

## Risks & Mitigations
| Risk | Mitigation |
|------|------------|
| [risk] | [mitigation] |
```

---

## Development Phases

| Phase | Focus | Use Case |
|-------|-------|----------|
| **0-to-1 (Greenfield)** | Generate from scratch | New features, new projects |
| **Creative Exploration** | Parallel implementations | Multiple approaches, experiments |
| **Iterative Enhancement** | Brownfield modernization | Add features, refactor |

---

## Best Practices

### When Writing Specs

1. **Focus on WHAT and WHY** - Not the tech stack (that comes in /plan)
2. **Be explicit** - Vague specs produce vague code
3. **Include edge cases** - What happens when things go wrong?
4. **Define success criteria** - How do we know it's done?

### When to Use Spec-Kit

- Features > 50 lines of code
- Multi-file changes
- New architectural patterns
- User-facing features
- Anything with multiple valid approaches

### When to Skip

- Bug fixes with clear cause
- Single-file changes < 50 lines
- Config/environment changes
- Urgent production fixes (spec after)

---

## Environment Variables

| Variable | Purpose |
|----------|---------|
| `SPECIFY_FEATURE` | Override feature detection for non-Git repos |
| `GH_TOKEN` | GitHub token for API requests |

---

## Integration with Projects

### Existing Project Integration

1. Create `.specify/` directory structure
2. Write `constitution.md` with project principles
3. Create templates based on project needs
4. Migrate existing specs to numbered format
5. Use slash commands for new features

### Claude Code Integration

Claude Code natively supports spec-kit slash commands when:
1. `.specify/` directory exists
2. Constitution and templates are in place
3. Claude is run from project root

---

## Related Resources

- **GitHub Repo**: https://github.com/github/spec-kit
- **Video Overview**: https://www.youtube.com/watch?v=a9eR1xsfvHg
- **Full Methodology**: https://github.com/github/spec-kit/blob/main/spec-driven.md

---

## Changelog

- 2026-01-13: Initial knowledgebase entry created
