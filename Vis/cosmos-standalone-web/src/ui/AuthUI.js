// Authentication UI Component
import { IcosaDeviceAuth } from '../auth/IcosaDeviceAuth.js';
import { AuthConfig, isAuthenticated } from '../config/auth-config.js';

export class AuthUI {
    constructor() {
        this.icosaAuth = new IcosaDeviceAuth();
        this.setupEventHandlers();
        this.checkAuthStatus();
    }
    
    setupEventHandlers() {
        // Success handler
        this.icosaAuth.onAuthSuccess = (tokenData) => {
            this.updateAuthStatus();
            this.showSuccessNotification('Icosa Gallery authenticated successfully!');
            
            // Reload the page to apply auth
            setTimeout(() => {
                window.location.reload();
            }, 2000);
        };
        
        // Error handler
        this.icosaAuth.onAuthError = (error) => {
            this.showErrorNotification(`Authentication failed: ${error.message}`);
        };
    }
    
    checkAuthStatus() {
        // Load saved token on startup
        const savedToken = IcosaDeviceAuth.loadSavedToken();
        if (savedToken) {
            AuthConfig.icosaGallery.jwtToken = savedToken.token;
            AuthConfig.icosaGallery.tokenExpiry = savedToken.expiresAt;
            console.log('Loaded saved Icosa Gallery token');
        }
        
        // Update UI
        this.updateAuthStatus();
    }
    
    updateAuthStatus() {
        // Update auth indicators in the UI
        const icosaCheckbox = document.querySelector('input[value="icosa"]');
        if (icosaCheckbox) {
            const label = icosaCheckbox.parentElement;
            const isAuthed = isAuthenticated('icosa');
            
            // Add auth indicator
            let indicator = label.querySelector('.auth-indicator');
            if (!indicator) {
                indicator = document.createElement('span');
                indicator.className = 'auth-indicator';
                indicator.style.cssText = 'margin-left: 5px; font-size: 12px;';
                label.appendChild(indicator);
            }
            
            if (isAuthed) {
                indicator.textContent = '✓';
                indicator.style.color = '#4CAF50';
                indicator.title = 'Authenticated';
            } else {
                indicator.textContent = '🔒';
                indicator.style.color = '#FF6B6B';
                indicator.title = 'Click to authenticate';
                indicator.style.cursor = 'pointer';
                
                // Add click handler to authenticate
                indicator.onclick = (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    this.startAuthentication('icosa');
                };
            }
        }
    }
    
    addAuthButtons() {
        // Add auth section to sidebar
        const sidebar = document.querySelector('.sidebar');
        if (!sidebar || document.getElementById('auth-section')) return;
        
        const authSection = document.createElement('div');
        authSection.id = 'auth-section';
        authSection.className = 'auth-section';
        authSection.innerHTML = `
            <h3>API Authentication</h3>
            <div class="auth-status">
                ${this.getAuthStatusHTML('icosa', 'Icosa Gallery')}
                ${this.getAuthStatusHTML('sketchfab', 'Sketchfab')}
                ${this.getAuthStatusHTML('github', 'GitHub')}
            </div>
        `;
        
        // Add styles
        const style = document.createElement('style');
        style.textContent = `
            .auth-section {
                margin-top: 20px;
                padding: 15px;
                background: rgba(255, 255, 255, 0.05);
                border-radius: 8px;
            }
            
            .auth-section h3 {
                margin-top: 0;
                margin-bottom: 10px;
                font-size: 14px;
                color: #aaa;
            }
            
            .auth-status {
                display: flex;
                flex-direction: column;
                gap: 8px;
            }
            
            .auth-item {
                display: flex;
                align-items: center;
                justify-content: space-between;
                padding: 8px 12px;
                background: rgba(255, 255, 255, 0.03);
                border-radius: 6px;
                font-size: 13px;
            }
            
            .auth-item.authenticated {
                border: 1px solid rgba(76, 175, 80, 0.3);
            }
            
            .auth-item.unauthenticated {
                border: 1px solid rgba(255, 107, 107, 0.3);
            }
            
            .auth-button {
                padding: 4px 12px;
                font-size: 11px;
                background: #FF6B6B;
                border: none;
                border-radius: 4px;
                color: white;
                cursor: pointer;
                transition: background 0.3s;
            }
            
            .auth-button:hover {
                background: #ff5252;
            }
            
            .auth-button.authenticated {
                background: #4CAF50;
                cursor: default;
            }
            
            .auth-button.authenticated:hover {
                background: #4CAF50;
            }
        `;
        
        if (!document.querySelector('#auth-styles')) {
            style.id = 'auth-styles';
            document.head.appendChild(style);
        }
        
        // Find a good position to insert
        const statsSection = sidebar.querySelector('.stats');
        if (statsSection) {
            statsSection.parentNode.insertBefore(authSection, statsSection.nextSibling);
        } else {
            sidebar.appendChild(authSection);
        }
    }
    
