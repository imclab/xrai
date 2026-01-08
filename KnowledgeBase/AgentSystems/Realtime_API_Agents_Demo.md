# Realtime API Agents Demo (OpenAI Agents SDK)

## Overview
- Demonstrates advanced low-latency voice agent patterns built on the OpenAI Realtime API and Agents SDK.
- Highlights integrated state management, event streaming, tool orchestration, and multi-agent handoffs.
- Repository offers both SDK-based implementation and an alternate `without-agents-sdk` branch for direct API usage.

## Setup
- Tech stack: Next.js + TypeScript front end.
- Install dependencies with `npm i`, then set `OPENAI_API_KEY` via shell environment or `.env` (copy provided template).
- Start development server with `npm run dev`, then access the UI at `http://localhost:3000`.
- Scenario selector in the UI loads predefined agent configurations for experimentation.

## Pattern 1 — Chat-Supervisor Hybrid
- Real-time voice agent handles conversational flow and lightweight tasks via `realtime-mini` (or similar) for minimal latency.
- Supervisor agent (e.g. `gpt-4.1`) owns heavy reasoning, tool invocation, and complex responses.
- Chat agent delivers immediate verbal feedback while deferring deeper work to the supervisor; expectation of short gaps (~2 seconds) before final answers arrive.
- Benefits:
  - Incremental migration path from text agents to voice without rewriting complex prompts.
  - High-quality reasoning/tool use retained from supervisor tier.
  - Lower cost than running full realtime intelligent models end-to-end.
- Customization tips:
  - Adjust `supervisorAgent` prompt to include existing instructions/tooling, optimized for concise spoken output.
  - Update `chatAgent` tone, greeting, and allowed actions list to control autonomy.
  - Prefer succinct YAML tool descriptors for chat agent to reduce parsing errors.
  - Tune model selection to balance latency and cost (e.g. `gpt-4o-mini-realtime` for chat, `gpt-4.1-mini` for supervisor).

## Pattern 2 — Sequential Handoffs
- Inspired by OpenAI Swarm: voice session flows through specialized agents connected by explicit handoff graph.
- Implemented via `RealtimeAgent` definitions with `handoffDescription`, `instructions`, `tools`, and `handoffs`.
- Example `simpleExample` configuration:
  - `greeter` welcomes users and offers haiku option.
  - On consent, handoff occurs to `haikuWriter` which collects topic and produces haiku.
- `customerServiceRetail` showcases production-style flow:
  - Agents for authentication, returns, sales, and simulated human escalation.
  - Uses helper `injectTransferTools` to provision tool calls enabling transitions.
  - Returns agent escalates to `o4-mini` for high-stakes verification and actions.
  - Prompts enforce state machine behavior to reliably capture data (names, phone numbers).

## Extending the Demo
- Add new agent sets under `src/app/agentConfigs/`, export from index for UI selection.
- `toolLogic` hooks enable real integrations; default behavior simply acknowledges tool calls.
- Provided metaprompt (and linked Voice Agent Metaprompter GPT) helps scaffold domain-specific state machines.
- Guardrails: moderation pipeline observed in `src/app/App.tsx`, marking responses `IN_PROGRESS`, `PASS`, or `FAIL` on guardrail events; adjust by locating `guardrail_tripped`.
- UI aids debugging with transcript + tool call history (left) and event log (right). Supports VAD/PTT toggles, audio playback control, and agent switching.

## References
- Video walkthrough (Chat-Supervisor): https://x.com/noahmacca/status/1927014156152058075
- Repository: https://github.com/openai/openai-apps-sdk-examples (see Realtime Agents Demo directory and `without-agents-sdk` branch).
