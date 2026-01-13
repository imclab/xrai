# API Setup Guide for Cosmos Visualizer

The Cosmos Visualizer can integrate with multiple APIs to fetch real 3D models, code repositories, and other media. Here's how to configure each data source:

## 1. Objaverse (Local Index) - ✅ Already Working

The system includes a local index of 200,000+ 3D models from Objaverse. This works out of the box:

- **File**: `public/objaverse_gltf_index_lite.json` (26MB)
- **Format**: JSON array with model URLs and metadata
- **No API key required**

## 2. Casa / Poly.pizza API

To enable the Casa/Poly.pizza integration:

1. Visit https://poly.pizza/developers to get an API key
2. Update `src/data/DataManager.js` line 221:
   ```javascript
   'Authorization': 'Bearer YOUR_CASA_API_KEY'
   ```

## 3. Polli Database API

To enable Polli integration:

1. Contact Polli for API access
2. Update `src/data/DataManager.js` line 268:
   ```javascript
   'Authorization': 'Bearer YOUR_POLLI_API_KEY'
   ```

## 4. Sketchfab API (Optional)

The Sketchfab API works without authentication for basic searches, but you can add an API key for higher limits:

1. Get API token from https://sketchfab.com/settings/developer
2. Add to line 314 in `src/data/DataManager.js`:
   ```javascript
   'Authorization': 'Token YOUR_SKETCHFAB_TOKEN'
   ```

## 5. GitHub API (Rate Limited)

GitHub API works without authentication but has strict rate limits (60 requests/hour). For higher limits:

1. Create a personal access token at https://github.com/settings/tokens
2. Update line 560 in `src/data/DataManager.js`:
   ```javascript
   'Authorization': 'token YOUR_GITHUB_TOKEN'
   ```

## Testing Your Configuration

1. **Basic Test** (uses local Objaverse data):
   ```
   Search for: "chair"
   Sources: Check "Icosa Gallery"
   ```

2. **Stress Test** (loads many models):
   - Click "10K Nodes" button - loads 10,000 models
   - Click "100K Nodes" button - loads 100,000 models
   - Click "1M Nodes" button - loads 1,000,000 models (requires good hardware)

3. **GitHub Test**:
   ```
   Search for: "three.js"
   Sources: Check "GitHub"
   ```

## Performance Tips

- The visualizer is optimized for 1M+ nodes
- Use Chrome/Edge for best WebGL performance
- Close other browser tabs when testing large datasets
- The system uses instanced rendering and LOD for efficiency

## Troubleshooting

**"No results found"**
- Check browser console for API errors
- Verify API keys are correctly set
- Ensure you're searching with the right sources checked

**Performance issues**
- Start with smaller searches (100-1000 nodes)
- Use "Force Graph" layout for large datasets
- Disable shadows in Three.js if needed

**CORS errors**
- Some APIs may require proxy setup
- Check if API supports browser requests
- Consider running a local proxy server