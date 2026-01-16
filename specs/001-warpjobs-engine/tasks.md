# Tasks: WarpJobs Intelligence Engine

**Spec**: 001-warpjobs-engine
**Created**: 2026-01-15

---

## Phase 1: Scraper Foundation

### Node.js Scraper Setup
- [ ] Initialize Node.js project with package.json
- [ ] Install dependencies: puppeteer, axios, cheerio
- [ ] Create `scraper/` directory structure
- [ ] Implement base `Scraper` class with retry logic
- [ ] Add proxy rotation support (optional)

### Site-Specific Scrapers
- [ ] Implement LinkedIn Jobs scraper
- [ ] Implement Google Careers scraper
- [ ] Implement Y Combinator Jobs scraper
- [ ] Add pagination handling (5+ pages)
- [ ] Implement rate limiting (respect robots.txt)

### Data Storage
- [ ] Define Job schema in `schema.js`
- [ ] Implement JSON file storage (`jobs_data.json`)
- [ ] Add deduplication logic (by URL hash)
- [ ] Implement data migration/upgrade scripts

### Zombie Process Handling
- [ ] Create `kill-zombies.sh` script
- [ ] Detect processes on port 7777
- [ ] Kill hanging Puppeteer/Chrome processes
- [ ] Integrate into startup script

---

## Phase 2: LLM Intelligence

### Gemini API Integration
- [ ] Set up Google AI API credentials
- [ ] Create `intelligence/` module
- [ ] Implement `analyzeJob(job)` function
- [ ] Parse LLM response into structured data

### Intelligence Fields
- [ ] Generate `priority_score` (0-100)
- [ ] Generate `match_explanation` (1-2 sentences)
- [ ] Extract `auto_tags` (technologies, skills)
- [ ] Store intelligence alongside job data

### User Profile Matching
- [ ] Define user profile schema (keywords, skills)
- [ ] Load profile from `config.json`
- [ ] Incorporate profile into LLM prompt
- [ ] Test: XR/AI/Spatial Computing prioritization

---

## Phase 3: Dashboard

### HTML/CSS Foundation
- [ ] Create `dashboard/index.html`
- [ ] Implement "Bloomberg Terminal" dark theme
- [ ] Add responsive table layout
- [ ] Include CSS-only loading indicators

### Data Display
- [ ] Load `jobs_data.json` via fetch
- [ ] Render jobs table with columns:
  - Priority Score
  - Company
  - Title
  - Location
  - Insight (tooltip)
  - Tags
- [ ] Implement sorting by priority score

### Filtering & Search
- [ ] Add keyword filter input
- [ ] Add tag filter (checkboxes)
- [ ] Add salary range filter
- [ ] Add location filter
- [ ] Persist filters in URL params

### Visualizations (P2)
- [ ] Add "Market Pulse" section
- [ ] Implement sparklines (trend over time)
- [ ] Add tag distribution chart
- [ ] Add company frequency chart

---

## Phase 4: Automation

### Scheduled Scraping
- [ ] Create cron job / LaunchAgent
- [ ] Run scraper daily at 6am
- [ ] Log results to `scrape.log`
- [ ] Send notification on completion (optional)

### Self-Healing
- [ ] Auto-restart on crash
- [ ] Retry failed scrapes (3 attempts)
- [ ] Alert on consecutive failures
- [ ] Health check endpoint

### Local Server
- [ ] Create Python HTTP server (`serve.py`)
- [ ] Serve dashboard on port 7777
- [ ] Auto-open browser on start
- [ ] Handle CORS for local development

---

## Definition of Done

Each task is complete when:
1. Feature works locally
2. No console errors
3. Documented in README
4. Tested with real data

---

## Technology Stack

| Component | Technology |
|-----------|------------|
| Scraper | Node.js + Puppeteer |
| Storage | JSON file (jobs_data.json) |
| Intelligence | Google Gemini API |
| Dashboard | Vanilla HTML/CSS/JS |
| Server | Python http.server |
| Automation | cron / LaunchAgent |

---

## Success Metrics

- [ ] 50+ jobs scraped in <2 minutes
- [ ] Dashboard loads in <500ms
- [ ] 100% of jobs have intelligence scores
- [ ] Zero manual intervention for daily updates

---

*Last Updated: 2026-01-15*
