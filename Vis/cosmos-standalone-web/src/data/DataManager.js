import axios from 'axios';
import pako from 'pako';
import { IcosaConfig, getIcosaEndpoint, buildIcosaSearchUrl } from '../config/icosa-config.js';
import { ObjaverseConfig, checkObjaverseIndex } from '../config/objaverse-config.js';

export class DataManager {
    constructor() {
        this.cache = new Map();
        this.searchProviders = {
            icosa: new IcosaProvider(),
            objaverse: new ObjaverseProvider(),
            github: new GitHubProvider(),
            web: new WebProvider(),
            local: new LocalProvider()
        };
    }
    
    async search(query, sources = ['all']) {
        const results = [];
        const activeSources = sources.includes('all') 
            ? Object.keys(this.searchProviders)
            : sources;
        
        // Search all sources in parallel
        const promises = activeSources.map(source => {
            return this.searchProviders[source]
                ?.search(query)
                .catch(err => {
                    console.error(`Search error in ${source}:`, err);
                    return [];
                });
        });
        
        const sourceResults = await Promise.all(promises);
        sourceResults.forEach(items => {
            if (Array.isArray(items)) {
                results.push(...items);
            }
        });
        
        return this.rankResults(results, query);
    }
    
    rankResults(results, query, maxResults = 1000000) {
        // Optimized ranking for massive datasets
        console.log(`Ranking ${results.length} results, max: ${maxResults}`);
        
        return results
            .map(result => ({
                ...result,
                relevance: this.calculateRelevance(query, result)
            }))
            .sort((a, b) => (b.relevance || 0) - (a.relevance || 0))
            .slice(0, maxResults); // Support up to 1M results
    }
    
    calculateRelevance(query, result) {
        const queryLower = query.toLowerCase();
        const text = `${result.name || ''} ${result.description || ''}`.toLowerCase();
        
        let score = 0;
        if (text.includes(queryLower)) score += 0.5;
        
        const words = queryLower.split(/\s+/);
        words.forEach(word => {
            if (text.includes(word)) {
                score += 0.3 / words.length;
            }
        });
        
        return Math.min(score, 1);
    }
    
    convertToGraphData(items) {
        const nodes = [];
        const links = [];
        const nodeMap = new Map();
        
        // Create nodes
        items.forEach((item, index) => {
            const node = {
                id: item.id || `node-${index}`,
                name: item.name || item.title || 'Untitled',
                type: item.source || 'unknown',
                val: item.relevance || 1,
                ...item
            };
            
            nodes.push(node);
            nodeMap.set(node.id, node);
        });
        
        // Create links based on relationships or similarity
        items.forEach(item => {
            if (item.relationships) {
                item.relationships.forEach(rel => {
                    if (nodeMap.has(rel.target)) {
                        links.push({
                            source: item.id,
                            target: rel.target,
                            value: rel.strength || 1
                        });
                    }
                });
            }
        });
        
        // Add similarity links if no explicit relationships
        if (links.length === 0 && nodes.length > 1) {
            // Create some connections based on type
            const typeGroups = new Map();
            nodes.forEach(node => {
                if (!typeGroups.has(node.type)) {
                    typeGroups.set(node.type, []);
                }
                typeGroups.get(node.type).push(node);
            });
            
            // Connect nodes within same type
            typeGroups.forEach(group => {
                for (let i = 0; i < group.length - 1; i++) {
                    links.push({
                        source: group[i].id,
                        target: group[i + 1].id,
                        value: 0.5
                    });
                }
            });
        }
        
        return { nodes, links };
    }
}

// Search Providers
class IcosaProvider {
    constructor() {
        // Icosa Gallery API endpoints (based on icosa-foundation/icosa-gallery)
        // Default to poly.pizza public instance, but can be changed to self-hosted
        this.icosaApiEndpoint = 'https://poly.pizza/api'; // Public Icosa Gallery instance
        this.icosaLegacyEndpoint = 'https://poly.googleapis.com/v1'; // Legacy Poly API format
        
        // Alternative endpoints
        this.casaApiEndpoint = 'https://poly.pizza/api/v1'; // Casa variant
        this.sketchfabApiEndpoint = 'https://api.sketchfab.com/v3'; // Sketchfab as alternative
        
        this.pageSize = 24; // Icosa Gallery default page size
        this.maxPages = 10; // Limit pages to avoid overwhelming the API
    }
    
    async search(query, limit = 100000) {
        try {
            console.log(`Searching Icosa Gallery for: "${query}" (limit: ${limit})`);
            const allResults = [];
            
            // Execute API searches in parallel for speed
            const apiPromises = [];
            
            // 1. Try Icosa Gallery API (poly.pizza or self-hosted)
            apiPromises.push(
                this.searchIcosaGallery(query, limit)
                    .catch(err => {
                        console.log('Icosa Gallery API error:', err.message);
                        return [];
                    })
            );
            
            // 2. Try legacy/sample Casa data as fallback
            apiPromises.push(
                this.searchCasaAPI(query, limit)
                    .catch(err => {
                        console.log('Casa fallback error:', err.message);
                        return [];
                    })
            );
            
            // 3. Try Sketchfab as alternative
            apiPromises.push(
                this.searchSketchfabAPI(query, Math.min(limit, 1000))
                    .catch(err => {
                        console.log('Sketchfab API error:', err.message);
                        return [];
                    })
            );
            
            // 4. Load from local Objaverse index for massive datasets
            apiPromises.push(
                this.loadObjaverseIndex(query, limit)
                    .catch(err => {
                        console.log('Objaverse index error:', err.message);
                        return [];
                    })
            );
            
            // Wait for all API calls to complete
            const results = await Promise.all(apiPromises);
            results.forEach(apiResults => allResults.push(...apiResults));
            
            console.log(`Total results found: ${allResults.length}`);
            
            // Convert to optimized graph nodes for 1M+ visualization
            return this.createOptimizedNodes(allResults.slice(0, limit));
        } catch (error) {
            console.error('Icosa search error:', error);
            return [];
        }
    }
    
