# Claude AI & Claude Code Documentation Reference

**Last Updated**: 2025-11-02
**Purpose**: Comprehensive reference for Claude AI best practices, Claude Code usage, and prompt engineering

---

## Overview

This document consolidates official Anthropic documentation for working with Claude AI and Claude Code effectively.

---

## 1. Claude Code - Official Documentation

### Core Documentation

**Main Hub**:
- https://docs.claude.com/en/docs/claude-code/overview - Claude Code overview
- https://github.com/anthropics/claude-code - Official GitHub repository

**Getting Started**:
- https://docs.claude.com/en/docs/claude-code/common-workflows - Common development workflows

---

### Agent System

**Agent Types & Configuration**:
- https://docs.claude.com/en/docs/claude-code/sub-agents - Subagent configuration and capabilities
- https://docs.claude.com/en/docs/claude-code/plugins-reference#agents - Agent plugin reference
- https://docs.claude.com/en/docs/claude-code/cli-reference#agents-flag-format - CLI agent flag formats

**Available Agents** (from global CLAUDE.md):
- **Explore** (Haiku) - Codebase exploration, file finding
- **research-agent** (Sonnet) - Unity XR/AR research, GitHub investigation
- **general-purpose** (Sonnet) - Multi-step tasks, complex implementation
- **code-tester** (Sonnet) - Automated testing after changes
- **tech-lead** (Sonnet) - Architecture reviews, technical decisions
- **orchestrator-agent** (Sonnet) - Multi-task coordination
- **monitor-agent** (Sonnet) - Project health monitoring
- **logger-agent** (Sonnet) - Adding structured logging
- **Plan** (Haiku) - Task planning and breakdown

---

### Skills & Extensions

**Extending Claude Code**:
- https://docs.claude.com/en/docs/claude-code/skills - Agent skills development

**Example**: Create custom skills for Unity-specific workflows (shader validation, performance profiling, etc.)

---

### Hooks & Automation

**Event Handling**:
- https://docs.claude.com/en/docs/claude-code/hooks-guide - Hooks guide
- https://docs.claude.com/en/docs/claude-code/hooks - Hook reference

**Use Cases**:
- Pre-commit validation (run tests before git commit)
- Post-file-write actions (auto-format, lint, compile check)
- Session initialization (load project-specific context)

---

### MCP (Model Context Protocol)

**External Tool Integration**:
- https://docs.claude.com/en/docs/claude-code/mcp - MCP overview

**Current MCP Servers** (from session):
- **filesystem** - File operations
- **memory** - Knowledge graph retention
- **fetch** - Web content fetching
- **github** - GitHub API integration
- **unity-mcp** - Unity Editor control (port 6400)

---

### Configuration & Settings

**Project Setup**:
- https://docs.claude.com/en/docs/claude-code/settings - Available settings
- https://docs.claude.com/en/docs/claude-code/settings#available-settings - Detailed settings reference
- https://docs.claude.com/en/docs/claude-code/settings#tools-available-to-claude - Tool permissions

**Model Configuration**:
- https://docs.claude.com/en/docs/claude-code/model-config - Model selection (Sonnet, Haiku, Opus)

**Memory System**:
- https://docs.claude.com/en/docs/claude-code/memory - Session memory and context retention

**Status Line**:
- https://docs.claude.com/en/docs/claude-code/statusline - Status line customization

---

### CLI & Terminal

**Command Line Interface**:
- https://docs.claude.com/en/docs/claude-code/cli-reference - CLI commands reference
- https://docs.claude.com/en/docs/claude-code/terminal-config - Terminal configuration

**Slash Commands**:
- Create custom slash commands for project-specific workflows
- Example: `/unity-test` to run Unity MCP test sequence

---

### Advanced Features

**Headless Mode**:
- https://docs.claude.com/en/docs/claude-code/headless - Run Claude Code without UI (CI/CD integration)

**Checkpointing**:
- https://docs.claude.com/en/docs/claude-code/checkpointing - Save/restore session state

---

## 2. Prompt Engineering Best Practices

### Core Principles

**Interactive Tutorial**:
- https://github.com/anthropics/prompt-eng-interactive-tutorial - Hands-on prompt engineering course

**Foundational Techniques**:
- https://docs.claude.com/en/docs/build-with-claude/prompt-engineering/be-clear-and-direct - Clarity and directness
- https://docs.claude.com/en/docs/build-with-claude/prompt-engineering/use-xml-tags - Structured prompts with XML