    getAuthStatusHTML(service, name) {
        const isAuthed = isAuthenticated(service);
        const statusClass = isAuthed ? 'authenticated' : 'unauthenticated';
        const buttonText = isAuthed ? '✓ Authenticated' : 'Authenticate';
        const buttonClass = `auth-button ${statusClass}`;
        
        return `
            <div class="auth-item ${statusClass}">
                <span>${name}</span>
                <button class="${buttonClass}" 
                        onclick="window.authUI.startAuthentication('${service}')"
                        ${isAuthed ? 'disabled' : ''}>
                    ${buttonText}
                </button>
            </div>
        `;
    }
    
    async startAuthentication(service) {
        switch (service) {
            case 'icosa':
                try {
                    await this.icosaAuth.authenticate();
                } catch (error) {
                    console.error('Icosa authentication failed:', error);
                }
                break;
                
            case 'sketchfab':
                this.showInfoNotification(
                    'Sketchfab Authentication',
                    'Visit https://sketchfab.com/settings/developer to get your API token, then add it to auth-config.js'
                );
                break;
                
            case 'github':
                this.showInfoNotification(
                    'GitHub Authentication',
                    'Visit https://github.com/settings/tokens to create a personal access token, then add it to auth-config.js'
                );
                break;
        }
    }
    
    showSuccessNotification(message) {
        this.showNotification(message, 'success');
    }
    
    showErrorNotification(message) {
        this.showNotification(message, 'error');
    }
    
    showInfoNotification(title, message) {
        this.showNotification(`<strong>${title}</strong><br>${message}`, 'info', 5000);
    }
    
    showNotification(message, type = 'info', duration = 3000) {
        const notification = document.createElement('div');
        notification.className = `notification ${type}`;
        notification.innerHTML = message;
        
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            max-width: 300px;
            padding: 16px 20px;
            background: ${type === 'success' ? '#4CAF50' : type === 'error' ? '#f44336' : '#2196F3'};
            color: white;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
            z-index: 10001;
            animation: slideIn 0.3s ease-out;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            font-size: 14px;
            line-height: 1.4;
        `;
        
        // Add animation
        if (!document.querySelector('#notification-styles')) {
            const style = document.createElement('style');
            style.id = 'notification-styles';
            style.textContent = `
                @keyframes slideIn {
                    from {
                        transform: translateX(100%);
                        opacity: 0;
                    }
                    to {
                        transform: translateX(0);
                        opacity: 1;
                    }
                }
                
                @keyframes slideOut {
                    from {
                        transform: translateX(0);
                        opacity: 1;
                    }
                    to {
                        transform: translateX(100%);
                        opacity: 0;
                    }
                }
            `;
            document.head.appendChild(style);
        }
        
        document.body.appendChild(notification);
        
        // Auto remove
        setTimeout(() => {
            notification.style.animation = 'slideOut 0.3s ease-out';
            setTimeout(() => notification.remove(), 300);
        }, duration);
    }
}

// Create global instance
window.authUI = new AuthUI();