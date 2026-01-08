# Knowledge Base Architecture Diagrams

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         CLIENT APPLICATIONS                              │
├────────────┬────────────┬────────────┬────────────┬──────────────────────┤
│   Unity    │   Unreal   │   Mobile   │    Web     │      Desktop         │
│  (WebGL)   │  (WebXR)   │ (RN/Flutter)│ (React)   │    (Electron)        │
│            │            │            │            │                      │
│ WebSocket  │ WebSocket  │ REST/WS    │ REST/WS    │    REST/GraphQL      │
└─────┬──────┴─────┬──────┴─────┬──────┴─────┬──────┴──────┬──────────────┘
      │            │            │            │             │
      └────────────┴────────────┴────────────┴─────────────┘
                                │
                    ┌───────────▼────────────┐
                    │   CLOUDFLARE CDN       │
                    │  - Edge caching        │
                    │  - DDoS protection     │
                    │  - SSL/TLS             │
                    │  - Image optimization  │
                    └───────────┬────────────┘
                                │
                    ┌───────────▼────────────┐
                    │    API GATEWAY         │
                    │  - Kong / Envoy        │
                    │  - Auth validation     │
                    │  - Rate limiting       │
                    │  - Request routing     │
                    └───┬────────┬───────┬───┘
                        │        │       │
              ┌─────────┤        │       └──────────┐
              │         │        │                  │
      ┌───────▼──┐ ┌────▼────┐ ┌▼────────┐  ┌──────▼──────┐
      │  REST    │ │ GraphQL │ │WebSocket│  │ MCP Server  │
      │  API     │ │  API    │ │  Sync   │  │ (AI Tools)  │
      │(FastAPI) │ │(Apollo) │ │(Node.js)│  │             │
      └────┬─────┘ └────┬────┘ └────┬────┘  └──────┬──────┘
           │            │           │              │
           └────────────┴───────────┴──────────────┘
                                │
                    ┌───────────▼────────────┐
                    │    SYNC ENGINE         │
                    │  - Yjs (text)          │
                    │  - Automerge (JSON)    │
                    │  - Event Sourcing      │
                    │  - CRDT resolution     │
                    └────────┬───────────────┘
                             │
              ┌──────────────┴───────────────┐
              │                              │
    ┌─────────▼──────────┐      ┌───────────▼──────────┐
    │   VECTOR DATABASE  │      │   OBJECT STORAGE     │
    │   (Qdrant)         │      │   (S3 / R2)          │
    │                    │      │                      │
    │ - Semantic search  │      │ - Raw files          │
    │ - Embeddings       │      │ - Versions           │
    │ - Metadata         │      │ - Backups            │
    │ - 99%+ recall      │      │ - Media assets       │
    └────────────────────┘      └──────────────────────┘
```

## Client-Side Architecture (Offline-First)

```
┌─────────────────────────────────────────────────────┐
│              BROWSER / MOBILE APP                    │
│                                                      │
│  ┌────────────────────────────────────────────────┐ │
│  │           USER INTERFACE (React/RN)            │ │
│  └──────────────────┬─────────────────────────────┘ │
│                     │                                │
│  ┌──────────────────▼─────────────────────────────┐ │
│  │         APPLICATION STATE (Redux/Zustand)      │ │
│  └──────────────────┬─────────────────────────────┘ │
│                     │                                │
│  ┌──────────────────▼─────────────────────────────┐ │
│  │      LOCAL DATABASE (Primary SSOT)             │ │
│  │                                                 │ │
│  │  ┌───────────────┐  ┌─────────────────────┐   │ │
│  │  │  IndexedDB    │  │  WatermelonDB       │   │ │
│  │  │  (Web)        │  │  (React Native)     │   │ │
│  │  └───────────────┘  └─────────────────────┘   │ │
│  │                                                 │ │
│  │  - Documents (Yjs format)                      │ │
│  │  - Metadata (JSON)                             │ │
│  │  - Search index (local)                        │ │
│  │  - User preferences                            │ │
│  └──────────────────┬─────────────────────────────┘ │
│                     │                                │
│  ┌──────────────────▼─────────────────────────────┐ │
│  │         SYNC WORKER (Background)               │ │
│  │                                                 │ │
│  │  - Queue local changes                         │ │
│  │  - Fetch remote updates                        │ │
│  │  - CRDT merge (conflict-free)                  │ │
│  │  - Retry failed syncs                          │ │
│  └──────────────────┬─────────────────────────────┘ │
│                     │                                │
└─────────────────────┼────────────────────────────────┘
                      │
                      │ WebSocket / HTTP
                      ▼
              ┌───────────────┐
              │  API GATEWAY  │
              └───────────────┘
