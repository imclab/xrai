# Advanced AI Infrastructure Guide

**Tags**: `infrastructure` `vllm` `dify` `ragflow` `optional`
**Updated**: 2026-01-21
**Status**: Future upgrade path (not required for current workflow)

---

## When to Upgrade

**Current System** (Simple, Working):
```
KB files → grep/pattern-indexer.sh → Claude Code API
```

**Upgrade When**:
- KB exceeds 1000+ files (semantic search needed)
- Multiple team members need shared AI context
- Need to run local LLMs for privacy/cost
- Building customer-facing AI features

**Don't Upgrade If**:
- Current grep-based search works fine
- Using Claude Code API (managed inference)
- Solo developer workflow
- KB under 500 files

---

## Tool Comparison

| Tool | Purpose | Complexity | Your Need |
|------|---------|------------|-----------|
| **vLLM** | Fast local LLM inference | High | Only if self-hosting LLMs |
| **Dify** | Agent workflow platform | Medium | Claude Code already does this |
| **RAGFlow** | Document RAG | Medium | If KB search is slow |

---

## vLLM Setup (Local LLM Inference)

**Use Case**: Run local models (Llama, Mistral) 24x faster than HuggingFace

### Requirements
- GPU with 24GB+ VRAM (RTX 4090, A100)
- Python 3.8+
- CUDA 12.1+

### Installation
```bash
# Install vLLM
pip install vllm

# Start server (example: Llama 3.1 8B)
vllm serve meta-llama/Llama-3.1-8B-Instruct \
  --host 0.0.0.0 \
  --port 8000 \
  --tensor-parallel-size 1
```

### OpenAI-Compatible API
```python
from openai import OpenAI

client = OpenAI(
    base_url="http://localhost:8000/v1",
    api_key="not-needed"
)

response = client.chat.completions.create(
    model="meta-llama/Llama-3.1-8B-Instruct",
    messages=[{"role": "user", "content": "Explain VFX Graph"}]
)
```

### Integration with Claude Code
```bash
# In .claude/settings.json, add custom model endpoint
# (Not officially supported - use for experimentation only)
```

### When to Use vLLM
- Privacy-sensitive code analysis
- Offline development
- Cost optimization (no API fees)
- Custom fine-tuned models

### When NOT to Use
- You have Claude API access (better quality)
- No GPU available
- Need latest model capabilities

---

## Dify Setup (Agent Workflows)

**Use Case**: Build custom AI agents with visual workflow editor

### Installation (Docker)
```bash
# Clone Dify
git clone https://github.com/langgenius/dify.git
cd dify/docker

# Start services
docker-compose up -d
```

**Access**: http://localhost:3000

### Key Features
- Visual agent workflow builder
- Built-in RAG with vector stores
- Multi-model support (Claude, GPT, local)
- API endpoints for integration

### Integration with Unity KB
```yaml
# Create a "Unity KB Assistant" workflow:
1. Add Knowledge Base node
2. Upload KB markdown files
3. Add LLM node (Claude or local)
4. Create API endpoint
```

### Expose as MCP Server
```typescript
// Create custom MCP server that calls Dify API
// Then Claude Code can use Dify workflows as tools
```

### When to Use Dify
- Need visual workflow building
- Team collaboration on AI agents
- Custom chatbots for documentation
- Multi-step agentic workflows

### When NOT to Use
- Claude Code already handles your workflows
- Solo developer (overhead not worth it)
- Simple Q&A needs (overkill)

---

## RAGFlow Setup (Document RAG)

**Use Case**: Semantic search over large document collections

### Installation (Docker)
```bash
# Clone RAGFlow
git clone https://github.com/infiniflow/ragflow.git
cd ragflow/docker

# Start services (requires 16GB+ RAM)
docker-compose up -d
```

**Access**: http://localhost:9380

### Key Features
- Deep document parsing (PDF, DOCX, MD, code)
- Multiple chunking strategies
- Vector + keyword hybrid search
- Built-in LLM integration

### Integration with Unity KB

