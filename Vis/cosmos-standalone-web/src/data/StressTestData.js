export class StressTestData {
    static async loadObjaverseSubset(count) {
        console.log(`Loading ${count} items from Objaverse index...`);
        
        try {
            const response = await fetch('/objaverse_gltf_index_lite.json');
            if (!response.ok) {
                return this.generateMassiveDataset(count);
            }
            
            const index = await response.json();
            const nodes = [];
            const links = [];
            
            // Take subset of data
            const subset = index.slice(0, Math.min(count, index.length));
            
            subset.forEach((item, i) => {
                nodes.push({
                    id: `obja-${i}`,
                    name: item.n || `Model ${i + 1}`,
                    type: 'artwork',
                    source: 'objaverse',
                    modelUrl: item.i,
                    val: 1 + Math.random() * 2,
                    artist: item.s || 'Unknown',
                    tags: item.g || []
                });
            });
            
            // Create some connections based on shared tags or sources
            for (let i = 0; i < nodes.length - 1; i++) {
                if (Math.random() > 0.7) {
                    links.push({
                        source: nodes[i].id,
                        target: nodes[i + 1].id,
                        value: Math.random()
                    });
                }
            }
            
            return { nodes, links };
        } catch (error) {
            console.error('Error loading Objaverse data:', error);
            return this.generateMassiveDataset(count);
        }
    }
    
    static generateMassiveDataset(nodeCount) {
        console.log(`Generating ${nodeCount.toLocaleString()} synthetic nodes...`);
        
        const nodes = [];
        const links = [];
        
        // Generate nodes in batches for memory efficiency
        const batchSize = 10000;
        const numBatches = Math.ceil(nodeCount / batchSize);
        
        for (let batch = 0; batch < numBatches; batch++) {
            const startIdx = batch * batchSize;
            const endIdx = Math.min(startIdx + batchSize, nodeCount);
            
            for (let i = startIdx; i < endIdx; i++) {
                // Create lightweight nodes
                nodes.push({
                    id: `node-${i}`,
                    name: `Item ${i}`,
                    type: this.getRandomType(),
                    source: this.getRandomSource(),
                    val: 0.5 + Math.random() * 2.5,
                    x: (Math.random() - 0.5) * 1000,
                    y: (Math.random() - 0.5) * 1000,
                    z: (Math.random() - 0.5) * 1000
                });
            }
            
            // Create sparse connections (only connect nearby nodes)
            if (batch > 0) {
                const prevBatchStart = (batch - 1) * batchSize;
                for (let i = 0; i < 10; i++) {
                    const sourceIdx = prevBatchStart + Math.floor(Math.random() * batchSize);
                    const targetIdx = startIdx + Math.floor(Math.random() * (endIdx - startIdx));
                    
                    if (nodes[sourceIdx] && nodes[targetIdx]) {
                        links.push({
                            source: nodes[sourceIdx].id,
                            target: nodes[targetIdx].id,
                            value: Math.random()
                        });
                    }
                }
            }
        }
        
        // Add some hub nodes for interesting topology
        const hubCount = Math.min(100, nodeCount / 1000);
        for (let i = 0; i < hubCount; i++) {
            const hubIdx = Math.floor(Math.random() * nodeCount);
            const connectionCount = 10 + Math.floor(Math.random() * 20);
            
            for (let j = 0; j < connectionCount; j++) {
                const targetIdx = Math.floor(Math.random() * nodeCount);
                if (hubIdx !== targetIdx && nodes[hubIdx] && nodes[targetIdx]) {
                    links.push({
                        source: nodes[hubIdx].id,
                        target: nodes[targetIdx].id,
                        value: 0.1 + Math.random() * 0.5
                    });
                }
            }
        }
        
        console.log(`Generated ${nodes.length} nodes and ${links.length} links`);
        return { nodes, links };
    }
    
    static getRandomType() {
        const types = ['artwork', 'model', 'image', 'video', 'document'];
        return types[Math.floor(Math.random() * types.length)];
    }
    
    static getRandomSource() {
        const sources = ['icosa', 'objaverse', 'github', 'local', 'web'];
        return sources[Math.floor(Math.random() * sources.length)];
    }
}