**Example - Clear & Direct**:
```xml
<!-- ❌ BAD -->
"Hey Claude, I was wondering if maybe you could help me out with something..."

<!-- ✅ GOOD -->
"Analyze ParticlePainter.cs for memory leaks. Report line numbers and fixes."
```

**Example - XML Tags**:
```xml
<task>Fix particle brush memory leak</task>
<file>ParticlePainter.cs</file>
<issue>Instantiate/Destroy on every stroke</issue>
<solution>Implement object pooling</solution>
```

---

### Advanced Prompting Patterns

**Chain of Thought**:
- https://docs.claude.com/en/docs/build-with-claude/prompt-engineering/chain-of-thought - Step-by-step reasoning
- https://docs.claude.com/en/docs/build-with-claude/prompt-engineering/chain-prompts - Multi-stage prompts

**Example - Chain of Thought**:
```xml
<reasoning>
1. ParticlePainter creates new GameObject every stroke
2. No cleanup = unbounded memory growth
3. GC spikes every 100+ strokes
→ Need object pooling pattern
</reasoning>
```

**Multishot Prompting**:
- https://docs.claude.com/en/docs/build-with-claude/prompt-engineering/multishot-prompting - Few-shot examples

**Example - Multishot**:
```xml
<examples>
<example>
<input>Fix memory leak in BrushManager</input>
<output>Replaced Instantiate() with object pool (lines 45-67)</output>
</example>
<example>
<input>Optimize particle rendering</input>
<output>Enabled GPU instancing, reduced draw calls 85 → 12</output>
</example>
</examples>

<task>Fix memory leak in ParticlePainter</task>
```

---

### Extended Thinking

**When to Use**:
- https://docs.claude.com/en/docs/build-with-claude/extended-thinking - Complex reasoning tasks
- https://docs.claude.com/en/docs/build-with-claude/prompt-engineering/extended-thinking-tips - Optimization tips

**Use Cases**:
- Architectural decisions (should I use WebGL or native?)
- Performance optimization (identify bottlenecks)
- Complex debugging (multi-file issues)

---

### Long Context Optimization

**Handling Large Codebases**:
- https://docs.claude.com/en/docs/build-with-claude/prompt-engineering/long-context-tips - 200K token window optimization

**Best Practices** (from session analysis):
1. **Section-based reading** (not full files)
   ```bash
   Read(file, offset=680, limit=48)  # Unity MCP section only
   ```

2. **Cache frequently accessed content**
   ```bash
   .claude/cache/CLAUDE_sections.json
   ```

3. **Use .claudeignore** (70-80% token reduction)
   ```
   Library/
   Temp/
   Logs/
   ```

---

## 3. Claude API Features

### Performance & Optimization

**Prompt Caching**:
- https://docs.claude.com/en/docs/build-with-claude/prompt-caching - Reduce costs on repeated context

**Token Counting**:
- https://docs.claude.com/en/docs/build-with-claude/token-counting - Usage tracking

**Batch Processing**:
- https://docs.claude.com/en/docs/build-with-claude/batch-processing - Process multiple requests efficiently

---

### Input Types

**Vision**:
- https://docs.claude.com/en/docs/build-with-claude/vision - Image analysis (screenshots, diagrams, UI mockups)

**Files**:
- https://docs.claude.com/en/docs/build-with-claude/files - Document processing (PDF, DOCX, code files)

**Search Results**:
- https://docs.claude.com/en/docs/build-with-claude/search-results - Web search integration

---

### Context Management

**Context Editing**:
- https://docs.claude.com/en/docs/build-with-claude/context-editing - Efficient context updates

**Embeddings**:
- https://docs.claude.com/en/docs/build-with-claude/embeddings - Semantic search and retrieval

---

## 4. Testing & Evaluation

### Building Evals

**Notebooks & Tutorials**:
- https://github.com/anthropics/claude-cookbooks/blob/main/misc/building_evals.ipynb - Building evaluation systems
- https://colab.research.google.com/drive/1SoAajN8CBYTl79VyTwxtxncfCWlHlyy9 - Interactive eval tutorial

**Best Practices**:
- https://docs.claude.com/en/docs/test-and-evaluate/define-success - Define success criteria
- https://docs.claude.com/en/docs/test-and-evaluate/develop-tests - Develop test suites