    async searchCasaAPI(query, limit) {
        // Casa/Poly.pizza API integration
        console.log(`Searching Casa Gallery for: "${query}"`);
        
        // For now, we'll use sample Casa-style data to demonstrate the integration
        // When real API access is available, replace this with actual API calls
        const casaSampleData = [
            {
                id: 'casa-sculpture-001',
                name: 'Digital Sculpture #1',
                artist: 'CasaArtist',
                description: 'Interactive digital sculpture from Casa Gallery',
                modelUrl: 'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/MetalRoughSpheres/glTF-Binary/MetalRoughSpheres.glb',
                thumbnail: 'https://example.com/thumb1.jpg',
                tags: ['sculpture', 'digital', 'interactive'],
                polyCount: 15000,
                license: 'CC BY 4.0'
            },
            {
                id: 'casa-vr-art-002',
                name: 'VR Art Experience',
                artist: 'VRArtist',
                description: 'Immersive VR artwork featured in Casa Gallery',
                modelUrl: 'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/MaterialsVariantsShoe/glTF-Binary/MaterialsVariantsShoe.glb',
                tags: ['vr', 'immersive', 'experience'],
                polyCount: 25000
            },
            {
                id: 'casa-generative-003',
                name: 'Generative Form',
                artist: 'GenArtist',
                description: 'Procedurally generated artwork from algorithms',
                modelUrl: 'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/ToyCar/glTF-Binary/ToyCar.glb',
                tags: ['generative', 'algorithmic', 'procedural'],
                polyCount: 8000
            },
            {
                id: 'casa-abstract-004',
                name: 'Abstract Composition',
                artist: 'AbstractMaster',
                description: 'Abstract 3D composition exploring form and color',
                modelUrl: 'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/Suzanne/glTF-Binary/Suzanne.glb',
                tags: ['abstract', 'composition', 'form'],
                polyCount: 12000
            },
            {
                id: 'casa-kinetic-005',
                name: 'Kinetic Structure',
                artist: 'KineticArt',
                description: 'Dynamic moving sculpture with animated parts',
                modelUrl: 'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/AnimatedMorphCube/glTF-Binary/AnimatedMorphCube.glb',
                tags: ['kinetic', 'animation', 'dynamic'],
                polyCount: 5000
            }
        ];
        
        // Filter based on query
        const queryLower = query.toLowerCase();
        const filtered = casaSampleData.filter(item => {
            if (!query || query === '') return true;
            const searchText = `${item.name} ${item.artist} ${item.description} ${(item.tags || []).join(' ')}`.toLowerCase();
            return searchText.includes(queryLower);
        });
        
        // Transform to our node format
        return filtered.slice(0, Math.min(limit, filtered.length)).map(item => ({
            id: item.id,
            name: item.name,
            artist: item.artist,
            description: item.description,
            modelUrl: item.modelUrl,
            thumbnail: item.thumbnail,
            tags: item.tags,
            polyCount: item.polyCount,
            license: item.license
        }));
        
        // When API key is available, use this code instead:
        /*
        const results = [];
        const pagesNeeded = Math.ceil(limit / this.pageSize);
        
        for (let page = 1; page <= Math.min(pagesNeeded, 10); page++) {
            try {
                const response = await fetch(
                    `${this.casaApiEndpoint}/assets?q=${encodeURIComponent(query)}&page=${page}&per_page=${this.pageSize}`,
                    {
                        headers: {
                            'Accept': 'application/json',
                            'Authorization': 'Bearer YOUR_CASA_API_KEY'
                        }
                    }
                );
                
                if (response.ok) {
                    const data = await response.json();
                    const assets = data.assets || data.results || [];
                    
                    assets.forEach(asset => {
                        results.push({
                            id: `casa-${asset.id || asset.uid}`,
                            name: asset.displayName || asset.name || asset.title,
                            artist: asset.authorName || asset.creator || 'Unknown',
                            description: asset.description || '',
                            modelUrl: this.extractModelUrl(asset),
                            thumbnail: asset.thumbnail?.url || asset.preview_url,
                            license: asset.license || 'CC BY 4.0',
                            polyCount: asset.metadata?.triangleCount || asset.polyCount,
                            tags: asset.tags || []
                        });
                    });
                }
            } catch (err) {
                // Continue with partial results
            }
        }
        
        return results;
        */
    }
    
