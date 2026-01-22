# AI Character Patterns (LLMUnity)

**Source**: undreamai/LLMUnity
**Updated**: 2026-01-17

---

## Overview

Local LLM inference for Unity AI characters. Runs on device (CPU/GPU) with no internet required. Includes RAG for semantic search.

---

## Architecture

```
GGUF Model → LLM (server) → LLMAgent (character) → Chat Response
                                    ↓
                              History Persistence
                                    ↓
                              RAG (optional)
```

---

## Core Components

### LLMAgent (Main Character Class)

```csharp
using LLMUnity;

public class MyNPC : MonoBehaviour
{
    public LLMAgent agent;

    async void Start()
    {
        // Simple chat
        string reply = await agent.Chat("Hello!");
        Debug.Log(reply);
    }

    // Streaming response
    async void TalkToPlayer(string playerMessage)
    {
        await agent.Chat(playerMessage, HandlePartialReply, OnComplete);
    }

    void HandlePartialReply(string replySoFar)
    {
        // Update UI as response streams in
        uiText.text = replySoFar;
    }

    void OnComplete()
    {
        // Response finished
    }
}
```

### System Prompt (Personality)

```csharp
// Define character personality in Inspector or code
agent.systemPrompt = @"You are a wise old wizard who speaks in riddles.
You guard ancient secrets but will share them with worthy adventurers.
Keep responses under 50 words.";
```

### History Management

```csharp
// Save conversation
await agent.SaveHistory();

// Load previous conversation
await agent.LoadHistory();

// Clear history for new conversation
await agent.ClearHistory();

// Access current history
List<ChatMessage> history = agent.chat;
foreach (var msg in history)
{
    Debug.Log($"{msg.role}: {msg.content}");
}
```

---

## RAG (Retrieval-Augmented Generation)

Add knowledge base context to LLM responses.

```csharp
using LLMUnity;

public class KnowledgeNPC : MonoBehaviour
{
    public LLMAgent agent;
    RAG rag;

    async void Start()
    {
        // Initialize RAG with semantic search
        rag = gameObject.AddComponent<RAG>();
        rag.Init(SearchMethods.DBSearch, ChunkingMethods.SentenceSplitter, agent.llm);

        // Add knowledge base
        await rag.Add("The ancient sword Flamebrand was forged by dwarves.");
        await rag.Add("Dragons are weak to ice magic.");
        await rag.Add("The king's castle is north of the village.");
    }

    async Task<string> AskWithContext(string question)
    {
        // Search for relevant context
        (string[] results, float[] distances) = await rag.Search(question, 3);

        // Build prompt with context
        string context = string.Join("\n", results);
        string prompt = $"Based on this knowledge:\n{context}\n\nAnswer: {question}";

        return await agent.Chat(prompt);
    }
}
```

---

## Function Calling

Constrain LLM output to valid function names using grammar.

```csharp
public class AIController : MonoBehaviour
{
    public LLMAgent agent;

    void Start()
    {
        // Constrain output to valid actions
        agent.grammar = @"root ::= ""attack"" | ""defend"" | ""heal"" | ""flee""";
    }

    async void DecideAction()
    {
        string action = await agent.Chat("Enemy approaches. What do you do?");

        switch (action.Trim())
        {
            case "attack": Attack(); break;
            case "defend": Defend(); break;
            case "heal": Heal(); break;
            case "flee": Flee(); break;
        }
    }
}
```

---

## Multi-Character Setup

Multiple agents sharing one LLM server.

```csharp
public class TavernScene : MonoBehaviour
{
    public LLM sharedLLM;           // One server
    public LLMAgent bartender;      // Uses slot 0
    public LLMAgent guardCaptain;   // Uses slot 1
    public LLMAgent mysteriousStranger; // Uses slot 2

    void Start()
    {
        // Each agent gets unique slot for context caching
        bartender.slot = 0;
        bartender.systemPrompt = "You are a friendly bartender...";

        guardCaptain.slot = 1;
        guardCaptain.systemPrompt = "You are a gruff guard captain...";

        mysteriousStranger.slot = 2;
        mysteriousStranger.systemPrompt = "You are mysterious and cryptic...";
    }
}
```

---

## Warmup (Faster First Response)

Pre-process system prompt for faster initial responses.

```csharp
async void Start()
{
    // Cache system prompt processing
    await agent.Warmup();

    // First chat will be faster
    await agent.Chat("Hello!");
}

// Or warmup with specific prompt
async void PrepareForCombat()
{
    await agent.Warmup("Prepare for battle!");
}
```

---

## Mobile Configuration

### iOS
Default settings work.

### Android
Required in Player Settings:
```
Scripting Backend: IL2CPP
Target Architecture: ARM64
```

### Smaller App Size
Enable "Download on Build" in LLM component - models download on first launch.

---

## Model Selection

| Size | RAM | Quality | Use Case |
|------|-----|---------|----------|
| 1-3B | 2-4GB | Basic | Mobile, simple NPC |
| 7B | 6-8GB | Good | Desktop, main characters |
| 13B+ | 12GB+ | Best | High-end, complex dialogue |

Recommended format: **Q4_K_M** quantization (balance of size/quality)

---

## Inspector Setup

1. Create empty GameObject
2. Add **LLM** component (one per scene)
   - Download or load .gguf model
   - Set parallel prompts (slots) count
3. Add **LLMAgent** component (per character)
   - Link to LLM
   - Set system prompt
   - Set slot (-1 for auto)
   - Set save filename for persistence

---

## Performance Tips

- Use **slots** for multi-character (shares model memory)
- **Warmup** reduces first response latency by 2-5s
- Keep system prompts under 500 tokens
- Use **grammar** to constrain output for actions
- Enable **RAG** only when knowledge base is needed

---

## Integration with VFX

Trigger VFX based on AI responses:

```csharp
async void ReactToResponse(string question)
{
    string response = await agent.Chat(question);

    // Parse emotion from response
    if (response.Contains("angry") || response.Contains("attack"))
    {
        angryVFX.Play();
    }
    else if (response.Contains("happy") || response.Contains("thank"))
    {
        happyVFX.Play();
    }
}
```

---

## Related Files

- `LLMUnity/Runtime/LLMAgent.cs` - Main character class
- `LLMUnity/Runtime/RAG/RAG.cs` - Semantic search
- `LLMUnity/Samples~/ChatBot/` - Chat UI sample
- `LLMUnity/Samples~/FunctionCalling/` - Action grammar
- `LLMUnity/Samples~/MultipleCharacters/` - Multi-agent
- `KnowledgeBase/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` - AI/ML repos
