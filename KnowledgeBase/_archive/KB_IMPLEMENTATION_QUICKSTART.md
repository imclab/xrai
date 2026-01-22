# Knowledge Base Implementation Quick Start

**Goal**: Get a working prototype running in 1-2 days
**Stack**: Qdrant + FastAPI + React + Yjs + Cloudflare R2

---

## Day 1: Backend Setup (4-6 hours)

### Step 1: Install Qdrant (10 min)

```bash
# Option A: Docker (recommended for local dev)
docker run -p 6333:6333 -p 6334:6334 \
    -v $(pwd)/qdrant_storage:/qdrant/storage:z \
    qdrant/qdrant

# Option B: Docker Compose
cat > docker-compose.yml <<EOF
version: '3.8'
services:
  qdrant:
    image: qdrant/qdrant:latest
    ports:
      - "6333:6333"
      - "6334:6334"
    volumes:
      - ./qdrant_storage:/qdrant/storage
EOF

docker-compose up -d

# Test connection
curl http://localhost:6333/
```

### Step 2: Set Up FastAPI Backend (30 min)

```bash
# Create project
mkdir kb-backend && cd kb-backend
python3 -m venv venv
source venv/bin/activate

# Install dependencies
pip install fastapi uvicorn qdrant-client openai pydantic python-multipart

# Create main.py
cat > main.py <<'EOF'
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from qdrant_client import QdrantClient
from qdrant_client.models import Distance, VectorParams, PointStruct
from openai import OpenAI
from pydantic import BaseModel
import uuid

app = FastAPI()

# CORS for local development
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:3000"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Initialize clients
qdrant = QdrantClient(url="http://localhost:6333")
openai_client = OpenAI()

# Create collection if not exists
try:
    qdrant.create_collection(
        collection_name="knowledge_base",
        vectors_config=VectorParams(size=3072, distance=Distance.COSINE),
    )
except Exception as e:
    print(f"Collection exists or error: {e}")

# Models
class Document(BaseModel):
    content: str
    metadata: dict = {}

class SearchQuery(BaseModel):
    query: str
    limit: int = 10

# Endpoints
@app.post("/api/documents")
async def add_document(doc: Document):
    """Add a document with semantic search indexing"""

    # Generate embedding
    response = openai_client.embeddings.create(
        model="text-embedding-3-large",
        input=doc.content
    )
    embedding = response.data[0].embedding

    # Store in Qdrant
    doc_id = str(uuid.uuid4())
    qdrant.upsert(
        collection_name="knowledge_base",
        points=[
            PointStruct(
                id=doc_id,
                vector=embedding,
                payload={
                    "content": doc.content,
                    "metadata": doc.metadata
                }
            )
        ]
    )

    return {"id": doc_id, "status": "indexed"}

@app.post("/api/search")
async def search(query: SearchQuery):
    """Semantic search across knowledge base"""

    # Generate query embedding
    response = openai_client.embeddings.create(
        model="text-embedding-3-large",
        input=query.query
    )
    query_vector = response.data[0].embedding

    # Search Qdrant
    results = qdrant.search(
        collection_name="knowledge_base",
        query_vector=query_vector,
        limit=query.limit,
        score_threshold=0.7
    )

    return {
        "results": [
            {
                "id": hit.id,
                "score": hit.score,
                "content": hit.payload.get("content"),
                "metadata": hit.payload.get("metadata")
            }
            for hit in results
        ]
    }

@app.get("/api/health")
async def health():
    return {"status": "healthy"}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
EOF

# Create .env file
cat > .env <<EOF
OPENAI_API_KEY=your_key_here
EOF

# Run server
uvicorn main:app --reload
```

### Step 3: Test Backend (5 min)

```bash
# In another terminal

# Test health
curl http://localhost:8000/api/health

# Add a test document
curl -X POST http://localhost:8000/api/documents \
  -H "Content-Type: application/json" \
  -d '{
    "content": "ARKit hand tracking supports 91 joints including 48 finger joints (24 per hand)",
    "metadata": {"platform": "iOS", "category": "hand-tracking"}
  }'

# Search
curl -X POST http://localhost:8000/api/search \
  -H "Content-Type: application/json" \
  -d '{
    "query": "How many finger joints does ARKit support?"
  }'
```