    async searchIcosaGallery(query, limit) {
        console.log(`Searching Icosa Gallery API for: "${query}"`);
        const results = [];
        
        try {
            // Import auth configuration
            const { getAuthHeader } = await import('../config/auth-config.js');
            
            const officialEndpoint = 'https://api.icosa.gallery/v1';
            const headers = {
                'Accept': 'application/json',
                ...getAuthHeader('icosa')
            };
            
            let pageToken = '';
            let pagesLoaded = 0;
            let officialSucceeded = false;
            
            while (pagesLoaded < this.maxPages && results.length < limit) {
                const params = new URLSearchParams({
                    pageSize: this.pageSize.toString(),
                    orderBy: 'LIKED'
                });
                
                if (query) {
                    params.append('q', query);
                    params.append('format', 'GLTF,GLTF2');
                } else {
                    // Use curated category when no query is supplied to guarantee results
                    params.append('category', 'ANIMALS');
                    params.append('format', 'GLTF,GLTF2');
                }
                
                if (pageToken) {
                    params.append('pageToken', pageToken);
                }
                
                const response = await fetch(`${officialEndpoint}/assets?${params}`, { headers });
                
                if (!response.ok) {
                    if (response.status === 401 || response.status === 403) {
                        console.warn('Icosa Gallery API requires authentication. Falling back to public endpoints.');
                    } else {
                        console.warn(`Icosa Gallery API returned ${response.status}. Falling back to public endpoints.`);
                    }
                    break;
                }
                
                officialSucceeded = true;
                const data = await response.json();
                const assets = data.assets || [];
                
                assets.forEach(asset => {
                    if (results.length >= limit) return;
                    
                    const { modelUrl, format } = this.extractIcosaModelUrl(asset);
                    
                    results.push({
                        id: `icosa-official-${asset.assetId}`,
                        name: asset.displayName || asset.name || 'Untitled',
                        artist: asset.authorName || 'Unknown Artist',
                        description: asset.description || '',
                        modelUrl,
                        format,
                        thumbnail: asset.thumbnail?.url,
                        license: asset.license || 'Unknown',
                        tags: asset.tags || [],
                        triangleCount: asset.triangleCount,
                        liked: asset.liked,
                        likeCount: asset.likeCount || 0,
                        source: 'icosa-official'
                    });
                });
                
                if (!data.nextPageToken || results.length >= limit) {
                    break;
                }
                
                pageToken = data.nextPageToken;
                pagesLoaded++;
            }
            
            if (officialSucceeded) {
                console.log(`Found ${results.length} assets from Icosa Gallery official API`);
                return results.slice(0, limit);
            } else {
                // Fall back to public endpoints (poly.pizza, legacy Poly)
                console.log('Using public Icosa Gallery endpoints (no auth)');
                
                let pageToken = '';
                let pagesLoaded = 0;
                
                while (pagesLoaded < this.maxPages && results.length < limit) {
                    const params = new URLSearchParams({
                        keywords: query || '',
                        format: 'GLTF2',
                        pageSize: this.pageSize,
                        orderBy: 'BEST'
                    });
                    
                    if (pageToken) {
                        params.append('pageToken', pageToken);
                    }
                    
                    // Try public endpoints
                    let response;
                    try {
                        // First try poly.pizza public instance
                        response = await fetch(`${this.icosaApiEndpoint}/assets?${params}`, {
                            headers: {
                                'Accept': 'application/json',
                                'User-Agent': 'CosmosVisualizer/1.0'
                            }
                        });
                    } catch (err) {
                        // Fallback to legacy Poly API format
                        console.log('Trying legacy Poly API format...');
                        response = await fetch(`${this.icosaLegacyEndpoint}/assets?key=AIzaSyBHZM8O3GiHKejLw3sb4yPpWcA5FRLQxVk&${params}`);
                    }
                    
                    if (response && response.ok) {
                        const data = await response.json();
                        const assets = data.assets || [];
                        
                        assets.forEach(asset => {
                            // Find the best format (prefer GLB)
                            let modelUrl = null;
                            let format = 'gltf';
                            
                            if (asset.formats) {
                                const glbFormat = asset.formats.find(f => 
                                    f.formatType === 'GLTF2' && f.resources?.find(r => r.url?.endsWith('.glb'))
                                );
                                const gltfFormat = asset.formats.find(f => f.formatType === 'GLTF2');
                                
                                if (glbFormat) {
                                    const glbResource = glbFormat.resources.find(r => r.url?.endsWith('.glb'));
                                    modelUrl = glbResource?.url;
                                    format = 'glb';
                                } else if (gltfFormat && gltfFormat.root) {
                                    modelUrl = gltfFormat.root.url;
                                    format = 'gltf';
                                }
                            }
                            
                            if (modelUrl || asset.displayName) {
                                results.push({
                                    id: `icosa-${asset.name?.split('/').pop() || results.length}`,
                                    name: asset.displayName || 'Untitled',
                                    artist: asset.authorName || 'Unknown Artist',
                                    description: asset.description || '',
                                    modelUrl: modelUrl,
                                    format: format,
                                    thumbnail: asset.thumbnail?.url,
                                    license: asset.license || 'Unknown',
                                    tags: asset.tags || [],
                                    createTime: asset.createTime,
                                    updateTime: asset.updateTime,
                                    likes: asset.likes || 0,
                                    source: 'icosa-public'
                                });
                            }
                        });
                        
                        pageToken = data.nextPageToken;
                        if (!pageToken) break;
                        pagesLoaded++;
                    } else {
                        console.log('Public Icosa Gallery API returned no results');
                        break;
                    }
                }
                
                console.log(`Found ${results.length} assets from public Icosa Gallery`);
            }
            
        } catch (error) {
            console.error('Icosa Gallery search error:', error);
        }
        
        return results;
    }
    
    extractIcosaModelUrl(asset) {
        let modelUrl = null;
        let format = 'gltf';
        
        // Official API may return formats as array (Poly format) or keyed object
        const formats = Array.isArray(asset.formats)
            ? asset.formats
            : Object.values(asset.formats || {});
        
        if (formats) {
            const glbFormat = formats.find(f => {
                if (f.formatType === 'GLTF2') {
                    if (f.root?.url?.toLowerCase().endsWith('.glb')) return true;
                    return f.resources?.some(r => r.url?.toLowerCase().endsWith('.glb'));
                }
                return false;
            });
            
            const gltfFormat = formats.find(f => f.formatType === 'GLTF2');
            
            if (glbFormat) {
                modelUrl = glbFormat.root?.url;
                if (!modelUrl && glbFormat.resources) {
                    const resource = glbFormat.resources.find(r => r.url?.toLowerCase().endsWith('.glb'));
                    modelUrl = resource?.url;
                }
                format = 'glb';
            } else if (gltfFormat) {
                modelUrl = gltfFormat.root?.url;
                format = 'gltf';
            }
        }
        
        return { modelUrl, format };
    }
    
