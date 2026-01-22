# Anti-Patterns

**Purpose**: Document what NOT to do. Learn from failures to prevent recurrence.
**Pattern**: Each anti-pattern → why it fails → better approach

---

## Template

```markdown
## [Date] - [Anti-Pattern Name]

**Description**: What to avoid
**Why It Fails**: Root cause
**Better Approach**: What to do instead
**Frequency**: How often this trap appears
**Caught By**: How we detect this happening
```

---

## Tool Usage Anti-Patterns

### Reading Before Searching

**Description**: Reading full files before knowing what to look for
**Why It Fails**: Wastes tokens, may read wrong file entirely
**Better Approach**: Grep/Glob first → identify target → Read specific file/lines
**Frequency**: Common (especially start of sessions)
**Caught By**: Token usage spike without progress

### Grep for File Names

**Description**: Using Grep to find files by name pattern
**Why It Fails**: Grep searches content, not filenames; slower and less accurate
**Better Approach**: Use Glob with pattern like `**/*.cs` or `**/FileName*`
**Frequency**: Occasional
**Caught By**: Grep returns no results for known-existing files

### Write Instead of Edit

**Description**: Using Write to change small parts of files
**Why It Fails**: Higher token cost (full file content), risk of losing other changes
**Better Approach**: Use Edit for modifications, Write only for new files
**Frequency**: Occasional
**Caught By**: Write tool used on existing files

---

## Unity Development Anti-Patterns

### String VFX Properties

**Description**: Using string literals for VFX Graph property names
**Why It Fails**: No compile-time validation, runtime lookup overhead, silent failures
**Better Approach**: Use `ExposedProperty` static fields
**Frequency**: Common in legacy code
**Caught By**: VFX properties not updating at runtime

### Skipping AR Null Checks

**Description**: Assuming AR textures/data always available
**Why It Fails**: AR subsystems initialize asynchronously, textures may be null
**Better Approach**: Always use `TryGetTexture` pattern, check subsystem state
**Frequency**: Common (spec 005 addressed this)
**Caught By**: Null reference crashes on device

### Hardcoded Thread Groups

**Description**: Using fixed thread group sizes in compute shaders
**Why It Fails**: Different GPUs have different optimal sizes, may truncate data
**Better Approach**: Query `GetKernelThreadGroupSizes()` and use `CeilToInt`
**Frequency**: Occasional
**Caught By**: Compute results incomplete or corrupted

---

## Agent Anti-Patterns

### Agent Without Constraints

**Description**: Giving agents access to all tools without restrictions
**Why It Fails**: Higher token cost, slower execution, may use wrong tools
**Better Approach**: Specify exact tools needed in agent definition
**Frequency**: Early agent definitions
**Caught By**: Agent uses inefficient tool chains

### Ignoring Shared Rules

**Description**: Agent definitions that don't reference shared rules
**Why It Fails**: Inconsistent behavior, duplicated instructions, drift over time
**Better Approach**: All agents reference `_AGENT_SHARED_RULES.md`
**Frequency**: Fixed (all agents now reference shared rules)
**Caught By**: Agent behaves differently than expected

---

## Architecture Anti-Patterns

### Reinventing KB Solutions

**Description**: Implementing something without checking if it exists in KB
**Why It Fails**: Wasted time, may implement inferior solution
**Better Approach**: Search KB first: `Grep pattern in KnowledgeBase/`
**Frequency**: Occasional
**Caught By**: Finding existing solution after implementation

### Ignoring Spec-Kit

**Description**: Starting implementation without spec or plan
**Why It Fails**: Scope creep, unclear success criteria, harder to validate
**Better Approach**: Create spec first, validate approach, then implement
**Frequency**: Occasional
**Caught By**: Implementation doesn't match requirements

---

## Token Anti-Patterns

### Full File Reads

**Description**: Reading entire large files when only part needed
**Why It Fails**: Token waste, context pollution, may hit limits
**Better Approach**: Use offset/limit, or Grep to find relevant lines first
**Frequency**: Common
**Caught By**: Token count spikes, context warnings

### Verbose Agent Output

**Description**: Agents that produce long explanations instead of results
**Why It Fails**: Token waste, user has to parse unnecessary text
**Better Approach**: Bullets, tables, concise results only
**Frequency**: Occasional
**Caught By**: Long agent responses with little actionable content

---

## Self-Healing Triggers

When anti-pattern is detected:
1. Log to FAILURE_LOG.md
2. Add prevention to this file
3. Update relevant rules/agents
4. Add detection to system-improver agent

---

**Last Updated**: 2026-01-21
**Anti-Patterns Documented**: 10
