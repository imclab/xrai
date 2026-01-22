# WarpJobs Intelligence Findings

**Last Updated:** 2026-01-20 (Deep Audit v3.0)
**Source:** Automated job search system + KB analysis + Deep Audit

---

## Top XR/AI Market Signals (from your KB)

| Keyword | Mentions | Priority |
|---------|----------|----------|
| Unity | 2,284 | HIGHEST |
| Meta | 539 | HIGH |
| API | 485 | HIGH |
| Quest | 283 | HIGH |
| ARKit | 221 | MEDIUM |
| RAG | 183 | MEDIUM |
| Gaussian Splatting | 125 | MEDIUM |
| AR Foundation | 124 | MEDIUM |

---

## Active Roadmap Intelligence

### Unity (Latest)
- "5 top trends redefining industry in 2026" (Jan 12)
- Unity for Humanity 2026 grant open
- Games made with Unity: 2025 review

### OpenAI
- Multimodal API expansions
- Vision model updates

### Google/Gemini
- Gemini multimodal capabilities
- ARCore SDK updates

### NVIDIA
- Omniverse/NeRF developments
- CUDA/DLSS updates

### Apple
- Vision Pro / ARKit / Spatial Computing
- ML model releases

---

## Patent Search URLs (Track Weekly)

- [Meta VR Patents](https://patents.google.com/?assignee=Meta&q=virtual+reality)
- [Apple Spatial Patents](https://patents.google.com/?assignee=Apple&q=spatial+computing)
- [Google AR Patents](https://patents.google.com/?assignee=Google&q=augmented+reality)
- [OpenAI Multimodal Patents](https://patents.google.com/?assignee=OpenAI&q=multimodal)
- [Unity XR Patents](https://patents.google.com/?assignee=Unity&q=XR)

---

## Job Market Intelligence

### Current Stats (2026-01-20 Deep Audit)
- Total jobs tracked: 892 (100% with site field)
- Dream jobs (enriched): 101
- Target companies: 1,538
- VC leads: 739
- Key people: 169
- LinkedIn opportunities: 1,271
- Active sources: 12

### Job Sources Distribution
| Source | Count |
|--------|-------|
| greenhouse | 215 |
| coinbase | 203 |
| roblox | 128 |
| unity | 121 |
| stripe | 87 |
| linkedin | 65 |
| epicgames | 40 |
| weworkremotely | 10 |
| remoteOK | 6 |
| stability | 6 |
| ashby | 5 |
| ycombinator | 4 |

### System Architecture (67 JS Files)
- Root scripts: 11
- Scripts directory: 29
- Lib modules: 27
- Data files: 36 JSON
- Cache files: 9

---

## 70/20/10 Strategy Reminder

| Activity | % | Weekly Target |
|----------|---|---------------|
| VC Networking | 70% | 14 touches |
| Direct Targeting | 20% | 4 applications |
| Job Boards | 10% | 2 exceptional only |

**Goal:** 10 high-level conversations > 100 applications

---

## Agent Capabilities (Ollama - Free/Local)

- `scoreJobRelevance(job)` - LLM scores job fit (0-100)
- `generateOutreach(target, 'vc')` - Personalized VC messages
- `generateOutreach(target, 'company')` - Company outreach
- `batchScoreJobs(jobs, limit)` - Batch scoring

**Model:** llama3.2 (local, no API cost)

---

## Auto-Improvement Loops (8 Active)

1. Scrapers - re-enable after cooldown, flag slow/low-yield
2. Scoring weights - boost keywords from top jobs
3. Role patterns - suggest new patterns from applied jobs
4. Data quality - flag stale/duplicate jobs
5. VC priorities - boost VCs with portfolio matches
6. Auto-fix - normalize data, fill missing fields
7. News trends - signal high-activity categories
8. Cleanup - dedupe and archive

---

## Integration Points

- KB Sync: `~/.claude/knowledgebase/` → WarpJobs every 2h
- Roadmap Scanner: 6 company blogs tracked
- Dashboard: http://localhost:3999
- LaunchAgent: Runs every 2 hours automatically