    async searchPolliAPI(query, limit) {
        // Polli database API integration
        // Note: Polli API endpoint needs to be verified and authenticated
        console.log('Polli API endpoint not available - skipping for now');
        return [];
        
        // When API is confirmed available, uncomment and update:
        /*
        const results = [];
        
        try {
            const response = await fetch(`${this.polliApiEndpoint}/search`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': 'Bearer YOUR_POLLI_API_KEY'
                },
                body: JSON.stringify({
                    query: query,
                    limit: Math.min(limit, 10000),
                    filters: {
                        formats: ['glb', 'gltf', 'obj', 'fbx'],
                        hasAnimation: false // For performance
                    }
                })
            });
            
            if (response.ok) {
                const data = await response.json();
                const models = data.models || data.results || [];
                
                models.forEach(model => {
                    results.push({
                        id: `polli-${model.id || model.uid}`,
                        name: model.name || model.title,
                        artist: model.creator || model.author || 'Unknown',
                        description: model.description || '',
                        modelUrl: model.download_url || model.model_url,
                        thumbnail: model.thumbnail_url || model.preview,
                        polyCount: model.poly_count || model.vertices,
                        tags: model.tags || model.categories || []
                    });
                });
            }
        } catch (err) {
            // Continue without Polli results
        }
        
        return results;
        */
    }
    
    async searchSketchfabAPI(query, limit) {
        // Sketchfab as alternative 3D asset source
        const results = [];
        
        try {
            const response = await fetch(
                `${this.sketchfabApiEndpoint}/search?q=${encodeURIComponent(query)}&type=models&downloadable=true&count=${Math.min(limit, 100)}`,
                {
                    headers: {
                        'Accept': 'application/json'
                    }
                }
            );
            
            if (response.ok) {
                const data = await response.json();
                const models = data.results || [];
                
                models.forEach(model => {
                    results.push({
                        id: `sketchfab-${model.uid}`,
                        name: model.name,
                        artist: model.user?.displayName || 'Unknown',
                        description: model.description || '',
                        modelUrl: model.download?.glb?.url || model.viewerUrl,
                        thumbnail: model.thumbnails?.images?.[0]?.url,
                        polyCount: model.vertexCount,
                        tags: model.tags?.map(t => t.name) || []
                    });
                });
            }
        } catch (err) {
            // Continue without Sketchfab results
        }
        
        return results;
    }
    
    async loadObjaverseIndex(query, limit) {
        // Load from massive local Objaverse dataset for 1M+ results
        const results = [];
        
        try {
            // Check if we have the local index file
            const response = await fetch('/objaverse_gltf_index_lite.json');
            if (response.ok) {
                const index = await response.json();
                console.log(`Loading from Objaverse index: ${index.length} total models`);
                
                // Efficient filtering for large datasets
                const queryLower = query.toLowerCase();
                let count = 0;
                
                for (let i = 0; i < index.length && count < limit; i++) {
                    const item = index[i];
                    const searchText = `${item.n || ''} ${(item.g || []).join(' ')}`.toLowerCase();
                    
                    // Match query or return all if query is empty
                    if (queryLower === '' || searchText.includes(queryLower)) {
                        results.push({
                            id: `obja-${i}`,
                            name: item.n || `Model ${i + 1}`,
                            artist: item.s || 'Objaverse',
                            modelUrl: item.i,
                            tags: item.g || [],
                            description: `From ${item.s || 'Unknown source'}`
                        });
                        count++;
                    }
                }
                
                console.log(`Loaded ${count} models from Objaverse matching "${query}"`);
            }
        } catch (err) {
            console.log('Objaverse index not available');
        }
        
        return results;
    }
    
    extractModelUrl(asset) {
        // Extract model URL from various API response formats
        if (asset.formats) {
            const glbFormat = asset.formats.find(f => f.formatType === 'GLTF2' || f.format === 'glb');
            if (glbFormat) return glbFormat.root?.url || glbFormat.url;
            
            const gltfFormat = asset.formats.find(f => f.format === 'gltf');
            if (gltfFormat) return gltfFormat.url;
        }
        
        return asset.download_url || asset.model_url || asset.glb_url || asset.gltf_url || asset.url;
    }
    