```

## Data Flow - Write Operation

```
┌──────────────┐
│ User Edit    │ 1. User types "Hello World" in editor
└──────┬───────┘
       │
       ▼
┌──────────────────────────────┐
│ 2. Apply to Local Store      │ IndexedDB.put(doc, changes)
│    (Optimistic Update)       │ UI updates immediately
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│ 3. Queue for Sync            │ syncQueue.add({
│                              │   op: "update",
└──────┬───────────────────────┘   doc_id: "abc",
       │                            changes: [...]
       │                          })
       ▼
┌──────────────────────────────┐
│ 4. Background Sync Worker    │ if (navigator.onLine) {
│    (Check Network)           │   sendToServer()
└──────┬───────────────────────┘ }
       │
       ▼
┌──────────────────────────────┐
│ 5. Send to Server            │ WebSocket.send({
│    (WebSocket)               │   type: "yjs_update",
└──────┬───────────────────────┘   update: YjsUpdate
       │                          })
       │
       ▼
┌──────────────────────────────┐
│ 6. Server CRDT Merge         │ YDoc.applyUpdate(update)
│                              │ No conflicts possible!
└──────┬───────────────────────┘
       │
       ├────────────────┬──────────────────┐
       │                │                  │
       ▼                ▼                  ▼
┌──────────────┐ ┌─────────────┐  ┌──────────────┐
│ 7a. Qdrant   │ │ 7b. S3/R2   │  │ 7c. Event    │
│ (Embedding)  │ │ (Raw File)  │  │ Store (Audit)│
└──────────────┘ └─────────────┘  └──────────────┘
       │                │                  │
       └────────────────┴──────────────────┘
                        │
                        ▼
┌───────────────────────────────────────┐
│ 8. Broadcast to Other Clients         │
│    (Real-time Collaboration)          │
└───────────────────────────────────────┘
```

## Data Flow - Read Operation (Semantic Search)

```
┌──────────────────┐
│ User Search      │ 1. User types "hand tracking ARKit"
│ "hand tracking"  │
└──────┬───────────┘
       │
       ▼
┌──────────────────────────────┐
│ 2. Check Local Cache         │ cache = localStorage.get("search:hand")
│                              │ if (cache && fresh) return cache
└──────┬───────────────────────┘
       │ Cache miss
       ▼
┌──────────────────────────────┐
│ 3. Generate Query Embedding  │ embedding = await openai.embeddings.create({
│    (Client or Server)        │   model: "text-embedding-3-large",
└──────┬───────────────────────┘   input: "hand tracking ARKit"
       │                          })
       ▼
┌──────────────────────────────┐
│ 4. Send to API               │ POST /api/search
│                              │ { query: "...", embedding: [...] }
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│ 5. Qdrant Vector Search      │ qdrant.search({
│    (Semantic Similarity)     │   collection: "kb",
└──────┬───────────────────────┘   vector: embedding,
       │                            limit: 10,
       │                            score_threshold: 0.7
       ▼                          })
┌──────────────────────────────┐
│ 6. Results Returned          │ [
│    (Ranked by Similarity)    │   {id: "doc1", score: 0.95, ...},
└──────┬───────────────────────┘   {id: "doc2", score: 0.89, ...}
       │                          ]
       ▼