### Step 4: Set Up Cloudflare R2 (15 min)

```bash
# Install Wrangler CLI
npm install -g wrangler

# Login to Cloudflare
wrangler login

# Create R2 bucket
wrangler r2 bucket create knowledge-base

# Get API credentials
# Go to: https://dash.cloudflare.com → R2 → Manage R2 API Tokens
# Create token, save: Account ID, Access Key ID, Secret Access Key
```

```python
# Add to main.py
import boto3
from botocore.config import Config

# R2 client (S3-compatible)
r2 = boto3.client(
    's3',
    endpoint_url='https://<account-id>.r2.cloudflarestorage.com',
    aws_access_key_id='<access-key-id>',
    aws_secret_access_key='<secret-access-key>',
    config=Config(signature_version='s3v4')
)

@app.post("/api/documents/upload")
async def upload_document(file: UploadFile):
    """Upload raw file to R2"""

    file_id = str(uuid.uuid4())
    file_key = f"documents/{file_id}/{file.filename}"

    # Upload to R2
    r2.upload_fileobj(
        file.file,
        'knowledge-base',
        file_key
    )

    # Also index content if text
    if file.filename.endswith('.md'):
        content = await file.read()
        # Generate embedding and store in Qdrant...

    return {"file_id": file_id, "key": file_key}
```

---

## Day 2: Frontend Setup (4-6 hours)

### Step 1: Create React App (10 min)

```bash
# Create app
npx create-react-app kb-frontend
cd kb-frontend

# Install dependencies
npm install @tanstack/react-query axios yjs y-websocket
```

### Step 2: Build Search Interface (30 min)

```javascript
// src/App.js
import React, { useState } from 'react';
import axios from 'axios';

const API_URL = 'http://localhost:8000';

function App() {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState([]);
  const [loading, setLoading] = useState(false);

  const handleSearch = async (e) => {
    e.preventDefault();
    setLoading(true);

    try {
      const response = await axios.post(`${API_URL}/api/search`, {
        query,
        limit: 10
      });
      setResults(response.data.results);
    } catch (error) {
      console.error('Search failed:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ padding: '20px', maxWidth: '800px', margin: '0 auto' }}>
      <h1>Knowledge Base Search</h1>

      <form onSubmit={handleSearch}>
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Search knowledge base..."
          style={{
            width: '100%',
            padding: '12px',
            fontSize: '16px',
            marginBottom: '10px'
          }}
        />
        <button type="submit" disabled={loading}>
          {loading ? 'Searching...' : 'Search'}
        </button>
      </form>

      <div style={{ marginTop: '20px' }}>
        {results.map((result, idx) => (
          <div key={idx} style={{
            border: '1px solid #ccc',
            padding: '15px',
            marginBottom: '10px',
            borderRadius: '5px'
          }}>
            <div style={{ fontWeight: 'bold', marginBottom: '5px' }}>
              Score: {(result.score * 100).toFixed(1)}%
            </div>
            <div>{result.content}</div>
            {result.metadata && (
              <div style={{ marginTop: '5px', fontSize: '12px', color: '#666' }}>
                {JSON.stringify(result.metadata)}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

export default App;
```

### Step 3: Add Offline Support (30 min)

```javascript
// src/db.js - IndexedDB wrapper
import { openDB } from 'idb';

const DB_NAME = 'knowledge-base';
const DB_VERSION = 1;

export async function initDB() {
  return openDB(DB_NAME, DB_VERSION, {
    upgrade(db) {
      if (!db.objectStoreNames.contains('documents')) {
        db.createObjectStore('documents', { keyPath: 'id' });
      }
      if (!db.objectStoreNames.contains('searches')) {
        db.createObjectStore('searches', { keyPath: 'query' });
      }
    },
  });
}

export async function cacheSearchResults(query, results) {
  const db = await initDB();
  await db.put('searches', {
    query,
    results,
    timestamp: Date.now()
  });
}

export async function getCachedSearch(query) {
  const db = await initDB();
  const cached = await db.get('searches', query);

  // Cache valid for 1 hour
  if (cached && (Date.now() - cached.timestamp < 3600000)) {
    return cached.results;
  }
  return null;
}
```