    createOptimizedNodes(items) {
        // Optimized node creation for 1M+ items
        console.log(`Creating optimized graph nodes for ${items.length} items`);
        
        const nodes = [];
        const artistMap = new Map();
        const tagMap = new Map();
        
        // Process items in chunks for better memory management
        const chunkSize = 10000;
        for (let i = 0; i < items.length; i += chunkSize) {
            const chunk = items.slice(i, Math.min(i + chunkSize, items.length));
            
            chunk.forEach((item, idx) => {
                const globalIdx = i + idx;
                
                // Create lightweight artwork node
                const artworkNode = {
                    id: item.id || `icosa-${globalIdx}`,
                    source: 'icosa',
                    name: item.name || `Model ${globalIdx}`,
                    type: 'artwork',
                    modelUrl: item.modelUrl,
                    val: 1 + (item.polyCount ? Math.log10(item.polyCount + 1) / 6 : 0),
                    // Minimal data for memory efficiency
                    data: {
                        artist: item.artist,
                        thumb: item.thumbnail,
                        tags: item.tags?.slice(0, 3) // Limit tags
                    }
                };
                
                nodes.push(artworkNode);
                
                // Group by artist (limit to prevent too many artist nodes)
                if (item.artist && artistMap.size < 1000) {
                    if (!artistMap.has(item.artist)) {
                        artistMap.set(item.artist, {
                            id: `artist-${item.artist.replace(/[^a-zA-Z0-9]/g, '-')}`,
                            name: item.artist,
                            count: 0
                        });
                    }
                    artistMap.get(item.artist).count++;
                }
                
                // Track popular tags for clustering
                if (item.tags) {
                    item.tags.slice(0, 3).forEach(tag => {
                        if (!tagMap.has(tag)) {
                            tagMap.set(tag, 0);
                        }
                        tagMap.set(tag, tagMap.get(tag) + 1);
                    });
                }
            });
        }
        
        // Add top artists as nodes
        const topArtists = Array.from(artistMap.values())
            .sort((a, b) => b.count - a.count)
            .slice(0, 100);
        
        topArtists.forEach(artist => {
            nodes.push({
                id: artist.id,
                source: 'icosa',
                name: artist.name,
                type: 'artist',
                val: 2 + Math.log10(artist.count),
                data: { artworkCount: artist.count }
            });
        });
        
        console.log(`Created ${nodes.length} nodes (${items.length} artworks, ${topArtists.length} artists)`);
        console.log(`Top tags: ${Array.from(tagMap.entries()).sort((a, b) => b[1] - a[1]).slice(0, 10).map(t => t[0]).join(', ')}`);
        
        return nodes;
    }
    
    clear() {
        // Clear method for consistency with other providers
    }
}

class ObjaverseProvider {
    constructor() {
        this.hasLocalIndex = false;
        this.checkLocalIndex();
    }
    
    async checkLocalIndex() {
        this.hasLocalIndex = await checkObjaverseIndex();
    }
    
    async search(query, limit = 100000) {
        try {
            console.log(`Searching Objaverse (10M+ objects) for: "${query}"`);
            
            // First try local index if available
            const indexPath = ObjaverseConfig.endpoints.localIndexGLTF;
            
            try {
                const response = await fetch(indexPath);
                if (response.ok) {
                    const index = await response.json();
                    console.log(`Objaverse local index contains ${index.length} objects`);
                    
                    // Search through the index
                    // Structure: i: URL, n: name, s: source, g: array of filenames
                    const queryLower = query.toLowerCase();
                    const results = index.filter(item => {
                        if (!query || query === '') return true;
                        const searchText = `${item.n || ''} ${item.g?.join(' ') || ''} ${item.s || ''}`.toLowerCase();
                        return searchText.includes(queryLower);
                    }).slice(0, Math.min(limit, index.length));
                    
                    console.log(`Found ${results.length} Objaverse objects matching "${query}"`);
                    
                    return results.map((item, i) => ({
                        id: `objaverse-${i}`,
                        source: 'objaverse',
                        name: item.n || `Model ${i + 1}`,
                        description: `Objaverse-XL model from ${item.s || 'Unknown source'}`,
                        modelUrl: item.i, // The URL
                        format: item.n?.endsWith('.glb') ? 'glb' : 'gltf',
                        provider: item.s,
                        // Add metadata for visual distinction
                        val: 1 + Math.random() * 2,
                        type: '3d_object',
                        tags: item.g || [],
                        dataset: 'Objaverse-XL (10M+ objects)'
                    }));
                }
            } catch (err) {
                console.log('Objaverse local index not available, using demonstration data');
            }
            
            // Fallback to Objaverse demonstration data
            // These represent the types of objects in Objaverse-XL (10M+ dataset)
            const objaverseModels = [
                // Architecture & Buildings
                {
                    name: 'Modern Architecture',
                    uid: 'obja-arch-001',
                    glb_url: 'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/Sponza/glTF/Sponza.gltf',
                    source: 'GitHub',
                    category: 'architecture'
                },
                // Furniture
                {
                    name: 'Velvet Sofa',
                    uid: 'obja-furn-001',
                    glb_url: 'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/GlamVelvetSofa/glTF-Binary/GlamVelvetSofa.glb',
                    source: 'Thingiverse',
                    category: 'furniture'
                },
                // Characters & Creatures
                {
                    name: 'Fox Character',
                    uid: 'obja-char-001',
                    glb_url: 'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/Fox/glTF-Binary/Fox.glb',
                    source: 'Sketchfab',
                    category: 'character'
                },
                // Sci-Fi & Technology
                {
                    name: 'Sci-Fi Helmet',
                    uid: 'obja-tech-001', 
                    glb_url: 'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/SciFiHelmet/glTF-Binary/SciFiHelmet.glb',
                    source: 'GitHub',
                    category: 'technology'
                },
                // Consumer Products
                {
                    name: 'Boom Box',
                    uid: 'obja-prod-001',
                    glb_url: 'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/BoomBox/glTF-Binary/BoomBox.glb',
                    source: 'Thingiverse',
                    category: 'product'
                },
                // Vehicles
                {
                    name: 'Toy Car',
                    uid: 'obja-vehi-001',
                    glb_url: 'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/ToyCar/glTF-Binary/ToyCar.glb',
                    source: 'Polycam',
                    category: 'vehicle'
                },
                // Organic/Medical
                {
                    name: 'Brain Stem',
                    uid: 'obja-med-001',
                    glb_url: 'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/BrainStem/glTF-Binary/BrainStem.glb',
                    source: 'GitHub',
                    category: 'medical'
                },
                // Tools & Equipment
                {
                    name: 'Lantern',
                    uid: 'obja-tool-001',
                    glb_url: 'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/Lantern/glTF-Binary/Lantern.glb',
                    source: 'Thingiverse',
                    category: 'tool'
                },
                // Props & Accessories
                {
                    name: 'Water Bottle',
                    uid: 'obja-prop-001',
                    glb_url: 'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/WaterBottle/glTF-Binary/WaterBottle.glb',
                    source: 'Sketchfab',
                    category: 'prop'
                },
                // Abstract & Art
                {
                    name: 'Suzanne',
                    uid: 'obja-art-001',
                    glb_url: 'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/Suzanne/glTF-Binary/Suzanne.glb',
                    source: 'GitHub',
                    category: 'art'
                }
            ];
            
            const queryLower = query.toLowerCase();
            const filtered = objaverseModels.filter(model => {
                if (!query || query === '') return true;
                const searchText = `${model.name} ${model.category} ${model.source}`.toLowerCase();
                return searchText.includes(queryLower);
            });
            
            return filtered.map((item, i) => ({
                id: `objaverse-${item.uid}`,
                source: 'objaverse',
                name: item.name,
                description: `Objaverse-XL ${item.category} model from ${item.source}`,
                modelUrl: item.glb_url,
                format: item.glb_url.endsWith('.glb') ? 'glb' : 'gltf',
                type: '3d_object',
                category: item.category,
                provider: item.source,
                val: 2 + Math.random(), // Medium importance
                tags: [item.category, item.source],
                dataset: 'Objaverse-XL Demo (Full: 10M+ objects)'
            }));
            
        } catch (error) {
            console.error('Objaverse search error:', error);
            return [];
        }
    }
}