---

## 5. Prompt Library & Resources

### Official Prompt Templates

**Prompt Library**:
- https://docs.claude.com/en/resources/prompt-library/library - Pre-built prompt templates

**Code Consultant Pattern**:
- https://docs.claude.com/en/resources/prompt-library/code-consultant - Technical advisory role

**Example Usage** (Code Consultant):
```xml
<role>Senior Unity XR Developer</role>
<expertise>AR Foundation, VFX Graph, Performance Optimization</expertise>
<task>Review ParticlePainter.cs and recommend fixes</task>
```

---

### Prompt Generator

**Auto-Generate Prompts**:
- https://docs.claude.com/en/docs/build-with-claude/prompt-engineering/prompt-generator - Prompt creation tool

---

## 6. Claude Cookbooks

**Complete Repository**:
- https://github.com/anthropics/claude-cookbooks/tree/main - Tutorials, examples, code snippets

**Topics Covered**:
- Prompt engineering patterns
- Building evals
- Function calling
- Tool use
- Multimodal inputs
- Production deployment

---

## 7. Claude for Sheets

**Integration**:
- https://docs.claude.com/en/docs/agents-and-tools/claude-for-sheets - Google Sheets integration

**Use Cases**:
- Batch data processing
- Content generation at scale
- Analysis and classification

---

## 8. External Resources

**Google Sheets Example**:
- https://docs.google.com/spreadsheets/d/15NqTbuypVn_lodnle1TmEcQoUwCkRVO_G3QC_gtSw2Y/edit?gid=150872633#gid=150872633 - Prompt engineering patterns (requires access)

---

## 9. Quick Reference - Best Practices Summary

### From Session Performance Analysis

**Token Optimization**:
- ✅ Use `.claudeignore` (70-80% reduction)
- ✅ Section-based file reading (50-70% reduction)
- ✅ Cache frequent reads (30-40% reduction)
- ✅ Use agents for multi-step tasks (avoid manual sequential work)

**Agent Usage**:
- ✅ research-agent BEFORE implementing Unity XR features
- ✅ Explore agent for "find/analyze" questions
- ✅ general-purpose for multi-step implementation
- ✅ code-tester after changes (30 sec vs 10 min manual)

**Prompt Engineering**:
- ✅ Be direct (no preambles)
- ✅ Use XML tags for structure
- ✅ Show reasoning with `<thinking>` when needed
- ✅ Provide concrete examples (multishot)
- ✅ Don't sycophancy (be honest, not overly agreeable)

**Tool Usage**:
- ✅ Use specialized tools (Read, Edit, Grep) not bash
- ✅ Parallel tool calls when independent
- ✅ TodoWrite for multi-step tasks
- ✅ Agent swarms for complex workflows

---

## 10. Application to Unity XR Development

### Unity-Specific Patterns

**Before Implementing Features**:
1. Launch research-agent (search Unity-XR-AI knowledge base)
2. Review findings (code snippets, GitHub repos)
3. Implement based on proven patterns

**Codebase Navigation**:
1. Use Explore agent (not manual grep/find)
2. Specify thoroughness (quick/medium/very thorough)
3. Let agent return summary with line numbers

**Testing Workflow**:
1. Write code
2. Launch code-tester agent (Unity MCP)
3. Fix issues
4. Repeat until passing

**Documentation Updates**:
1. Section-based reads (not full CLAUDE.md)
2. Update only changed sections
3. Verify against codebase with rg/grep

---

## 11. Integration with Unity-XR-AI Knowledge Base

**Cross-References**:
- This document: Claude AI best practices
- `_WEB_INTEROPERABILITY_STANDARDS.md`: Web deployment strategies
- `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md`: Unity VFX code snippets
- `_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md`: 520+ curated repos

**Workflow Integration**:
```xml
<workflow>Implement Unity XR Feature</workflow>
<step1>research-agent searches Unity-XR-AI knowledge base</step1>
<step2>Apply Claude prompt engineering best practices</step2>
<step3>Use Explore agent for codebase navigation</step3>
<step4>code-tester validates implementation</step4>
<step5>Update knowledge base with findings</step5>
```

---

**Last Updated**: 2025-11-02
**Total Resources**: 49 documentation links
**Category**: AI Assistant Best Practices, Claude Code Configuration, Prompt Engineering
**Maintenance**: Update when new Claude docs released or workflow patterns change
