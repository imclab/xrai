// Icosa Gallery Configuration
// Based on icosa-foundation/icosa-gallery

export const IcosaConfig = {
    // Default to poly.pizza public instance
    defaultEndpoint: 'https://poly.pizza/api',
    
    // Alternative Icosa Gallery instances
    endpoints: {
        icosaOfficial: {
            name: 'Icosa Gallery (Official)',
            url: 'https://api.icosa.gallery/v1',
            requiresAuth: true, // Requires JWT token
            cors: true, // May need CORS proxy
            apiDocs: 'https://api.icosa.gallery/v1/docs'
        },
        polyPizza: {
            name: 'Poly.pizza (Public)',
            url: 'https://poly.pizza/api',
            requiresAuth: false,
            cors: true // May need CORS proxy
        },
        googlePolyLegacy: {
            name: 'Google Poly (Legacy)',
            url: 'https://poly.googleapis.com/v1',
            requiresAuth: false,
            apiKey: 'AIzaSyBHZM8O3GiHKejLw3sb4yPpWcA5FRLQxVk' // Public API key
        },
        selfHosted: {
            name: 'Self-Hosted Icosa',
            url: 'http://localhost:8080/api', // Change to your instance
            requiresAuth: false
        }
    },
    
    // Search parameters
    searchDefaults: {
        pageSize: 24,
        format: 'GLTF2',
        orderBy: 'BEST',
        maxPages: 10
    },
    
    // CORS proxy for browser access (if needed)
    corsProxy: 'https://cors-anywhere.herokuapp.com/',
    
    // Enable/disable CORS proxy
    useCorsProxy: false,
    
    // Format preferences
    formatPreferences: ['glb', 'gltf', 'obj', 'fbx'],
    
    // Visual settings for Icosa content
    visualization: {
        color: '#FF6B6B',      // Red/Pink for Icosa content
        emissiveIntensity: 0.3,
        nodeScale: 1.2,        // Make Icosa content slightly larger
        showArtistLabels: true,
        animateArtworks: true
    }
};

// Helper function to get configured endpoint
export function getIcosaEndpoint(endpointName = 'polyPizza') {
    const endpoint = IcosaConfig.endpoints[endpointName];
    if (!endpoint) {
        console.warn(`Unknown Icosa endpoint: ${endpointName}, using default`);
        return IcosaConfig.defaultEndpoint;
    }
    
    let url = endpoint.url;
    
    // Add CORS proxy if needed
    if (IcosaConfig.useCorsProxy && endpoint.cors) {
        url = IcosaConfig.corsProxy + url;
    }
    
    return url;
}

// Helper to build search URL
export function buildIcosaSearchUrl(endpoint, query, pageToken = '', pageNumber = 1) {
    const params = new URLSearchParams();
    
    // Official Icosa Gallery API v1 format
    if (endpoint.includes('api.icosa.gallery')) {
        params.append('q', query || '');
        params.append('pageSize', IcosaConfig.searchDefaults.pageSize);
        params.append('page', pageNumber);
        params.append('orderBy', 'LIKED'); // Use LIKED for Icosa Gallery
        
        // Add format filter for glTF models
        if (query) {
            params.append('format', 'GLTF,GLTF2');
            params.append('keywords', query);
        }
        
        return `${endpoint}/assets?${params}`;
    }
    
    // Legacy Google Poly / poly.pizza format
    else {
        params.append('keywords', query || '');
        params.append('format', IcosaConfig.searchDefaults.format);
        params.append('pageSize', IcosaConfig.searchDefaults.pageSize);
        params.append('orderBy', IcosaConfig.searchDefaults.orderBy);
        
        if (pageToken) {
            params.append('pageToken', pageToken);
        }
        
        // Add API key for Google Poly legacy
        if (endpoint.includes('googleapis.com') && IcosaConfig.endpoints.googlePolyLegacy.apiKey) {
            params.append('key', IcosaConfig.endpoints.googlePolyLegacy.apiKey);
        }
        
        return `${endpoint}/assets?${params}`;
    }
}