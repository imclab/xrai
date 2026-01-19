# LLM for Unity - AI Character Integration

**Source**: [undreamai/LLMUnity](https://github.com/undreamai/LLMUnity)
**Asset Store**: [LLM for Unity](https://assetstore.unity.com/packages/slug/273604)
**Documentation**: [undream.ai/LLMUnity](https://undream.ai/LLMUnity)
**License**: Apache 2.0

---

## Overview

Seamless integration of Large Language Models (LLMs) in Unity for creating intelligent AI characters. Runs locally on CPU/GPU with no internet required. Includes RAG system for semantic search.

---

## Key Features

- **Local Execution** - No internet, no data leaves game
- **Cross-Platform** - PC, Mobile (iOS/Android), VR
- **Fast Inference** - CPU + GPU (Nvidia, AMD, Apple Metal)
- **All Major LLMs** - GGUF format models
- **RAG System** - Semantic search with ANN
- **Remote Server** - Optional distributed setup

---

## Quick Start

### 1. Setup LLM
```
1. Create empty GameObject
2. Add Component → LLM script
3. Download Model or Load .gguf
```

### 2. Create AI Character
```
1. Create empty GameObject
2. Add Component → LLMAgent script
3. Set System Prompt (character role)
4. Link to LLM GameObject
```

### 3. Use in Script
```csharp
using LLMUnity;

public class MyScript : MonoBehaviour
{
    public LLMAgent llmAgent;

    async void Start()
    {
        string reply = await llmAgent.Chat("Hello bot!");
        Debug.Log(reply);
    }

    // Streaming response
    void HandleReply(string replySoFar)
    {
        Debug.Log(replySoFar);
    }

    void Game()
    {
        _ = llmAgent.Chat("Hello!", HandleReply);
    }
}
```

---

## RAG (Semantic Search)

```csharp
RAG rag = gameObject.AddComponent<RAG>();
rag.Init(SearchMethods.DBSearch, ChunkingMethods.SentenceSplitter, llm);

// Add data
await rag.Add("Hi! I'm a search system.");
await rag.Add("The weather is nice.");

// Search
(string[] results, float[] distances) = await rag.Search("hello!", 2);

// Feed to LLM
string prompt = $"Answer based on:\n{string.Join("\n", results)}";
await llmAgent.Chat(prompt);
```

---

## Project Structure

```
LLMUnity/
├── Editor/              # Editor tools, model manager
├── Runtime/             # Core LLM/Agent scripts
├── Samples~/
│   ├── SimpleInteraction/
│   ├── MultipleCharacters/
│   ├── FunctionCalling/
│   ├── RAG/
│   ├── MobileDemo/
│   ├── ChatBot/
│   └── KnowledgeBaseGame/
├── Plugins/             # LlamaLib native binaries
└── package.json
```

---

## Mobile Builds

### iOS
Default player settings work.

### Android
Required settings:
- Scripting Backend: `IL2CPP`
- Target Architecture: `ARM64`

Use `Download on Build` for smaller APK (models download on first launch).

---

## Model Management

Built-in model manager with:
- Download models from HuggingFace
- Load local .gguf files
- Q4_K_M quantized models included
- Build-time model bundling
- Runtime model download option

---

## Games Using LLMUnity

- Verbal Verdict (Steam)
- I, Chatbot: AISYLUM (Epic)
- Case Closed (Steam)
- Digital Humans (Steam)
- Dating App Simulator (Steam)
- Psycho Simulator (Steam)

---

## Integration Patterns

### Function Calling
Use grammar to restrict output to function names:
```csharp
llmAgent.grammar = @"root ::= ""call_function1"" | ""call_function2""";
```

### Chat History
```csharp
// Save
await llmAgent.SaveHistory();

// Load
await llmAgent.LoadHistory();

// Access
List<ChatMessage> history = llmAgent.chat;
```

### Remote Server
1. Create LLM project with `Remote` enabled
2. Build and run server
3. Client uses `LLMAgent` with remote IP

---

## Related Projects

- `TamagotchU/` - ML-Agents integration
- `KnowledgeBase/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` - AI/ML repos
