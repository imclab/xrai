# Building MCP with LLMs Tutorial

> **Source**: https://modelcontextprotocol.io/tutorials/building-mcp-with-llms  
> **Last Updated**: 2025-01-13

---

## Overview

This guide explains how to use LLMs (like Claude) to help build custom MCP servers and clients faster.

---

## Step 1: Prepare Documentation

Gather the necessary documentation for the LLM:

1. **Full MCP docs**: https://modelcontextprotocol.io/llms-full.txt
2. **SDK documentation**:
   - TypeScript: https://github.com/modelcontextprotocol/typescript-sdk
   - Python: https://github.com/modelcontextprotocol/python-sdk
3. Copy README files and relevant docs into your conversation

---

## Step 2: Describe Your Server

Be specific about what you want to build:

- What **resources** will it expose?
- What **tools** will it provide?
- What **prompts** should it offer?
- What **external systems** does it connect to?

### Example Prompt

```
Build an MCP server that:
- Connects to my company's PostgreSQL database
- Exposes table schemas as resources
- Provides tools for running read-only SQL queries
- Includes prompts for common data analysis tasks
```

---

## Step 3: Work Iteratively with Claude

1. **Start with core functionality** - then iterate to add features
2. **Ask for explanations** - understand the code
3. **Request modifications** - as requirements evolve
4. **Get help testing** - handle edge cases

### Claude Can Implement

- Resource management and exposure
- Tool definitions and implementations
- Prompt templates and handlers
- Error handling and logging
- Connection and transport setup

---

## Step 4: Best Practices

When building MCP servers with Claude:

1. **Break down complex servers** into smaller pieces
2. **Test each component** before moving on
3. **Keep security in mind** - validate inputs, limit access
4. **Document your code** well
5. **Follow MCP specification** carefully

---

## Step 5: After Building

1. **Review generated code** carefully
2. **Test with MCP Inspector** tool
3. **Connect to Claude Desktop** or other MCP clients
4. **Iterate** based on real usage and feedback

---

## Tips

- Claude can modify and improve servers as requirements change
- Ask specific questions about implementing MCP features
- Get help troubleshooting issues that arise
- Use the full documentation URL for best results

---

## Quick Reference

| Task | Prompt Example |
|------|----------------|
| Create tool | "Add a tool that queries the user database and returns results" |
| Add resource | "Expose the config file as a resource with proper URI scheme" |
| Handle errors | "Add error handling for database connection failures" |
| Add auth | "Implement OAuth authentication for the API calls" |
| Test edge cases | "Help me test what happens when the API returns empty results" |

---

*Created for Unity-XR-AI Knowledge Base*