```javascript
// Update App.js to use cache
import { cacheSearchResults, getCachedSearch } from './db';

const handleSearch = async (e) => {
  e.preventDefault();
  setLoading(true);

  try {
    // Check cache first
    const cached = await getCachedSearch(query);
    if (cached) {
      setResults(cached);
      setLoading(false);
      return;
    }

    // Fetch from API
    const response = await axios.post(`${API_URL}/api/search`, {
      query,
      limit: 10
    });

    // Cache results
    await cacheSearchResults(query, response.data.results);
    setResults(response.data.results);
  } catch (error) {
    console.error('Search failed:', error);
  } finally {
    setLoading(false);
  }
};
```

### Step 4: Add Real-Time Sync (Yjs) (60 min)

```bash
# Install Yjs dependencies
npm install yjs y-websocket y-indexeddb
```

```javascript
// src/sync.js
import * as Y from 'yjs';
import { WebsocketProvider } from 'y-websocket';
import { IndexeddbPersistence } from 'y-indexeddb';

export function createSyncedDocument(docName) {
  // Create Yjs document
  const ydoc = new Y.Doc();

  // IndexedDB persistence (offline-first)
  const indexeddbProvider = new IndexeddbPersistence(docName, ydoc);

  // WebSocket provider (real-time sync)
  const websocketProvider = new WebsocketProvider(
    'ws://localhost:1234', // Yjs WebSocket server
    docName,
    ydoc
  );

  websocketProvider.on('status', event => {
    console.log('Sync status:', event.status); // connected/disconnected
  });

  return { ydoc, indexeddbProvider, websocketProvider };
}

// Usage example
const { ydoc } = createSyncedDocument('my-knowledge-base');
const ytext = ydoc.getText('content');

// Listen for changes
ytext.observe(event => {
  console.log('Document updated:', ytext.toString());
});

// Make changes
ytext.insert(0, 'Hello from Yjs!');
```

### Step 5: Set Up Yjs WebSocket Server (15 min)

```bash
# Create new directory
mkdir yjs-server && cd yjs-server
npm init -y
npm install ws y-websocket

# Create server.js
cat > server.js <<'EOF'
const WebSocket = require('ws');
const http = require('http');
const { setupWSConnection } = require('y-websocket/bin/utils');

const server = http.createServer((request, response) => {
  response.writeHead(200, { 'Content-Type': 'text/plain' });
  response.end('Yjs WebSocket Server');
});

const wss = new WebSocket.Server({ server });

wss.on('connection', (ws, req) => {
  setupWSConnection(ws, req);
});

server.listen(1234, () => {
  console.log('Yjs WebSocket server running on ws://localhost:1234');
});
EOF

# Run server
node server.js
```

---

## Testing the Complete System

### Terminal 1: Qdrant
```bash
docker-compose up
```

### Terminal 2: FastAPI Backend
```bash
cd kb-backend
source venv/bin/activate
uvicorn main:app --reload
```

### Terminal 3: Yjs Server
```bash
cd yjs-server
node server.js
```

### Terminal 4: React Frontend
```bash
cd kb-frontend
npm start
```

### Test Flow

1. **Add documents** (via API or UI)
2. **Search semantically** (should find relevant results)
3. **Go offline** (disable network)
4. **Search again** (should return cached results)
5. **Open multiple browser tabs** (Yjs real-time sync)
6. **Edit in one tab** (changes appear in other tabs)

---

## Migration Script for Existing Files