┌──────────────────────────────┐
│ 7. Hydrate Full Documents    │ docs = await s3.getObjects(doc_ids)
│    (From S3/R2)              │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│ 8. Return to Client          │ Response: {
│                              │   results: [...],
└──────┬───────────────────────┘   took_ms: 45
       │                          }
       ▼
┌──────────────────────────────┐
│ 9. Cache Locally             │ localStorage.set("search:hand", results)
│    (For Offline)             │ indexedDB.put(results)
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│ 10. Display Results          │ UI.render(results)
│     (<50ms total)            │
└──────────────────────────────┘
```

## Sync Flow - Conflict Resolution (CRDT)

```
┌─────────────┐                      ┌─────────────┐
│  Client A   │                      │  Client B   │
│  (Offline)  │                      │  (Offline)  │
└──────┬──────┘                      └──────┬──────┘
       │                                    │
       │ Edit: "Hello World"                │ Edit: "Hello Unity"
       │ (Same document, same time)         │
       │                                    │
       ▼                                    ▼
┌──────────────┐                      ┌──────────────┐
│ Local Yjs    │                      │ Local Yjs    │
│ State A      │                      │ State B      │
└──────┬───────┘                      └──────┬───────┘
       │                                    │
       │ Both come online                   │
       │                                    │
       └────────────┬───────────────────────┘
                    │
                    ▼
         ┌──────────────────────┐
         │   Sync Server        │
         │   (Yjs Awareness)    │
         └──────────┬───────────┘
                    │
            ┌───────┴────────┐
            │                │
            ▼                ▼
    ┌───────────────┐ ┌───────────────┐
    │ Apply Update  │ │ Apply Update  │
    │ from Client A │ │ from Client B │
    └───────┬───────┘ └───────┬───────┘
            │                │
            └────────┬────────┘
                     │
                     ▼
          ┌──────────────────────┐
          │   CRDT Merge         │
          │   (Automatic)        │
          │                      │
          │  Result: "Hello      │
          │           Unity      │
          │           World"     │
          │                      │
          │  NO CONFLICT!        │
          └──────────┬───────────┘
                     │
         ┌───────────┴────────────┐
         │                        │
         ▼                        ▼
┌─────────────────┐      ┌─────────────────┐
│ Broadcast to    │      │ Broadcast to    │
│ Client A        │      │ Client B        │
│                 │      │                 │
│ "Hello          │      │ "Hello          │
│  Unity          │      │  Unity          │
│  World"         │      │  World"         │
└─────────────────┘      └─────────────────┘
```

## Security Architecture

```
┌──────────────────────────────────────────────────────┐
│                    CLIENT                             │
└───────────────────┬──────────────────────────────────┘
                    │
                    │ 1. Login Request
                    ▼
┌──────────────────────────────────────────────────────┐
│              AUTH PROVIDER (Auth0/Cognito)           │
│                                                       │
│  - Username/Password OR                              │
│  - Social OAuth (Google/GitHub) OR                   │
│  - WebAuthn (Fingerprint/Face ID)                    │
└───────────────────┬──────────────────────────────────┘
                    │
                    │ 2. Return Tokens
                    │    - Access Token (JWT, 15 min)
                    │    - Refresh Token (7 days)
                    ▼
┌──────────────────────────────────────────────────────┐
│                    CLIENT                             │
│                                                       │
│  localStorage.setItem("access_token", jwt)           │
└───────────────────┬──────────────────────────────────┘
                    │
                    │ 3. API Request
                    │    Authorization: Bearer <jwt>
                    ▼
