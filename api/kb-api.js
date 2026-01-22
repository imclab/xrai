#!/usr/bin/env node
/**
 * Unity XR Knowledgebase REST API
 *
 * Lightweight HTTP server for KB access from any tool/platform.
 *
 * Usage:
 *   node kb-api.js [port]        # Start server (default port 3847)
 *
 * Endpoints:
 *   GET /api/search?q=query      # Search KB
 *   GET /api/files               # List KB files
 *   GET /api/file/:name          # Get specific file
 *   GET /api/patterns            # Get auto-fix patterns
 *   GET /api/stats               # Get KB stats
 *   GET /api/health              # Health check
 */

const http = require('http');
const fs = require('fs');
const path = require('path');
const url = require('url');

const PORT = process.argv[2] || 3847;
const KB_PATH = path.join(__dirname, '..', 'KnowledgeBase');

// Simple in-memory cache
const cache = {
  files: null,
  patterns: null,
  lastUpdate: 0
};

// Load KB file list
function getFiles() {
  if (cache.files && Date.now() - cache.lastUpdate < 60000) {
    return cache.files;
  }

  try {
    const files = fs.readdirSync(KB_PATH)
      .filter(f => f.endsWith('.md'))
      .map(f => ({
        name: f,
        path: `/api/file/${encodeURIComponent(f)}`,
        size: fs.statSync(path.join(KB_PATH, f)).size
      }));

    cache.files = files;
    cache.lastUpdate = Date.now();
    return files;
  } catch (e) {
    return { error: e.message };
  }
}

// Read a KB file
function getFile(name) {
  try {
    const filePath = path.join(KB_PATH, name);
    if (!filePath.startsWith(KB_PATH)) {
      return { error: 'Invalid path' };
    }
    const content = fs.readFileSync(filePath, 'utf8');
    return { name, content };
  } catch (e) {
    return { error: e.message };
  }
}

// Simple search (grep-like)
function search(query, limit = 20) {
  const files = getFiles();
  if (files.error) return files;

  const results = [];
  const queryLower = query.toLowerCase();

  for (const file of files) {
    try {
      const content = fs.readFileSync(path.join(KB_PATH, file.name), 'utf8');
      const lines = content.split('\n');
      const matches = [];

      for (let i = 0; i < lines.length; i++) {
        if (lines[i].toLowerCase().includes(queryLower)) {
          matches.push({
            line: i + 1,
            text: lines[i].substring(0, 200)
          });
          if (matches.length >= 3) break; // Max 3 matches per file
        }
      }

      if (matches.length > 0) {
        results.push({
          file: file.name,
          matches,
          score: matches.length
        });
      }

      if (results.length >= limit) break;
    } catch (e) {
      // Skip unreadable files
    }
  }

  // Sort by score
  results.sort((a, b) => b.score - a.score);

  return {
    query,
    count: results.length,
    results: results.slice(0, limit)
  };
}

// Get auto-fix patterns
function getPatterns() {
  const file = getFile('_AUTO_FIX_PATTERNS.md');
  if (file.error) return file;

  // Extract pattern count
  const match = file.content.match(/\*\*Patterns\*\*:\s*(\d+)/);
  const count = match ? parseInt(match[1]) : 'unknown';

  return {
    file: '_AUTO_FIX_PATTERNS.md',
    count,
    preview: file.content.substring(0, 2000) + '...'
  };
}

// Get stats
function getStats() {
  const files = getFiles();
  if (files.error) return files;

  const totalSize = files.reduce((sum, f) => sum + f.size, 0);

  return {
    kbPath: KB_PATH,
    fileCount: files.length,
    totalSizeKB: Math.round(totalSize / 1024),
    categories: {
      patterns: files.filter(f => f.name.includes('PATTERN')).length,
      intelligence: files.filter(f => f.name.includes('INTELLIGENCE') || f.name.includes('LEARNING')).length,
      vfx: files.filter(f => f.name.includes('VFX')).length,
      ar: files.filter(f => f.name.includes('AR') || f.name.includes('FOUNDATION')).length
    },
    endpoints: [
      'GET /api/search?q=query',
      'GET /api/files',
      'GET /api/file/:name',
      'GET /api/patterns',
      'GET /api/stats',
      'GET /api/health'
    ]
  };
}

// HTTP server
const server = http.createServer((req, res) => {
  const parsedUrl = url.parse(req.url, true);
  const pathname = parsedUrl.pathname;

  // CORS headers
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Content-Type', 'application/json');

  let result;

  if (pathname === '/api/health') {
    result = { status: 'ok', timestamp: new Date().toISOString() };
  }
  else if (pathname === '/api/stats') {
    result = getStats();
  }
  else if (pathname === '/api/files') {
    result = getFiles();
  }
  else if (pathname === '/api/patterns') {
    result = getPatterns();
  }
  else if (pathname === '/api/search') {
    const query = parsedUrl.query.q;
    const limit = parseInt(parsedUrl.query.limit) || 20;
    if (!query) {
      result = { error: 'Missing query parameter ?q=' };
    } else {
      result = search(query, limit);
    }
  }
  else if (pathname.startsWith('/api/file/')) {
    const name = decodeURIComponent(pathname.replace('/api/file/', ''));
    result = getFile(name);
  }
  else {
    result = {
      name: 'Unity XR Knowledgebase API',
      version: '1.0.0',
      docs: 'https://github.com/imclab/Unity-XR-AI#api',
      endpoints: getStats().endpoints
    };
  }

  res.statusCode = result.error ? 400 : 200;
  res.end(JSON.stringify(result, null, 2));
});

server.listen(PORT, () => {
  console.log(`Unity XR KB API running at http://localhost:${PORT}`);
  console.log(`KB Path: ${KB_PATH}`);
  console.log(`\nEndpoints:`);
  console.log(`  GET /api/search?q=query  - Search KB`);
  console.log(`  GET /api/files           - List files`);
  console.log(`  GET /api/file/:name      - Get file`);
  console.log(`  GET /api/patterns        - Auto-fix patterns`);
  console.log(`  GET /api/stats           - Statistics`);
  console.log(`  GET /api/health          - Health check`);
});
