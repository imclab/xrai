// Icosa Gallery Device Authentication Flow
// Based on the official Icosa Gallery API documentation

export class IcosaDeviceAuth {
    constructor() {
        this.authEndpoint = 'https://api.icosa.gallery/v1/users/device-login';
        this.pollInterval = null;
        this.onAuthSuccess = null;
        this.onAuthError = null;
    }
    
    // Start the device authentication flow
    async startDeviceLogin() {
        try {
            console.log('Starting Icosa Gallery device login flow...');
            
            // Step 1: Request device code
            const response = await fetch(this.authEndpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                }
            });
            
            if (!response.ok) {
                throw new Error(`Device login request failed: ${response.status}`);
            }
            
            const data = await response.json();
            console.log('Device login response:', data);
            
            // Expected response format:
            // {
            //   "device_code": "ABC123...",
            //   "user_code": "ABCD-1234",
            //   "verification_uri": "https://icosa.gallery/device",
            //   "expires_in": 600,
            //   "interval": 5
            // }
            
            if (data.user_code && data.verification_uri) {
                return {
                    userCode: data.user_code,
                    verificationUrl: data.verification_uri,
                    deviceCode: data.device_code,
                    expiresIn: data.expires_in || 600,
                    interval: data.interval || 5
                };
            } else {
                throw new Error('Invalid device login response');
            }
        } catch (error) {
            console.error('Device login error:', error);
            throw error;
        }
    }
    
    // Poll for authentication completion
    async pollForToken(deviceCode, interval = 5) {
        return new Promise((resolve, reject) => {
            const startTime = Date.now();
            const timeout = 600000; // 10 minutes timeout
            
            this.pollInterval = setInterval(async () => {
                try {
                    // Check if timeout exceeded
                    if (Date.now() - startTime > timeout) {
                        clearInterval(this.pollInterval);
                        reject(new Error('Authentication timeout'));
                        return;
                    }
                    
                    // Poll the token endpoint
                    const response = await fetch(`${this.authEndpoint}/token`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        body: JSON.stringify({
                            device_code: deviceCode,
                            grant_type: 'urn:ietf:params:oauth:grant-type:device_code'
                        })
                    });
                    
                    const data = await response.json();
                    
                    if (response.ok && data.access_token) {
                        clearInterval(this.pollInterval);
                        resolve({
                            accessToken: data.access_token,
                            tokenType: data.token_type || 'Bearer',
                            expiresIn: data.expires_in,
                            refreshToken: data.refresh_token
                        });
                    } else if (data.error === 'authorization_pending') {
                        // Still waiting for user to authorize
                        console.log('Authorization pending...');
                    } else if (data.error === 'slow_down') {
                        // Increase polling interval
                        clearInterval(this.pollInterval);
                        this.pollInterval = setInterval(arguments.callee, interval * 2000);
                    } else if (data.error) {
                        clearInterval(this.pollInterval);
                        reject(new Error(data.error_description || data.error));
                    }
                } catch (error) {
                    clearInterval(this.pollInterval);
                    reject(error);
                }
            }, interval * 1000);
        });
    }
    
    // Stop polling
    stopPolling() {
        if (this.pollInterval) {
            clearInterval(this.pollInterval);
            this.pollInterval = null;
        }
    }
    
    // Complete authentication flow
    async authenticate() {
        try {
            // Start device login
            const loginData = await this.startDeviceLogin();
            
            // Display instructions to user
            this.displayAuthInstructions(loginData);
            
            // Poll for token
            const tokenData = await this.pollForToken(loginData.deviceCode, loginData.interval);
            
            // Save the token
            this.saveToken(tokenData);
            
            if (this.onAuthSuccess) {
                this.onAuthSuccess(tokenData);
            }
            
            return tokenData;
        } catch (error) {
            if (this.onAuthError) {
                this.onAuthError(error);
            }
            throw error;
        }
    }
    
    // Display authentication instructions
    displayAuthInstructions(loginData) {
        const message = `
🔐 Icosa Gallery Authentication Required

To authenticate with Icosa Gallery:

1. Visit: ${loginData.verificationUrl}
2. Enter code: ${loginData.userCode}
3. Sign in with your Icosa Gallery account
4. Authorize the application

Waiting for authentication... (expires in ${Math.floor(loginData.expiresIn / 60)} minutes)
        `;
        
        console.log(message);
        
        // Create UI notification if in browser
        if (typeof window !== 'undefined') {
            this.showAuthModal(loginData);
        }
    }
    
    // Show authentication modal in browser
    showAuthModal(loginData) {
        // Remove existing modal if any
        const existingModal = document.getElementById('icosa-auth-modal');
        if (existingModal) {
            existingModal.remove();
        }
        
        // Create modal HTML
        const modalHTML = `
            <div id="icosa-auth-modal" style="
                position: fixed;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
                background: rgba(20, 20, 20, 0.95);
                backdrop-filter: blur(10px);
                border: 1px solid rgba(255, 255, 255, 0.2);
                border-radius: 12px;
                padding: 30px;
                z-index: 10000;
                color: white;
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                max-width: 400px;
                box-shadow: 0 10px 40px rgba(0, 0, 0, 0.5);
            ">
                <h2 style="margin-top: 0; color: #FF6B6B;">🔐 Icosa Gallery Authentication</h2>
                
                <p>To access Icosa Gallery's full API:</p>
                
                <ol style="line-height: 1.8;">
                    <li>Click the button below or visit:<br>
                        <a href="${loginData.verificationUrl}" target="_blank" style="color: #4a9eff; word-break: break-all;">
                            ${loginData.verificationUrl}
                        </a>
                    </li>
                    <li>Enter this code:<br>
                        <code style="
                            display: inline-block;
                            margin: 10px 0;
                            padding: 10px 20px;
                            background: rgba(255, 255, 255, 0.1);
                            border-radius: 6px;
                            font-size: 24px;
                            letter-spacing: 2px;
                            user-select: all;
                        ">${loginData.userCode}</code>
                    </li>
                    <li>Sign in and authorize the application</li>
                </ol>
                
                <button onclick="window.open('${loginData.verificationUrl}', '_blank')" style="
                    width: 100%;
                    padding: 12px;
                    margin-top: 20px;
                    background: #FF6B6B;
                    border: none;
                    border-radius: 6px;
                    color: white;
                    font-size: 16px;
                    cursor: pointer;
                    transition: background 0.3s;
                ">Open Icosa Gallery</button>
                
                <button onclick="document.getElementById('icosa-auth-modal').remove()" style="
                    width: 100%;
                    padding: 12px;
                    margin-top: 10px;
                    background: transparent;
                    border: 1px solid rgba(255, 255, 255, 0.3);
                    border-radius: 6px;
                    color: white;
                    font-size: 16px;
                    cursor: pointer;
                    transition: all 0.3s;
                ">Cancel</button>
                
                <p style="
                    margin-top: 20px;
                    margin-bottom: 0;
                    font-size: 12px;
                    color: #aaa;
                    text-align: center;
                ">Waiting for authentication...</p>
            </div>
        `;
        
        // Add modal to page
        const modalDiv = document.createElement('div');
        modalDiv.innerHTML = modalHTML;
        document.body.appendChild(modalDiv.firstElementChild);
    }
    
    // Save token to storage
    saveToken(tokenData) {
        try {
            // Save to localStorage for persistence
            if (typeof window !== 'undefined' && window.localStorage) {
                const authData = {
                    token: tokenData.accessToken,
                    tokenType: tokenData.tokenType,
                    expiresAt: Date.now() + (tokenData.expiresIn * 1000),
                    refreshToken: tokenData.refreshToken
                };
                
                localStorage.setItem('icosa_auth', JSON.stringify(authData));
            }
            
            // Update AuthConfig in memory
            if (typeof window !== 'undefined' && window.AuthConfig) {
                window.AuthConfig.icosaGallery.jwtToken = tokenData.accessToken;
                window.AuthConfig.icosaGallery.tokenExpiry = Date.now() + (tokenData.expiresIn * 1000);
            }
            
            console.log('✅ Icosa Gallery authentication successful!');
            console.log('Token saved and will expire in', Math.floor(tokenData.expiresIn / 3600), 'hours');
            
        } catch (error) {
            console.error('Failed to save token:', error);
        }
    }
    
    // Load saved token
    static loadSavedToken() {
        try {
            if (typeof window !== 'undefined' && window.localStorage) {
                const authData = JSON.parse(localStorage.getItem('icosa_auth') || '{}');
                
                if (authData.token && authData.expiresAt > Date.now()) {
                    // Token is still valid
                    return {
                        token: authData.token,
                        tokenType: authData.tokenType,
                        expiresAt: authData.expiresAt,
                        refreshToken: authData.refreshToken
                    };
                } else if (authData.token) {
                    console.log('Icosa Gallery token expired');
                }
            }
        } catch (error) {
            console.error('Failed to load saved token:', error);
        }
        
        return null;
    }
    
    // Clear saved token
    static clearToken() {
        if (typeof window !== 'undefined' && window.localStorage) {
            localStorage.removeItem('icosa_auth');
        }
        
        if (typeof window !== 'undefined' && window.AuthConfig) {
            window.AuthConfig.icosaGallery.jwtToken = '';
            window.AuthConfig.icosaGallery.tokenExpiry = null;
        }
    }
}