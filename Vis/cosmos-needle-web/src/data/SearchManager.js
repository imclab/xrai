import axios from 'axios';
import pako from 'pako';

export class SearchManager {
    constructor() {
        this.sources = {
            icosa: this.searchIcosa.bind(this),
            objaverse: this.searchObjaverse.bind(this),
            github: this.searchGitHub.bind(this),
            web: this.searchWeb.bind(this),
            local: this.searchLocal.bind(this)
        };
        
        this.cache = new Map();
    }
    
    async search(query, sourcesArray) {
        const results = [];
        const selectedSources = sourcesArray.includes('all') 
            ? Object.keys(this.sources) 
            : sourcesArray;
        
        // Search in parallel
        const promises = selectedSources.map(source => 
            this.sources[source]?.(query).catch(err => {
                console.error(`Error searching ${source}:`, err);
                return [];
            })
        );
        
        const sourceResults = await Promise.all(promises);
        sourceResults.forEach(items => results.push(...items));
        
        return this.rankResults(results, query);
    }
    
    async searchIcosa(query) {
        try {
            const response = await axios.get('https://api.icosa.foundation/artworks/search', {
                params: { q: query, limit: 20 }
            });
            
            return response.data.results.map(artwork => ({
                id: `icosa-${artwork.id}`,
                source: 'icosa',
                name: artwork.title,
                description: artwork.description,
                url: `https://icosa.foundation/artworks/${artwork.id}`,
                modelUrl: artwork.gltfUrl,
                format: 'gltf',
                thumbnail: artwork.thumbnailUrl,
                author: artwork.artist?.name,
                relevance: this.calculateRelevance(query, artwork.title, artwork.description)
            }));
        } catch (error) {
            console.error('Icosa search error:', error);
            return [];
        }
    }
    
    async searchObjaverse(query) {
        try {
            // Use Objaverse API or local index if available
            const indexPath = '/Users/jamestunick/Desktop/XRAI/objaverse_gltf_index_lite.json';
            
            // For demo, return mock results
            // In production, would load the actual index or use API
            const mockResults = [
                {
                    uid: 'obj-001',
                    name: `Objaverse ${query} Model 1`,
                    gltfUrl: 'https://objaverse.allenai.org/example.glb'
                },
                {
                    uid: 'obj-002', 
                    name: `Objaverse ${query} Model 2`,
                    gltfUrl: 'https://objaverse.allenai.org/example2.glb'
                }
            ];
            
            return mockResults.map(obj => ({
                id: `objaverse-${obj.uid}`,
                source: 'objaverse',
                name: obj.name,
                modelUrl: obj.gltfUrl,
                format: 'glb',
                relevance: this.calculateRelevance(query, obj.name)
            }));
        } catch (error) {
            console.error('Objaverse search error:', error);
            return [];
        }
    }
    
    async searchGitHub(query) {
        try {
            // Use GitHub Archive API
            const date = new Date();
            const hour = date.getUTCHours();
            const dateStr = date.toISOString().split('T')[0];
            
            const url = `https://data.gharchive.org/${dateStr}-${hour}.json.gz`;
            const response = await axios.get(url, { responseType: 'arraybuffer' });
            
            // Decompress gzip data
            const decompressed = pako.inflate(response.data, { to: 'string' });
            const events = decompressed.split('\n')
                .filter(line => line.trim())
                .map(line => JSON.parse(line));
            
            // Filter events by query
            const filtered = events.filter(event => {
                const searchText = `${event.repo?.name} ${event.actor?.login} ${event.payload?.description || ''}`.toLowerCase();
                return searchText.includes(query.toLowerCase());
            });
            
            // Convert to graph nodes
            const nodes = new Map();
            const relationships = [];
            
            filtered.forEach(event => {
                // Add actor node
                const actorId = `github-actor-${event.actor?.login}`;
                if (!nodes.has(actorId)) {
                    nodes.set(actorId, {
                        id: actorId,
                        source: 'github',
                        name: event.actor?.login || 'Unknown',
                        type: 'user',
                        url: `https://github.com/${event.actor?.login}`
                    });
                }
                
                // Add repo node
                const repoId = `github-repo-${event.repo?.name}`;
                if (!nodes.has(repoId)) {
                    nodes.set(repoId, {
                        id: repoId,
                        source: 'github',
                        name: event.repo?.name || 'Unknown',
                        type: 'repository',
                        url: `https://github.com/${event.repo?.name}`
                    });
                }
                
                // Add relationship
                relationships.push({
                    source: actorId,
                    target: repoId,
                    type: event.type
                });
            });
            
            // Convert to results format
            const results = Array.from(nodes.values());
            results.forEach(node => {
                node.relationships = relationships
                    .filter(rel => rel.source === node.id || rel.target === node.id)
                    .map(rel => ({
                        target: rel.source === node.id ? rel.target : rel.source,
                        strength: 1
                    }));
                node.relevance = this.calculateRelevance(query, node.name);
            });
            
            return results;
            
        } catch (error) {
            console.error('GitHub search error:', error);
            return [];
        }
    }
    
    async searchWeb(query) {
        // In a real implementation, this would use a web search API
        // For demo, return mock results
        return [
            {
                id: `web-1`,
                source: 'web',
                name: `Web result for "${query}"`,
                url: `https://example.com/search?q=${encodeURIComponent(query)}`,
                relevance: 0.8
            }
        ];
    }
    
    async searchLocal(query) {
        // This would search local file system
        // For demo, return empty array
        return [];
    }
    
    calculateRelevance(query, ...fields) {
        const queryLower = query.toLowerCase();
        const text = fields.join(' ').toLowerCase();
        
        // Simple relevance scoring
        let score = 0;
        if (text.includes(queryLower)) score += 0.5;
        
        const words = queryLower.split(' ');
        words.forEach(word => {
            if (text.includes(word)) score += 0.3 / words.length;
        });
        
        return Math.min(score, 1);
    }
    
    rankResults(results, query) {
        return results
            .sort((a, b) => (b.relevance || 0) - (a.relevance || 0))
            .slice(0, 100); // Limit results
    }
}