class GitHubProvider {
    async search(query) {
        try {
            // Use GitHub API directly for real search results
            const response = await fetch(`https://api.github.com/search/repositories?q=${encodeURIComponent(query)}+in:name,description&sort=stars&order=desc&per_page=20`, {
                headers: {
                    'Accept': 'application/vnd.github.v3+json',
                    'User-Agent': 'CosmosVisualizer/1.0'
                }
            });
            
            if (response.ok) {
                const data = await response.json();
                console.log(`Found ${data.total_count} GitHub repositories for "${query}"`);
                
                const nodes = [];
                const relationships = [];
                
                data.items.forEach((repo, i) => {
                    // Calculate value based on stars, forks, and other metrics
                    const stars = repo.stargazers_count || 0;
                    const forks = repo.forks_count || 0;
                    const watchers = repo.watchers_count || 0;
                    const openIssues = repo.open_issues_count || 0;
                    
                    // Logarithmic scaling for better visualization
                    const starScore = Math.log10(stars + 1);
                    const forkScore = Math.log10(forks + 1) * 0.8;
                    const watcherScore = Math.log10(watchers + 1) * 0.5;
                    const activityScore = Math.log10(openIssues + 1) * 0.3;
                    
                    // Combined importance score (1-10 scale)
                    const totalScore = starScore + forkScore + watcherScore + activityScore;
                    const normalizedVal = Math.min(10, Math.max(1, totalScore));
                    
                    // Add repository node
                    const repoNode = {
                        id: `github-repo-${repo.id}`,
                        source: 'github',
                        name: repo.name,
                        description: repo.description || 'No description',
                        type: 'repository',
                        url: repo.html_url,
                        val: normalizedVal, // Size based on combined metrics
                        stars: stars,
                        forks: forks,
                        watchers: watchers,
                        openIssues: openIssues,
                        language: repo.language,
                        topics: repo.topics || [],
                        created_at: repo.created_at,
                        updated_at: repo.updated_at,
                        owner: repo.owner.login,
                        // Additional metadata for visual effects
                        brightness: Math.min(1, starScore / 5), // Brightness based on stars
                        activity: openIssues > 0 ? Math.min(1, openIssues / 100) : 0
                    };
                    nodes.push(repoNode);
                    
                    // Add owner as a separate node
                    const ownerNode = {
                        id: `github-user-${repo.owner.id}`,
                        source: 'github',
                        name: repo.owner.login,
                        type: 'user',
                        url: repo.owner.html_url,
                        avatar: repo.owner.avatar_url,
                        val: 1.5
                    };
                    
                    // Only add owner if not already added
                    if (!nodes.find(n => n.id === ownerNode.id)) {
                        nodes.push(ownerNode);
                    }
                    
                    // Add relationship
                    relationships.push({
                        source: ownerNode.id,
                        target: repoNode.id,
                        strength: 1,
                        type: 'owns'
                    });
                    
                    // Add language relationships
                    if (repo.language && i > 0) {
                        const prevRepo = data.items[i - 1];
                        if (prevRepo.language === repo.language) {
                            relationships.push({
                                source: `github-repo-${prevRepo.id}`,
                                target: repoNode.id,
                                strength: 0.3,
                                type: 'same_language'
                            });
                        }
                    }
                });
                
                // Add relationships to nodes
                nodes.forEach(node => {
                    node.relationships = relationships
                        .filter(rel => rel.source === node.id || rel.target === node.id)
                        .map(rel => ({
                            target: rel.source === node.id ? rel.target : rel.source,
                            strength: rel.strength,
                            type: rel.type
                        }));
                });
                
                return nodes;
            } else {
                console.log('GitHub API rate limited, using fallback');
            }
            
            // Fallback: Real 3D/WebGL related repositories
            const real3DRepos = [
                {
                    name: 'three.js',
                    owner: 'mrdoob',
                    description: 'JavaScript 3D library',
                    stars: 95000,
                    topics: ['3d', 'webgl', 'javascript']
                },
                {
                    name: 'react-three-fiber',
                    owner: 'pmndrs',
                    description: 'React renderer for three.js',
                    stars: 24000,
                    topics: ['react', 'threejs', '3d']
                },
                {
                    name: 'aframe',
                    owner: 'aframevr',
                    description: 'Web framework for building VR experiences',
                    stars: 15000,
                    topics: ['vr', 'webvr', '3d']
                },
                {
                    name: 'babylonjs',
                    owner: 'BabylonJS',
                    description: 'Powerful 3D engine',
                    stars: 21000,
                    topics: ['3d', 'webgl', 'game-engine']
                },
                {
                    name: 'model-viewer',
                    owner: 'google',
                    description: 'Display 3D models on the web',
                    stars: 5000,
                    topics: ['3d', 'gltf', 'web-components']
                }
            ];
            
            const filtered = real3DRepos.filter(repo => {
                const searchText = `${repo.name} ${repo.owner} ${repo.description} ${repo.topics.join(' ')}`.toLowerCase();
                return searchText.includes(query.toLowerCase()) || query === '';
            });
            
            const nodes = [];
            const relationships = [];
            
            filtered.forEach((repo, i) => {
                // Add repo node
                const repoNode = {
                    id: `github-repo-${i}`,
                    source: 'github',
                    name: repo.name,
                    description: repo.description,
                    type: 'repository',
                    url: `https://github.com/${repo.owner}/${repo.name}`,
                    val: Math.log(repo.stars) / 2, // Size based on popularity
                    stars: repo.stars,
                    topics: repo.topics
                };
                nodes.push(repoNode);
                
                // Add owner node
                const ownerNode = {
                    id: `github-user-${i}`,
                    source: 'github',
                    name: repo.owner,
                    type: 'user',
                    url: `https://github.com/${repo.owner}`,
                    val: 2
                };
                nodes.push(ownerNode);
                
                // Add relationship
                relationships.push({
                    source: ownerNode.id,
                    target: repoNode.id,
                    strength: 1
                });
                
                // Add topic relationships
                if (i > 0 && repo.topics.some(topic => filtered[i-1].topics.includes(topic))) {
                    relationships.push({
                        source: repoNode.id,
                        target: `github-repo-${i-1}`,
                        strength: 0.5
                    });
                }
            });
            
            // Add relationships to nodes
            nodes.forEach(node => {
                node.relationships = relationships
                    .filter(rel => rel.source === node.id || rel.target === node.id)
                    .map(rel => ({
                        target: rel.source === node.id ? rel.target : rel.source,
                        strength: rel.strength
                    }));
            });
            
            return nodes;
        } catch (error) {
            console.error('GitHub search error:', error);
            return [];
        }
    }
    