```python
#!/usr/bin/env python3
# migrate_kb.py - Migrate existing knowledge base to cloud

import os
import sys
from pathlib import Path
import requests
import frontmatter
from openai import OpenAI

# Config
API_URL = "http://localhost:8000"
KB_PATH = "/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase"

def migrate_file(file_path):
    """Migrate a single markdown file"""

    with open(file_path, 'r', encoding='utf-8') as f:
        post = frontmatter.load(f)

    # Prepare document
    doc = {
        "content": post.content,
        "metadata": {
            **post.metadata,
            "source_path": str(file_path),
            "filename": file_path.name
        }
    }

    # Send to API
    response = requests.post(f"{API_URL}/api/documents", json=doc)

    if response.status_code == 200:
        print(f"✅ Migrated: {file_path.name}")
        return True
    else:
        print(f"❌ Failed: {file_path.name} - {response.text}")
        return False

def main():
    kb_path = Path(KB_PATH)
    md_files = list(kb_path.rglob("*.md"))

    print(f"Found {len(md_files)} markdown files")
    print("Starting migration...\n")

    success = 0
    failed = 0

    for md_file in md_files:
        if migrate_file(md_file):
            success += 1
        else:
            failed += 1

    print(f"\n✅ Success: {success}")
    print(f"❌ Failed: {failed}")

if __name__ == "__main__":
    main()
```

```bash
# Run migration
python migrate_kb.py
```

---

## Production Deployment Checklist

### Before Going Live

- [ ] Set up proper authentication (Auth0/Cognito)
- [ ] Configure CORS properly (whitelist production domains)
- [ ] Enable HTTPS (SSL/TLS certificates)
- [ ] Set up CDN (Cloudflare)
- [ ] Configure rate limiting (10 req/s per user)
- [ ] Enable monitoring (Prometheus/Grafana)
- [ ] Set up automated backups (daily to S3 Glacier)
- [ ] Configure logging (structured JSON logs)
- [ ] Add error tracking (Sentry)
- [ ] Load testing (k6 or Locust)
- [ ] Security audit (OWASP checklist)
- [ ] Document API (OpenAPI/Swagger)

### Environment Variables

```bash
# .env.production
OPENAI_API_KEY=sk-...
QDRANT_URL=https://qdrant.example.com
QDRANT_API_KEY=...
R2_ACCOUNT_ID=...
R2_ACCESS_KEY_ID=...
R2_SECRET_ACCESS_KEY=...
AUTH0_DOMAIN=...
AUTH0_CLIENT_ID=...
AUTH0_CLIENT_SECRET=...
FRONTEND_URL=https://kb.example.com
ALLOWED_ORIGINS=https://kb.example.com,https://api.example.com
```

---

## Troubleshooting

### Qdrant Connection Issues
```bash
# Check if Qdrant is running
curl http://localhost:6333/

# Check logs
docker logs <container-id>

# Restart Qdrant
docker-compose restart qdrant
```

### Slow Search Performance
```bash
# Check collection info
curl http://localhost:6333/collections/knowledge_base

# Optimize index (if needed)
curl -X POST http://localhost:6333/collections/knowledge_base/index
```

### Yjs Sync Not Working
```bash
# Check WebSocket server
curl http://localhost:1234/

# Check browser console for errors
# Look for: "WebSocket connection failed"

# Check CORS settings in server.js
```

### High OpenAI Costs
```python
# Option 1: Use smaller model
model="text-embedding-3-small"  # $0.02/1M tokens vs $0.13/1M

# Option 2: Cache embeddings
# Option 3: Batch requests (max 100 inputs per request)
```

---

## Next Steps

1. **Day 3-4**: Add document upload UI
2. **Day 5-6**: Implement user authentication
3. **Day 7-8**: Build mobile app (React Native)
4. **Week 2**: Add Unity WebGL client
5. **Week 3**: Deploy to production
6. **Week 4**: Monitoring & optimization

---

**Quick Start Version**: 1.0
**Created**: January 7, 2026
**Estimated Time**: 1-2 days for working prototype