**Step 1: Upload KB**
```python
import requests

# Upload all KB markdown files
for file in glob.glob("KnowledgeBase/*.md"):
    with open(file, 'rb') as f:
        requests.post(
            "http://localhost:9380/api/document/upload",
            files={"file": f},
            headers={"Authorization": "Bearer <token>"}
        )
```

**Step 2: Create RAG Endpoint**
```python
# RAGFlow provides OpenAI-compatible chat endpoint
# Point Claude Code to use RAGFlow for KB queries
```

**Step 3: Query via API**
```bash
curl -X POST http://localhost:9380/api/chat \
  -H "Authorization: Bearer <token>" \
  -d '{"question": "How to optimize VFX Graph performance?"}'
```

### Custom MCP Server for RAGFlow
```typescript
// mcp-ragflow-server.ts
import { Server } from "@modelcontextprotocol/sdk/server";

const server = new Server({
  name: "ragflow-kb",
  version: "1.0.0"
});

server.setRequestHandler("tools/call", async (request) => {
  if (request.params.name === "search_kb") {
    const response = await fetch("http://localhost:9380/api/search", {
      method: "POST",
      body: JSON.stringify({ query: request.params.arguments.query })
    });
    return { content: await response.json() };
  }
});
```

### When to Use RAGFlow
- KB exceeds 500+ files
- Need semantic search (not just grep)
- Complex document formats (PDF, DOCX)
- Team needs shared search

### When NOT to Use
- Current grep/pattern-indexer works
- KB is mostly markdown (simple format)
- Don't need semantic search

---

## Recommended Upgrade Path

### Phase 0: Current (Stay Here If Working)
```
grep + pattern-indexer.sh + _QUICK_FIX.md
```
**Token Cost**: Zero for search
**Complexity**: Low

### Phase 1: RAGFlow Only (If Search Slow)
```
RAGFlow (KB search) → Claude Code (implementation)
```
**Token Cost**: Low (only LLM calls)
**Complexity**: Medium

### Phase 2: Full Stack (For Teams/Products)
```
RAGFlow (KB) + Dify (workflows) + vLLM (local inference)
```
**Token Cost**: Minimal (self-hosted)
**Complexity**: High

---

## Integration with GLOBAL_RULES.md

If you add these tools, update GLOBAL_RULES.md:

```markdown
### Advanced AI Infrastructure (Optional)

| Tool | Port | Purpose |
|------|------|---------|
| vLLM | 8000 | Local LLM inference |
| Dify | 3000 | Agent workflows |
| RAGFlow | 9380 | KB semantic search |

**MCP Integration**:
- `mcp-ragflow-kb` - Semantic KB search
- `mcp-dify-workflows` - Custom agent workflows

**Fallback Order**:
1. grep _QUICK_FIX.md (fastest)
2. grep _PATTERN_TAGS.md
3. RAGFlow semantic search
4. Claude Code full context
```

---

## Cost Comparison

| Approach | Setup Cost | Running Cost | Quality |
|----------|------------|--------------|---------|
| Current (Claude API) | $0 | ~$50-200/mo | Best |
| + RAGFlow | 2-4 hrs | +$0 (self-host) | Best + semantic |
| + vLLM | 4-8 hrs | +electricity | Good (local models) |
| Full stack | 8-16 hrs | +electricity | Flexible |

---

## Recommendation

**For James (Solo Unity Dev)**:
- **Stay with current system** until grep becomes slow
- If KB > 500 files: Add RAGFlow for semantic search
- Skip Dify (Claude Code handles workflows)
- Skip vLLM (Claude API quality is better)

**Future Trigger Points**:
- KB search takes > 5 seconds → Add RAGFlow
- API costs exceed $500/month → Consider vLLM
- Team grows to 3+ devs → Consider Dify for shared workflows

---

## See Also

- `_AI_CODING_BEST_PRACTICES.md` - Research-backed workflows
- `_SIMPLIFIED_INTELLIGENCE_CORE.md` - Current simple system
- `_PATTERN_ARCHITECTURE.md` - KB design principles