    processGitHubEvents(events, query) {
        const nodes = new Map();
        const relationships = [];
        
        events
            .filter(event => {
                const searchText = `${event.repo?.name || ''} ${event.actor?.login || ''}`.toLowerCase();
                return searchText.includes(query.toLowerCase());
            })
            .slice(0, 50)
            .forEach(event => {
                // Add actor
                const actorId = `github-actor-${event.actor?.id}`;
                if (!nodes.has(actorId)) {
                    nodes.set(actorId, {
                        id: actorId,
                        source: 'github',
                        name: event.actor?.login,
                        type: 'user',
                        url: `https://github.com/${event.actor?.login}`,
                        val: 1
                    });
                }
                
                // Add repo
                const repoId = `github-repo-${event.repo?.id}`;
                if (!nodes.has(repoId)) {
                    nodes.set(repoId, {
                        id: repoId,
                        source: 'github',
                        name: event.repo?.name,
                        type: 'repository',
                        url: `https://github.com/${event.repo?.name}`,
                        val: 2
                    });
                }
                
                relationships.push({
                    source: actorId,
                    target: repoId,
                    type: event.type
                });
            });
        
        return Array.from(nodes.values());
    }
}

class WebProvider {
    async search(query) {
        try {
            // Option 1: Use a search API like SerpAPI, Bing, etc (requires API key)
            // const response = await fetch(`https://api.serpapi.com/search?q=${query}&api_key=YOUR_KEY`);
            
            // Option 2: Use Wikipedia API for educational content
            try {
                const wikiResponse = await fetch(
                    `https://en.wikipedia.org/api/rest_v1/page/search/${encodeURIComponent(query)}?limit=5`
                );
                
                if (wikiResponse.ok) {
                    const wikiData = await wikiResponse.json();
                    return wikiData.pages.map((page, i) => ({
                        id: `wiki-${page.id}`,
                        source: 'web',
                        name: page.title,
                        description: page.description || page.extract,
                        url: `https://en.wikipedia.org/wiki/${encodeURIComponent(page.title)}`,
                        thumbnail: page.thumbnail?.source
                    }));
                }
            } catch (err) {
                console.log('Wikipedia search failed, using fallback');
            }
            
            // Fallback: Curated 3D/XR web resources
            const webResources = [
                {
                    name: 'Three.js Documentation',
                    url: 'https://threejs.org/docs/',
                    description: 'Official Three.js documentation and examples'
                },
                {
                    name: 'WebXR Samples',
                    url: 'https://immersive-web.github.io/webxr-samples/',
                    description: 'WebXR API examples and demos'
                },
                {
                    name: 'Sketchfab',
                    url: 'https://sketchfab.com/',
                    description: 'Platform for publishing 3D models'
                },
                {
                    name: 'A-Frame School',
                    url: 'https://aframe.io/aframe-school/',
                    description: 'Interactive tutorials for WebVR'
                },
                {
                    name: 'Babylon.js Playground',
                    url: 'https://playground.babylonjs.com/',
                    description: 'Interactive Babylon.js examples'
                }
            ];
            
            const filtered = webResources.filter(resource => {
                const searchText = `${resource.name} ${resource.description}`.toLowerCase();
                return searchText.includes(query.toLowerCase()) || query === '';
            });
            
            return filtered.map((resource, i) => ({
                id: `web-${i}`,
                source: 'web',
                name: resource.name,
                description: resource.description,
                url: resource.url
            }));
            
        } catch (error) {
            console.error('Web search error:', error);
            return [{
                id: 'web-fallback',
                source: 'web',
                name: `Search web for "${query}"`,
                url: `https://www.google.com/search?q=${encodeURIComponent(query + ' 3d webgl webxr')}`,
                description: 'Search Google for 3D/WebGL/WebXR content'
            }];
        }
    }
}

