// Objaverse Configuration
// Based on https://objaverse.allenai.org/

export const ObjaverseConfig = {
    // Objaverse doesn't have a direct REST API, so we need workarounds
    endpoints: {
        // Official Objaverse website
        website: 'https://objaverse.allenai.org/',
        
        // GitHub repo with data
        github: 'https://github.com/allenai/objaverse-xl',
        
        // Hugging Face dataset
        huggingFace: 'https://huggingface.co/datasets/allenai/objaverse-xl',
        
        // Local index files (if downloaded)
        localIndexGLTF: '/objaverse_gltf_index_lite.json',
        localIndexGLB: '/objaverse_glb_index_lite.json',
        
        // Example object URLs from Objaverse (GitHub hosted)
        exampleObjects: [
            'https://github.com/allenai/objaverse-xl/raw/main/scripts/rendering/example_objects/',
            'https://raw.githubusercontent.com/allenai/objaverse-rendering/main/views/'
        ],
        
        // Proxy endpoint for Python API bridge (if implemented)
        proxyAPI: '/api/objaverse' // Would need backend implementation
    },
    
    // Dataset information
    dataset: {
        totalObjects: '10M+',
        version: 'Objaverse-XL',
        license: 'ODC-By v1.0',
        formats: ['blend', 'obj', 'fbx', 'gltf', 'glb'],
        sources: [
            'GitHub',
            'Thingiverse', 
            'Polycam',
            'Sketchfab'
        ]
    },
    
    // Visual settings for Objaverse content
    visualization: {
        color: '#4ECDC4',      // Teal for Objaverse content
        emissiveIntensity: 0.2,
        nodeScale: 1.0,
        showSourceLabels: true,
        animateModels: true
    },
    
    // Search configuration
    search: {
        useLocalIndex: true,    // Use local JSON index if available
        maxResults: 100000,     // Support massive datasets
        cacheResults: true,
        cacheDuration: 3600000  // 1 hour cache
    },
    
    // Python API bridge configuration (for future implementation)
    pythonBridge: {
        enabled: false,
        endpoint: 'http://localhost:5000/objaverse',
        timeout: 30000
    }
};

// Helper to check if local Objaverse index is available
export async function checkObjaverseIndex() {
    try {
        const response = await fetch(ObjaverseConfig.endpoints.localIndexGLTF);
        if (response.ok) {
            const data = await response.json();
            console.log(`Objaverse local index loaded: ${data.length} objects`);
            return true;
        }
    } catch (error) {
        console.log('Objaverse local index not found, using fallback data');
    }
    return false;
}

// Helper to get Objaverse object URL
export function getObjaverseObjectUrl(objectId, format = 'glb') {
    // Objaverse objects are typically hosted on various platforms
    // This would need to be extracted from the dataset
    
    // For now, return example URLs
    const exampleUrls = [
        'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/Duck/glTF-Binary/Duck.glb',
        'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/Fox/glTF-Binary/Fox.glb',
        'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/BrainStem/glTF-Binary/BrainStem.glb'
    ];
    
    return exampleUrls[Math.floor(Math.random() * exampleUrls.length)];
}