// Authentication Configuration for Various APIs
// Store your API keys here or in environment variables

const env = (() => {
    if (typeof process !== 'undefined' && process?.env) {
        return process.env;
    }
    if (typeof import.meta !== 'undefined' && import.meta.env) {
        return import.meta.env;
    }
    return {};
})();

export const AuthConfig = {
    // Icosa Gallery JWT token
    // To obtain: Use device login at https://api.icosa.gallery/v1/users/device-login
    icosaGallery: {
        jwtToken: env.ICOSA_JWT_TOKEN || env.VITE_ICOSA_JWT_TOKEN || '',
        // Token expires after 2 weeks, monitor expiration
        tokenExpiry: null,
        deviceLoginUrl: 'https://api.icosa.gallery/v1/users/device-login'
    },
    
    // Sketchfab API token
    // Get from: https://sketchfab.com/settings/developer
    sketchfab: {
        apiToken: env.SKETCHFAB_TOKEN || env.VITE_SKETCHFAB_TOKEN || '',
        tokenType: 'Token' // Sketchfab uses "Token" prefix
    },
    
    // GitHub personal access token
    // Create at: https://github.com/settings/tokens
    github: {
        token: env.GITHUB_TOKEN || env.VITE_GITHUB_TOKEN || '',
        tokenType: 'token' // GitHub uses lowercase "token" prefix
    },
    
    // Poly.pizza API key (if different from Icosa)
    polyPizza: {
        apiKey: env.POLYPIZZA_API_KEY || env.VITE_POLYPIZZA_API_KEY || ''
    }
};

// Helper to get authorization header
export function getAuthHeader(service) {
    switch (service) {
        case 'icosa':
            return AuthConfig.icosaGallery.jwtToken 
                ? { 'Authorization': `Bearer ${AuthConfig.icosaGallery.jwtToken}` }
                : {};
        
        case 'sketchfab':
            return AuthConfig.sketchfab.apiToken
                ? { 'Authorization': `${AuthConfig.sketchfab.tokenType} ${AuthConfig.sketchfab.apiToken}` }
                : {};
        
        case 'github':
            return AuthConfig.github.token
                ? { 'Authorization': `${AuthConfig.github.tokenType} ${AuthConfig.github.token}` }
                : {};
        
        default:
            return {};
    }
}

// Check if service is authenticated
export function isAuthenticated(service) {
    switch (service) {
        case 'icosa':
            return !!AuthConfig.icosaGallery.jwtToken;
        case 'sketchfab':
            return !!AuthConfig.sketchfab.apiToken;
        case 'github':
            return !!AuthConfig.github.token;
        default:
            return false;
    }
}