class LocalProvider {
    async search(query) {
        try {
            // Search through local file index
            const localFiles = [
                // Unity Projects
                {
                    name: 'unity-cosmos',
                    type: 'unity_project',
                    path: '/Users/jamestunick/Desktop/XRAI/unity-cosmos',
                    description: 'Unity VR cosmos visualization project'
                },
                {
                    name: 'SplatVFX_Experiments',
                    type: 'unity_project', 
                    path: '/Users/jamestunick/Desktop/XRAI/vnmf stuff/vnmf-complete/decoder/unity/SplatVFX_Experiments-main',
                    description: 'Unity VFX experiments with splat rendering'
                },
                
                // 3D Models and Assets
                {
                    name: 'objaverse_gltf_index_lite.json',
                    type: 'data_file',
                    path: '/Users/jamestunick/Desktop/XRAI/objaverse_gltf_index_lite.json',
                    description: 'Index of 200k+ 3D models from Objaverse',
                    size: '26MB'
                },
                {
                    name: 'objaverse_glb_index_lite.json', 
                    type: 'data_file',
                    path: '/Users/jamestunick/Desktop/XRAI/objaverse_glb_index_lite.json',
                    description: 'GLB format index for Objaverse models',
                    size: '206MB'
                },
                
                // AI Services
                {
                    name: 'xrrai-prompt',
                    type: 'ai_project',
                    path: '/Users/jamestunick/Desktop/XRAI/xrrai-prompt',
                    description: 'AI-powered 3D content generation platform'
                },
                {
                    name: 'TTS Services',
                    type: 'ai_service',
                    path: '/Users/jamestunick/Desktop/XRAI/xrrai-prompt/TTS',
                    description: 'Text-to-speech AI services'
                },
                
                // Web Projects
                {
                    name: 'cosmos-standalone-web',
                    type: 'web_project',
                    path: '/Users/jamestunick/Desktop/XRAI/cosmos-standalone-web',
                    description: 'Three.js hypergraph visualizer'
                },
                {
                    name: 'AI-XR-MCP-main',
                    type: 'web_project',
                    path: '/Users/jamestunick/Desktop/XRAI/AI-XR-MCP-main',
                    description: 'MCP server for 3D WebGL visualizations'
                },
                
                // Documentation and Specs
                {
                    name: 'xrai-format',
                    type: 'specification',
                    path: '/Users/jamestunick/Desktop/XRAI/xrai-format',
                    description: 'XRAI spatial media format specification'
                },
                {
                    name: 'CLAUDE.md',
                    type: 'documentation',
                    path: '/Users/jamestunick/Desktop/XRAI/CLAUDE.md',
                    description: 'Claude Code project instructions'
                },
                
                // Sample Projects
                {
                    name: 'EntityComponentSystemSamples',
                    type: 'unity_samples',
                    path: '/Users/jamestunick/Desktop/XRAI/vis/EntityComponentSystemSamples-master',
                    description: 'Unity DOTS/ECS sample projects'
                },
                
                // Media Files (examples)
                {
                    name: 'XR Portal Demo GIF',
                    type: 'media_file',
                    path: '/Users/jamestunick/Desktop/XRAI/364233351-078d9368-25ff-4fa8-99ed-0dbfadfc02b9.gif',
                    description: 'XR portal demonstration animation'
                },
                {
                    name: 'VFX Learning Guide',
                    type: 'document',
                    path: '/Users/jamestunick/Desktop/XRAI/931a11be-1f96-4bb6-8432-ad8fa2ebf3b2_Portals_101__XR__AI__VFX_Graph_Learning_Guide.pdf',
                    description: 'XR AI VFX Graph learning guide'
                }
            ];
            
            // Filter by query
            const filtered = localFiles.filter(file => {
                const searchText = `${file.name} ${file.description} ${file.type}`.toLowerCase();
                return searchText.includes(query.toLowerCase()) || query === '';
            });
            
            // Add relationships based on project structure
            const relationships = [];
            filtered.forEach((file, i) => {
                // Connect related files
                if (file.type === 'unity_project' && filtered.some(f => f.type === 'unity_samples')) {
                    const samplesFile = filtered.find(f => f.type === 'unity_samples');
                    if (samplesFile) {
                        relationships.push({
                            source: file.name,
                            target: samplesFile.name,
                            strength: 0.5,
                            type: 'related_project'
                        });
                    }
                }
                
                // Connect AI projects to data files
                if (file.type === 'ai_project' && filtered.some(f => f.type === 'data_file')) {
                    filtered
                        .filter(f => f.type === 'data_file')
                        .forEach(dataFile => {
                            relationships.push({
                                source: file.name,
                                target: dataFile.name,
                                strength: 0.7,
                                type: 'uses_data'
                            });
                        });
                }
            });
            
            return filtered.map((file, i) => ({
                id: `local-${i}`,
                source: 'local',
                name: file.name,
                description: file.description,
                type: file.type,
                path: file.path,
                size: file.size,
                val: file.size ? Math.log(parseInt(file.size)) : 2,
                relationships: relationships
                    .filter(rel => rel.source === file.name || rel.target === file.name)
                    .map(rel => ({
                        target: rel.source === file.name ? rel.target : rel.source,
                        strength: rel.strength,
                        type: rel.type
                    }))
            }));
            
        } catch (error) {
            console.error('Local search error:', error);
            return [];
        }
    }
}