┌──────────────────────────────────────────────────────┐
│              API GATEWAY                              │
│                                                       │
│  ┌────────────────────────────────────────────────┐  │
│  │ JWT Validation Middleware                      │  │
│  │                                                │  │
│  │  1. Verify signature (public key)             │  │
│  │  2. Check expiration                          │  │
│  │  3. Validate issuer/audience                  │  │
│  │  4. Check rate limits                         │  │
│  └────────────┬───────────────────────────────────┘  │
│               │                                       │
│               ▼                                       │
│  ┌────────────────────────────────────────────────┐  │
│  │ Authorization Middleware                       │  │
│  │                                                │  │
│  │  1. Extract user_id from JWT                  │  │
│  │  2. Load user permissions (RBAC)              │  │
│  │  3. Check resource access (ABAC)              │  │
│  └────────────┬───────────────────────────────────┘  │
└────────────────┼──────────────────────────────────────┘
                 │
                 │ 4. Authorized Request
                 ▼
┌──────────────────────────────────────────────────────┐
│              APPLICATION SERVICES                     │
│                                                       │
│  - Apply row-level security (Qdrant)                 │
│  - Log access (audit trail)                          │
│  - Return filtered results                           │
└──────────────────────────────────────────────────────┘
```

## Deployment Architecture (Production)

```
┌─────────────────────────────────────────────────────────────┐
│                      CLOUDFLARE                              │
│  - DNS                                                       │
│  - DDoS Protection                                           │
│  - WAF (Web Application Firewall)                           │
│  - CDN (200+ edge locations)                                │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                  LOAD BALANCER (AWS ALB)                     │
│  - Health checks                                             │
│  - SSL/TLS termination                                       │
│  - Sticky sessions (WebSocket)                               │
└──────┬────────────────────────────────────┬─────────────────┘
       │                                    │
       ▼                                    ▼
┌──────────────────┐              ┌──────────────────┐
│  API Server 1    │              │  API Server 2    │
│  (Auto-scaling)  │              │  (Auto-scaling)  │
│                  │              │                  │
│  - FastAPI       │              │  - FastAPI       │
│  - Node.js       │              │  - Node.js       │
│  - Docker        │              │  - Docker        │
└────────┬─────────┘              └────────┬─────────┘
         │                                 │
         └─────────────┬───────────────────┘
                       │
            ┌──────────┴───────────┐
            │                      │
            ▼                      ▼
┌─────────────────────┐  ┌─────────────────────┐
│   Qdrant Cluster    │  │   S3 / R2 Storage   │
│   (3 nodes)         │  │                     │
│                     │  │  - Multi-region     │
│  - Leader + 2 Rep   │  │  - Versioning ON    │
│  - Auto-failover    │  │  - Encryption       │
│  - Backups daily    │  │  - Lifecycle rules  │
└─────────────────────┘  └─────────────────────┘

         Monitoring & Observability
┌─────────────────────────────────────────────┐
│  - Prometheus (metrics)                     │
│  - Grafana (dashboards)                     │
│  - Loki (logs)                              │
│  - Jaeger (tracing)                         │
│  - PagerDuty (alerts)                       │
└─────────────────────────────────────────────┘
```

---

## Technology Stack Summary

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Frontend** | React / React Native | Web & mobile UI |
| **Local DB** | IndexedDB / WatermelonDB | Offline-first storage |
| **Sync** | Yjs + Automerge | CRDT conflict resolution |
| **API** | FastAPI + Apollo + Socket.io | REST + GraphQL + WebSocket |
| **Gateway** | Kong / Envoy | Auth, rate limit, routing |
| **Auth** | Auth0 / Cognito | OAuth2 + JWT |
| **Vector DB** | Qdrant | Semantic search |
| **Storage** | S3 / Cloudflare R2 | Object storage |
| **CDN** | Cloudflare | Global edge cache |
| **AI** | MCP + OpenAI | Tool integration + embeddings |
| **Monitor** | Prometheus + Grafana | Metrics & alerts |
| **Deploy** | Docker + Kubernetes | Container orchestration |

---

**Version**: 1.0
**Created**: January 7, 2026
**Companion Document**: CLOUD_NATIVE_KB_ARCHITECTURE_2025.md
