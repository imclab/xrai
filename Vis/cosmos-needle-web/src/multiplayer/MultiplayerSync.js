import { EventDispatcher } from 'three';

export class MultiplayerSync extends EventDispatcher {
    constructor(engine, networking) {
        super();
        this.engine = engine;
        this.networking = networking;
        this.userId = null;
        this.roomState = {
            searchQuery: '',
            searchSources: [],
            graphData: null,
            currentMode: 'hypergraph',
            currentLayout: 'force',
            selectedArtwork: null
        };
        
        this.init();
    }
    
    init() {
        // Register message handlers
        this.networking.on('connected', (data) => {
            this.userId = data.userId;
            console.log('Connected to multiplayer:', this.userId);
        });
        
        this.networking.on('message', (message) => {
            this.handleMessage(message);
        });
        
        this.networking.on('roomStateUpdate', (state) => {
            this.roomState = { ...this.roomState, ...state };
            this.applyRoomState();
        });
        
        // Sync user positions/avatars
        this.networking.on('userJoined', (user) => {
            this.createUserAvatar(user);
        });
        
        this.networking.on('userLeft', (userId) => {
            this.removeUserAvatar(userId);
        });
        
        this.networking.on('userPositionUpdate', (data) => {
            this.updateUserPosition(data.userId, data.position, data.rotation);
        });
    }
    
    handleMessage(message) {
        switch (message.type) {
            case 'searchResults':
                this.dispatchEvent({
                    type: 'searchResults',
                    data: message.data
                });
                break;
                
            case 'nodeClick':
                this.dispatchEvent({
                    type: 'nodeClick',
                    node: message.node
                });
                break;
                
            case 'modeChange':
                this.dispatchEvent({
                    type: 'modeChange',
                    mode: message.mode
                });
                break;
                
            case 'layoutChange':
                this.dispatchEvent({
                    type: 'layoutChange',
                    layout: message.layout
                });
                break;
                
            case 'modelLoaded':
                this.dispatchEvent({
                    type: 'modelLoaded',
                    url: message.url,
                    position: message.position
                });
                break;
        }
    }
    
    broadcastSearchResults(query, sources, graphData) {
        this.networking.send({
            type: 'searchResults',
            data: {
                query,
                sources,
                graphData
            }
        });
        
        // Update room state
        this.updateRoomState({
            searchQuery: query,
            searchSources: sources,
            graphData: graphData
        });
    }
    
    broadcastNodeClick(node) {
        this.networking.send({
            type: 'nodeClick',
            node: node
        });
    }
    
    broadcastModeChange(mode) {
        this.networking.send({
            type: 'modeChange',
            mode: mode
        });
        
        this.updateRoomState({ currentMode: mode });
    }
    
    broadcastLayoutChange(layout) {
        this.networking.send({
            type: 'layoutChange',
            layout: layout
        });
        
        this.updateRoomState({ currentLayout: layout });
    }
    
    broadcastModelLoaded(url, position) {
        this.networking.send({
            type: 'modelLoaded',
            url: url,
            position: position
        });
    }
    
    updateRoomState(updates) {
        this.roomState = { ...this.roomState, ...updates };
        this.networking.send({
            type: 'roomStateUpdate',
            state: this.roomState
        });
    }
    
    applyRoomState() {
        // Apply the current room state
        if (this.roomState.graphData) {
            this.dispatchEvent({
                type: 'searchResults',
                data: {
                    query: this.roomState.searchQuery,
                    sources: this.roomState.searchSources,
                    graphData: this.roomState.graphData
                }
            });
        }
        
        if (this.roomState.currentMode) {
            this.dispatchEvent({
                type: 'modeChange',
                mode: this.roomState.currentMode
            });
        }
        
        if (this.roomState.currentLayout) {
            this.dispatchEvent({
                type: 'layoutChange',
                layout: this.roomState.currentLayout
            });
        }
    }
    
    // Avatar management
    createUserAvatar(user) {
        // Simple avatar representation
        const geometry = new THREE.ConeGeometry(0.5, 1, 8);
        const material = new THREE.MeshStandardMaterial({
            color: this.getUserColor(user.id)
        });
        const avatar = new THREE.Mesh(geometry, material);
        avatar.position.set(0, 0, 0);
        avatar.userData = { userId: user.id, isAvatar: true };
        
        // Add name label
        // Would add TextGeometry or sprite here
        
        this.engine.scene.add(avatar);
    }
    
    removeUserAvatar(userId) {
        const avatar = this.engine.scene.children.find(
            child => child.userData.isAvatar && child.userData.userId === userId
        );
        
        if (avatar) {
            this.engine.scene.remove(avatar);
            avatar.geometry.dispose();
            avatar.material.dispose();
        }
    }
    
    updateUserPosition(userId, position, rotation) {
        const avatar = this.engine.scene.children.find(
            child => child.userData.isAvatar && child.userData.userId === userId
        );
        
        if (avatar) {
            avatar.position.copy(position);
            avatar.rotation.copy(rotation);
        }
    }
    
    getUserColor(userId) {
        // Generate consistent color from user ID
        let hash = 0;
        for (let i = 0; i < userId.length; i++) {
            hash = userId.charCodeAt(i) + ((hash << 5) - hash);
        }
        
        const hue = hash % 360;
        return new THREE.Color().setHSL(hue / 360, 0.7, 0.5);
    }
    
    // Send local player position updates
    updateLocalPosition(position, rotation) {
        this.networking.send({
            type: 'userPositionUpdate',
            userId: this.userId,
            position: position,
            rotation: rotation
        });
    }
}