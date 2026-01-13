import * as THREE from 'three';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';
import { OBJLoader } from 'three/examples/jsm/loaders/OBJLoader.js';
import { FBXLoader } from 'three/examples/jsm/loaders/FBXLoader.js';
import { USDZLoader } from 'three/examples/jsm/loaders/USDZLoader.js';
import { DRACOLoader } from 'three/examples/jsm/loaders/DRACOLoader.js';
import { RGBELoader } from 'three/examples/jsm/loaders/RGBELoader.js';
// import ForceGraph3D from '3d-force-graph';
import { gsap } from 'gsap';
import { VisualizationManager } from './visualization/VisualizationManager.js';
import { StreamingDataManager } from './data/StreamingDataManager.js';
import { StressTestData } from './data/StressTestData.js';
import { ModelLoader } from './loaders/ModelLoader.js';
import { UIController } from './ui/UIController.js';
import { DynamicSearchUI } from './ui/DynamicSearchUI.js';
import './ui/AuthUI.js'; // Initialize authentication UI

class CosmosVisualizer {
    constructor() {
        this.scene = null;
        this.camera = null;
        this.renderer = null;
        this.controls = null;
        this.raycaster = new THREE.Raycaster();
        this.mouse = new THREE.Vector2();
        
        this.currentMode = 'graph';
        this.forceGraph = null;
        
        // Managers
        this.visualizationManager = null;
        this.dataManager = new StreamingDataManager();
        this.modelLoader = new ModelLoader();
        this.uiController = new UIController(this);
        this.dynamicSearchUI = null;
        
        // Search state management
        this.activeSearchHandle = null;
        this.isUpdatingVisualization = false;
        this.lastGraphData = null;
        this.isRunningDiagnostics = false;
        
        this.init();
    }
    
    async init() {
        console.log('Initializing Cosmos Visualizer...');
        try {
            await this.setupScene();
            console.log('Scene setup complete');
            
            this.setupManagers();
            this.setupDynamicSearch();
            console.log('Managers setup complete');
            
            this.setupEventListeners();
            console.log('Event listeners setup complete');
            
            this.animate();
            console.log('Animation started');
            
            // Load initial data if available
            await this.loadInitialData();
            console.log('Initial data loaded');
        } catch (error) {
            console.error('Initialization error:', error);
        }
    }
    
    async setupScene() {
        console.log('Setting up scene...');
        // Scene
        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x0a0a0a); // Slightly lighter than black
        this.scene.fog = new THREE.Fog(0x0a0a0a, 100, 1000);
        
        // Camera
        this.camera = new THREE.PerspectiveCamera(
            75,
            window.innerWidth / window.innerHeight,
            0.1,
            2000
        );
        this.camera.position.set(0, 50, 100);
        
        // Renderer with WebGPU support check
        const canvas = document.getElementById('canvas');
        
        if (navigator.gpu) {
            // WebGPU is available
            console.log('WebGPU detected, using WebGPU renderer');
            // Would use WebGPURenderer here when Three.js fully supports it
            this.renderer = new THREE.WebGLRenderer({ 
                canvas, 
                antialias: true,
                powerPreference: 'high-performance'
            });
        } else {
            // Fallback to WebGL
            this.renderer = new THREE.WebGLRenderer({ 
                canvas, 
                antialias: true 
            });
        }
        
        this.renderer.setSize(window.innerWidth, window.innerHeight);
        this.renderer.setPixelRatio(window.devicePixelRatio);
        this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
        this.renderer.toneMappingExposure = 1;
        
        // Controls
        this.controls = new OrbitControls(this.camera, this.renderer.domElement);
        this.controls.enableDamping = true;
        this.controls.dampingFactor = 0.05;
        
        // Lights
        const ambientLight = new THREE.AmbientLight(0x404040, 0.5);
        this.scene.add(ambientLight);
        
        const directionalLight = new THREE.DirectionalLight(0xffffff, 1);
        directionalLight.position.set(50, 100, 50);
        directionalLight.castShadow = true;
        directionalLight.shadow.mapSize.width = 2048;
        directionalLight.shadow.mapSize.height = 2048;
        this.scene.add(directionalLight);
        
        // Add starfield
        await this.createStarfield();
        
        // Load HDRI environment
        await this.loadEnvironment();
    }
    
    async createStarfield() {
        const starsGeometry = new THREE.BufferGeometry();
        const starsMaterial = new THREE.PointsMaterial({
            color: 0xFFFFFF,
            size: 0.7,
            transparent: true,
            opacity: 0.8
        });
        
        const starsVertices = [];
        for (let i = 0; i < 10000; i++) {
            const x = (Math.random() - 0.5) * 2000;
            const y = (Math.random() - 0.5) * 2000;
            const z = (Math.random() - 0.5) * 2000;
            starsVertices.push(x, y, z);
        }
        
        starsGeometry.setAttribute('position', new THREE.Float32BufferAttribute(starsVertices, 3));
        const stars = new THREE.Points(starsGeometry, starsMaterial);
        this.scene.add(stars);
    }
    
    async loadEnvironment() {
        const rgbeLoader = new RGBELoader();
        try {
            const texture = await rgbeLoader.loadAsync('https://dl.polyhaven.org/file/ph-assets/HDRIs/hdr/1k/moonless_golf_1k.hdr');
            texture.mapping = THREE.EquirectangularReflectionMapping;
            this.scene.environment = texture;
        } catch (error) {
            console.warn('Failed to load HDRI environment:', error);
        }
    }
    
    setupManagers() {
        this.visualizationManager = new VisualizationManager(this.scene, this.camera);
        
        // Setup model loader callbacks
        this.modelLoader.onProgress = (progress) => {
            this.uiController.updateLoadingProgress(progress);
        };
        
        this.modelLoader.onComplete = (model) => {
            this.scene.add(model);
            this.uiController.hideLoading();
        };
    }
    
    setupDynamicSearch() {
        // Initialize dynamic search UI after DOM is ready
        setTimeout(() => {
            this.dynamicSearchUI = new DynamicSearchUI(this);
            console.log('Dynamic search UI initialized');
            
            // Add auth section to sidebar after dynamic search is ready
            if (window.authUI) {
                window.authUI.addAuthButtons();
                console.log('Auth section added to UI');
            }
        }, 100);
    }
    
    setupEventListeners() {
        // Window resize
        window.addEventListener('resize', () => this.onWindowResize());
        
        // Mouse events for interaction
        this.renderer.domElement.addEventListener('mousemove', (e) => this.onMouseMove(e));
        this.renderer.domElement.addEventListener('click', (e) => this.onMouseClick(e));
        
        // Keyboard navigation
        document.addEventListener('keydown', (e) => this.onKeyDown(e));
        
        // UI events are handled by UIController
    }
    
    onWindowResize() {
        this.camera.aspect = window.innerWidth / window.innerHeight;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(window.innerWidth, window.innerHeight);
        
        if (this.forceGraph) {
            this.forceGraph.width(window.innerWidth);
            this.forceGraph.height(window.innerHeight);
        }
    }
    
    onMouseMove(event) {
        this.mouse.x = (event.clientX / window.innerWidth) * 2 - 1;
        this.mouse.y = -(event.clientY / window.innerHeight) * 2 + 1;
        
        // Update tooltip
        this.updateTooltip(event);
    }
    
    onMouseClick(event) {
        this.raycaster.setFromCamera(this.mouse, this.camera);
        const intersects = this.raycaster.intersectObjects(this.scene.children, true);
        
        if (intersects.length > 0) {
            const object = intersects[0].object;
            const point = intersects[0].point;
            
            if (object.userData && object.userData.nodeData) {
                // Store selected node for keyboard navigation
                this.selectedNode = object;
                this.selectedNodeData = object.userData.nodeData;
                
                // Zoom to node first
                this.zoomToNode(object, point, object.userData.nodeData);
                
                // Handle click action after zoom
                setTimeout(() => {
                    this.handleNodeClick(object.userData.nodeData);
                }, 1000);
            }
        }
    }
    
    zoomToNode(object, clickPoint, nodeData) {
        console.log('Zooming to node:', nodeData.name);
        
        // Calculate target camera position
        const objectPosition = object.position.clone();
        if (object.parent) {
            object.parent.localToWorld(objectPosition);
        }
        
        // Use click point for more accurate positioning
        const targetPoint = clickPoint || objectPosition;
        
        // Calculate zoom distance based on object size
        const box = new THREE.Box3().setFromObject(object);
        const size = box.getSize(new THREE.Vector3());
        const maxDim = Math.max(size.x, size.y, size.z);
        const distance = Math.max(maxDim * 3, 10); // At least 10 units away
        
        // Calculate camera position
        const direction = this.camera.position.clone().sub(targetPoint).normalize();
        const targetCameraPos = targetPoint.clone().add(direction.multiplyScalar(distance));
        
        // Animate camera
        gsap.to(this.camera.position, {
            x: targetCameraPos.x,
            y: targetCameraPos.y,
            z: targetCameraPos.z,
            duration: 1,
            ease: 'power2.inOut',
            onUpdate: () => {
                this.camera.lookAt(targetPoint);
                this.controls.target.copy(targetPoint);
            },
            onComplete: () => {
                // Show detailed info panel
                this.showNodeDetails(nodeData);
            }
        });
        
        // Highlight the selected node
        this.highlightNode(object);
    }
    
    highlightNode(object) {
        // Remove previous highlight
        if (this.highlightedObject) {
            if (this.highlightedObject.userData.originalMaterial) {
                this.highlightedObject.material = this.highlightedObject.userData.originalMaterial;
            }
            if (this.highlightRing) {
                this.scene.remove(this.highlightRing);
            }
        }
        
        // Store original material
        if (object.material) {
            object.userData.originalMaterial = object.material;
            
            // Create highlighted material
            const highlightMaterial = object.material.clone();
            highlightMaterial.emissive = new THREE.Color(0xffff00);
            highlightMaterial.emissiveIntensity = 0.3;
            object.material = highlightMaterial;
        }
        
        // Add selection ring
        const geometry = new THREE.RingGeometry(5, 6, 32);
        const material = new THREE.MeshBasicMaterial({ 
            color: 0xffff00, 
            side: THREE.DoubleSide,
            transparent: true,
            opacity: 0.5
        });
        this.highlightRing = new THREE.Mesh(geometry, material);
        
        // Position ring at object
        const objectWorldPos = new THREE.Vector3();
        object.getWorldPosition(objectWorldPos);
        this.highlightRing.position.copy(objectWorldPos);
        this.highlightRing.lookAt(this.camera.position);
        
        this.scene.add(this.highlightRing);
        
        // Animate ring
        gsap.to(this.highlightRing.scale, {
            x: 1.5,
            y: 1.5,
            z: 1.5,
            duration: 1,
            repeat: -1,
            yoyo: true,
            ease: 'power2.inOut'
        });
        
        this.highlightedObject = object;
    }
    
    showNodeDetails(nodeData) {
        // Create or update details panel
        let detailsPanel = document.getElementById('nodeDetailsPanel');
        if (!detailsPanel) {
            detailsPanel = document.createElement('div');
            detailsPanel.id = 'nodeDetailsPanel';
            detailsPanel.style.cssText = `
                position: absolute;
                top: 20px;
                right: 20px;
                width: 300px;
                max-height: 80vh;
                background: rgba(0,0,0,0.9);
                backdrop-filter: blur(10px);
                border: 1px solid rgba(255,255,255,0.2);
                border-radius: 12px;
                padding: 20px;
                color: white;
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                overflow-y: auto;
                z-index: 100;
                animation: slideIn 0.3s ease-out;
            `;
            document.body.appendChild(detailsPanel);
            
            // Add close button
            const closeBtn = document.createElement('button');
            closeBtn.style.cssText = `
                position: absolute;
                top: 10px;
                right: 10px;
                background: transparent;
                border: none;
                color: white;
                font-size: 24px;
                cursor: pointer;
                padding: 5px;
            `;
            closeBtn.innerHTML = '×';
            closeBtn.onclick = () => this.hideNodeDetails();
            detailsPanel.appendChild(closeBtn);
        }
        
        // Build details content
        const icon = this.getMediaTypeIcon(nodeData.type);
        let content = `
            <h3 style="margin-top: 0; margin-bottom: 15px; font-size: 18px;">
                <span style="font-size: 24px; margin-right: 8px;">${icon}</span>
                ${nodeData.name || 'Untitled'}
            </h3>
            <div style="font-size: 14px; line-height: 1.6;">
        `;
        
        // Add type badge with source color
        content += `
            <div style="display: inline-block; padding: 4px 12px; background: ${this.getSourceColor(nodeData.source)}; 
                        border-radius: 20px; font-size: 12px; margin-bottom: 15px; color: white; font-weight: 500;">
                ${nodeData.source.toUpperCase()} / ${nodeData.type || 'item'}
            </div>
        `;
        
        // Add description
        if (nodeData.description) {
            content += `<p style="margin: 10px 0; color: #ccc;">${nodeData.description}</p>`;
        }
        
        // Add metadata
        content += `<div style="margin-top: 15px; padding-top: 15px; border-top: 1px solid #333;">`;
        
        if (nodeData.artist) {
            content += `<div style="margin: 5px 0;"><strong>Artist:</strong> ${nodeData.artist}</div>`;
        }
        
        if (nodeData.stars !== undefined) {
            content += `<div style="margin: 5px 0;"><strong>⭐ Stars:</strong> ${nodeData.stars.toLocaleString()}</div>`;
        }
        
        if (nodeData.forks !== undefined) {
            content += `<div style="margin: 5px 0;"><strong>🍴 Forks:</strong> ${nodeData.forks.toLocaleString()}</div>`;
        }
        
        if (nodeData.watchers !== undefined) {
            content += `<div style="margin: 5px 0;"><strong>👁️ Watchers:</strong> ${nodeData.watchers.toLocaleString()}</div>`;
        }
        
        if (nodeData.openIssues !== undefined) {
            content += `<div style="margin: 5px 0;"><strong>📝 Open Issues:</strong> ${nodeData.openIssues.toLocaleString()}</div>`;
        }
        
        if (nodeData.language) {
            content += `<div style="margin: 5px 0;"><strong>Language:</strong> ${nodeData.language}</div>`;
        }
        
        if (nodeData.polyCount) {
            content += `<div style="margin: 5px 0;"><strong>Polygons:</strong> ${nodeData.polyCount.toLocaleString()}</div>`;
        }
        
        if (nodeData.size) {
            content += `<div style="margin: 5px 0;"><strong>Size:</strong> ${nodeData.size}</div>`;
        }
        
        if (nodeData.path) {
            content += `<div style="margin: 5px 0; word-break: break-all;"><strong>Path:</strong> <code style="font-size: 11px; background: #333; padding: 2px 5px; border-radius: 3px;">${nodeData.path}</code></div>`;
        }
        
        if (nodeData.tags && nodeData.tags.length > 0) {
            content += `<div style="margin: 10px 0;"><strong>Tags:</strong><br/>`;
            nodeData.tags.forEach(tag => {
                content += `<span style="display: inline-block; margin: 2px; padding: 2px 8px; background: #444; border-radius: 12px; font-size: 12px;">${tag}</span>`;
            });
            content += `</div>`;
        }
        
        if (nodeData.topics && nodeData.topics.length > 0) {
            content += `<div style="margin: 10px 0;"><strong>Topics:</strong><br/>`;
            nodeData.topics.forEach(topic => {
                content += `<span style="display: inline-block; margin: 2px; padding: 2px 8px; background: #444; border-radius: 12px; font-size: 12px;">${topic}</span>`;
            });
            content += `</div>`;
        }
        
        content += `</div>`;
        
        // Add action buttons
        content += `<div style="margin-top: 20px; display: flex; gap: 10px;">`;
        
        if (nodeData.url) {
            content += `<button onclick="window.open('${nodeData.url}', '_blank')" style="
                flex: 1;
                padding: 8px 16px;
                background: #0066ff;
                border: none;
                border-radius: 6px;
                color: white;
                cursor: pointer;
                font-size: 14px;
            ">Open Link</button>`;
        }
        
        if (nodeData.modelUrl) {
            content += `<button onclick="window.cosmosVisualizer.loadModelInViewer('${nodeData.modelUrl}')" style="
                flex: 1;
                padding: 8px 16px;
                background: #ff0066;
                border: none;
                border-radius: 6px;
                color: white;
                cursor: pointer;
                font-size: 14px;
            ">View 3D Model</button>`;
        }
        
        content += `</div></div>`;
        
        // Add CSS animation if not already present
        if (!document.getElementById('nodeDetailStyles')) {
            const style = document.createElement('style');
            style.id = 'nodeDetailStyles';
            style.innerHTML = `
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
                
                #nodeDetailsPanel::-webkit-scrollbar {
                    width: 8px;
                }
                
                #nodeDetailsPanel::-webkit-scrollbar-track {
                    background: rgba(255,255,255,0.1);
                    border-radius: 4px;
                }
                
                #nodeDetailsPanel::-webkit-scrollbar-thumb {
                    background: rgba(255,255,255,0.3);
                    border-radius: 4px;
                }
            `;
            document.head.appendChild(style);
        }
        
        detailsPanel.innerHTML = content;
        
        // Re-add close button after innerHTML update
        const closeBtn = document.createElement('button');
        closeBtn.style.cssText = `
            position: absolute;
            top: 10px;
            right: 10px;
            background: transparent;
            border: none;
            color: white;
            font-size: 24px;
            cursor: pointer;
            padding: 5px;
        `;
        closeBtn.innerHTML = '×';
        closeBtn.onclick = () => this.hideNodeDetails();
        detailsPanel.appendChild(closeBtn);
    }
    
    hideNodeDetails() {
        const detailsPanel = document.getElementById('nodeDetailsPanel');
        if (detailsPanel) {
            detailsPanel.style.animation = 'slideIn 0.3s ease-out reverse';
            setTimeout(() => {
                detailsPanel.remove();
            }, 300);
        }
        
        // Remove highlight
        if (this.highlightedObject && this.highlightedObject.userData.originalMaterial) {
            this.highlightedObject.material = this.highlightedObject.userData.originalMaterial;
        }
        
        if (this.highlightRing) {
            this.scene.remove(this.highlightRing);
            this.highlightRing = null;
        }
        
        // Clear selection
        this.selectedNode = null;
        this.selectedNodeData = null;
    }
    
    onKeyDown(event) {
        switch(event.key) {
            case 'Escape':
                // Close details panel and reset camera
                this.hideNodeDetails();
                this.resetCamera();
                break;
                
            case 'ArrowLeft':
            case 'ArrowRight':
            case 'ArrowUp':
            case 'ArrowDown':
                // Navigate between nodes
                this.navigateNodes(event.key);
                break;
                
            case 'Enter':
                // Activate selected node
                if (this.selectedNodeData) {
                    this.handleNodeClick(this.selectedNodeData);
                }
                break;
                
            case ' ':
                // Spacebar - toggle detail panel
                event.preventDefault();
                if (this.selectedNodeData) {
                    const panel = document.getElementById('nodeDetailsPanel');
                    if (panel) {
                        this.hideNodeDetails();
                    } else {
                        this.showNodeDetails(this.selectedNodeData);
                    }
                }
                break;
                
            case 'Tab':
                // Tab through visible nodes
                event.preventDefault();
                this.selectNextVisibleNode(event.shiftKey);
                break;
                
            case '+':  
            case '=':
                // Zoom in
                this.zoomIn();
                break;
                
            case '-':
            case '_':
                // Zoom out
                this.zoomOut();
                break;
        }
    }
    
    resetCamera() {
        // Animate camera back to default position
        gsap.to(this.camera.position, {
            x: 0,
            y: 50,
            z: 100,
            duration: 1.5,
            ease: 'power2.inOut',
            onUpdate: () => {
                this.camera.lookAt(0, 0, 0);
                this.controls.target.set(0, 0, 0);
            }
        });
    }
    
    navigateNodes(direction) {
        if (!this.selectedNode) return;
        
        // Get all nodes with data
        const nodes = [];
        this.scene.traverse((child) => {
            if (child.userData && child.userData.nodeData) {
                nodes.push(child);
            }
        });
        
        if (nodes.length === 0) return;
        
        // Find current node index
        const currentIndex = nodes.indexOf(this.selectedNode);
        if (currentIndex === -1) return;
        
        let newIndex = currentIndex;
        
        // Calculate new index based on direction
        switch(direction) {
            case 'ArrowLeft':
                newIndex = (currentIndex - 1 + nodes.length) % nodes.length;
                break;
            case 'ArrowRight':
                newIndex = (currentIndex + 1) % nodes.length;
                break;
            case 'ArrowUp':
                // Find node above in 3D space
                newIndex = this.findNodeInDirection(nodes, currentIndex, new THREE.Vector3(0, 1, 0));
                break;
            case 'ArrowDown':
                // Find node below in 3D space
                newIndex = this.findNodeInDirection(nodes, currentIndex, new THREE.Vector3(0, -1, 0));
                break;
        }
        
        // Select new node
        if (newIndex !== currentIndex && nodes[newIndex]) {
            const newNode = nodes[newIndex];
            this.selectedNode = newNode;
            this.selectedNodeData = newNode.userData.nodeData;
            
            // Get world position of new node
            const worldPos = new THREE.Vector3();
            newNode.getWorldPosition(worldPos);
            
            // Zoom to new node
            this.zoomToNode(newNode, worldPos, this.selectedNodeData);
        }
    }
    
    findNodeInDirection(nodes, currentIndex, direction) {
        const currentNode = nodes[currentIndex];
        const currentPos = new THREE.Vector3();
        currentNode.getWorldPosition(currentPos);
        
        let bestNode = currentIndex;
        let bestScore = -Infinity;
        
        for (let i = 0; i < nodes.length; i++) {
            if (i === currentIndex) continue;
            
            const nodePos = new THREE.Vector3();
            nodes[i].getWorldPosition(nodePos);
            
            const toNode = nodePos.clone().sub(currentPos).normalize();
            const score = toNode.dot(direction);
            
            if (score > bestScore && score > 0.5) {
                bestScore = score;
                bestNode = i;
            }
        }
        
        return bestNode;
    }
    
    selectNextVisibleNode(reverse = false) {
        // Get visible nodes in camera frustum
        const frustum = new THREE.Frustum();
        const cameraMatrix = new THREE.Matrix4().multiplyMatrices(
            this.camera.projectionMatrix,
            this.camera.matrixWorldInverse
        );
        frustum.setFromProjectionMatrix(cameraMatrix);
        
        const visibleNodes = [];
        this.scene.traverse((child) => {
            if (child.userData && child.userData.nodeData) {
                const worldPos = new THREE.Vector3();
                child.getWorldPosition(worldPos);
                if (frustum.containsPoint(worldPos)) {
                    visibleNodes.push(child);
                }
            }
        });
        
        if (visibleNodes.length === 0) return;
        
        // Sort by distance to camera
        const cameraPos = this.camera.position.clone();
        visibleNodes.sort((a, b) => {
            const aPos = new THREE.Vector3();
            const bPos = new THREE.Vector3();
            a.getWorldPosition(aPos);
            b.getWorldPosition(bPos);
            return aPos.distanceTo(cameraPos) - bPos.distanceTo(cameraPos);
        });
        
        // Find current index
        const currentIndex = this.selectedNode ? visibleNodes.indexOf(this.selectedNode) : -1;
        
        let nextIndex;
        if (reverse) {
            nextIndex = currentIndex > 0 ? currentIndex - 1 : visibleNodes.length - 1;
        } else {
            nextIndex = currentIndex < visibleNodes.length - 1 ? currentIndex + 1 : 0;
        }
        
        // Select next node
        const nextNode = visibleNodes[nextIndex];
        if (nextNode) {
            this.selectedNode = nextNode;
            this.selectedNodeData = nextNode.userData.nodeData;
            
            const worldPos = new THREE.Vector3();
            nextNode.getWorldPosition(worldPos);
            this.zoomToNode(nextNode, worldPos, this.selectedNodeData);
        }
    }
    
    zoomIn() {
        const currentDistance = this.camera.position.length();
        const newDistance = Math.max(currentDistance * 0.8, 10); // Min distance 10
        
        const direction = this.camera.position.clone().normalize();
        const newPosition = direction.multiplyScalar(newDistance);
        
        gsap.to(this.camera.position, {
            x: newPosition.x,
            y: newPosition.y,
            z: newPosition.z,
            duration: 0.5,
            ease: 'power2.inOut'
        });
    }
    
    zoomOut() {
        const currentDistance = this.camera.position.length();
        const newDistance = Math.min(currentDistance * 1.25, 500); // Max distance 500
        
        const direction = this.camera.position.clone().normalize();
        const newPosition = direction.multiplyScalar(newDistance);
        
        gsap.to(this.camera.position, {
            x: newPosition.x,
            y: newPosition.y,
            z: newPosition.z,
            duration: 0.5,
            ease: 'power2.inOut'
        });
    }
    
    getSourceColor(source) {
        const colors = {
            icosa: '#FF6B6B',      // Casa Gallery - Red/Pink
            objaverse: '#4ECDC4',   // Objaverse - Teal
            github: '#95E1D3',      // GitHub - Light Green
            local: '#F38181',       // Local Files - Coral
            web: '#AA96DA',         // Web Search - Purple
            casa: '#FF6B6B',        // Casa Gallery alias
            polli: '#FFB6E1',       // Polli - Light Pink
            sketchfab: '#1CAAD9'    // Sketchfab - Blue
        };
        return colors[source] || '#888888';
    }
    
    getMediaTypeIcon(type) {
        const icons = {
            // Media types
            'image': '🖼️',
            'video': '🎬',
            '3d_object': '🎮',
            'model': '🎮',
            'artwork': '🎨',
            'person': '👤',
            'user': '👤',
            'artist': '🎨',
            'place': '📍',
            'repository': '📚',
            'file': '📄',
            'folder': '📁',
            'project': '🏗️',
            'unity_project': '🎮',
            'web_project': '🌐',
            'ai_project': '🤖',
            'ai_service': '🧠',
            'data_file': '💾',
            'media_file': '🎞️',
            'document': '📝',
            'documentation': '📚',
            'specification': '📋'
        };
        return icons[type] || '⭐';
    }
    
    loadModelInViewer(modelUrl) {
        // Switch to Icosa viewer and load the model
        this.switchMode('icosa');
        
        // Send model URL to iframe
        setTimeout(() => {
            const iframe = document.getElementById('icosaFrame');
            if (iframe && iframe.contentWindow) {
                iframe.contentWindow.postMessage({
                    modelUrl: modelUrl
                }, '*');
            }
        }, 500);
    }
    
    updateTooltip(event) {
        const tooltip = document.getElementById('tooltip');
        this.raycaster.setFromCamera(this.mouse, this.camera);
        const intersects = this.raycaster.intersectObjects(this.scene.children, true);
        
        if (intersects.length > 0 && intersects[0].object.userData.nodeData) {
            const data = intersects[0].object.userData.nodeData;
            tooltip.style.display = 'block';
            tooltip.style.left = event.clientX + 10 + 'px';
            tooltip.style.top = event.clientY + 10 + 'px';
            tooltip.textContent = data.name || data.id;
        } else {
            tooltip.style.display = 'none';
        }
    }
    
    async handleNodeClick(nodeData) {
        console.log('Node clicked:', nodeData);
        
        if (nodeData.modelUrl) {
            // Option 1: Load in scene
            if (this.currentMode === 'graph') {
                this.uiController.showLoading();
                try {
                    const model = await this.modelLoader.load(nodeData.modelUrl, nodeData.format);
                    this.displayModel(model);
                } catch (error) {
                    console.error('Failed to load model:', error);
                    this.uiController.hideLoading();
                }
            } 
            // Option 2: Load in Icosa viewer
            else if (this.currentMode === 'icosa') {
                const icosaFrame = document.getElementById('icosaFrame');
                if (icosaFrame && icosaFrame.contentWindow) {
                    icosaFrame.contentWindow.postMessage({
                        modelUrl: nodeData.modelUrl
                    }, '*');
                }
            }
        } else if (nodeData.url) {
            window.open(nodeData.url, '_blank');
        }
    }
    
    displayModel(model) {
        // Clear previous models
        this.clearModels();
        
        // Center and scale model
        const box = new THREE.Box3().setFromObject(model);
        const center = box.getCenter(new THREE.Vector3());
        const size = box.getSize(new THREE.Vector3());
        
        const maxDim = Math.max(size.x, size.y, size.z);
        const scale = 50 / maxDim;
        
        model.scale.multiplyScalar(scale);
        model.position.sub(center.multiplyScalar(scale));
        
        this.scene.add(model);
        
        // Animate camera to focus on model
        gsap.to(this.camera.position, {
            x: 0,
            y: 25,
            z: 75,
            duration: 1.5,
            ease: 'power2.inOut',
            onUpdate: () => this.camera.lookAt(0, 0, 0)
        });
    }
    
    clearModels() {
        const modelsToRemove = [];
        this.scene.traverse((child) => {
            if (child.userData.isLoadedModel) {
                modelsToRemove.push(child);
            }
        });
        
        modelsToRemove.forEach(model => {
            this.scene.remove(model);
            if (model.geometry) model.geometry.dispose();
            if (model.material) {
                if (Array.isArray(model.material)) {
                    model.material.forEach(mat => mat.dispose());
                } else {
                    model.material.dispose();
                }
            }
        });
    }
    
    async performSearch(query, sources, limit = 1000) {
        // Use streaming search for dynamic, non-blocking results
        try {
            console.log(`🔍 Dynamic search for "${query}" in sources: ${sources.join(', ')}, limit: ${limit}`);
            
            // Cancel any previous search
            if (this.activeSearchHandle) {
                this.activeSearchHandle.cancel();
            }
            
            // Start streaming search immediately
            this.activeSearchHandle = await this.dataManager.searchStreaming(query, sources, {
                timeout: 3000, // Fast timeout for responsiveness
                limit: limit
            });
            
            let currentResults = [];
            let nodeCount = 0;
            
            // Process results as they stream in
            this.activeSearchHandle.onProgress(({ newResults, totalResults, count }) => {
                currentResults = totalResults;
                nodeCount = count;
                
                // Update visualization progressively (every 50 results for smooth UI)
                if (count % 50 === 0 || count < 50) {
                    requestAnimationFrame(() => {
                        const graphData = this.dataManager.convertToGraphData(currentResults);
                        this.updateVisualizationFast(graphData);
                        
                        // Update stats in real-time
                        this.uiController.updateStats({
                            nodes: graphData.nodes.length,
                            links: graphData.links.length,
                            status: `${count} results...`
                        });
                    });
                }
            });
            
            // Final results
            this.activeSearchHandle.onComplete(({ results, count }) => {
                console.log(`✅ Search complete: ${count} results in ${performance.now()}ms`);
                
                const graphData = this.dataManager.convertToGraphData(results);
                this.lastGraphData = graphData;
                this.updateVisualization(graphData);
                
                // Final stats update
                this.uiController.updateStats({
                    nodes: graphData.nodes.length,
                    links: graphData.links.length,
                    status: `${count} results found`
                });
            });
            
        } catch (error) {
            console.error('Search error:', error);
            this.uiController.updateStats({
                status: 'Search failed',
                error: error.message
            });
        }
    }
    
    // Fast visualization update for streaming results
    updateVisualizationFast(graphData) {
        if (this.isUpdatingVisualization) return; // Skip if already updating
        
        this.isUpdatingVisualization = true;
        
        // Use requestIdleCallback for non-blocking updates
        const updateCallback = () => {
            const layout = this.uiController.getSelectedLayout();
            
            // Only update if we have reasonable amount of data
            if (graphData.nodes.length > 0 && graphData.nodes.length < 10000) {
                switch (layout) {
                    case 'cosmos':
                        this.visualizationManager.updateCosmos(graphData);
                        break;
                    case 'city':
                        this.visualizationManager.updateCityBlocks(graphData);
                        break;
                    default:
                        this.visualizationManager.updateCosmos(graphData);
                }
            }
            
            this.isUpdatingVisualization = false;
        };
        
        if (typeof requestIdleCallback !== 'undefined') {
            requestIdleCallback(updateCallback, { timeout: 100 });
        } else {
            setTimeout(updateCallback, 16); // ~60fps
        }
    }
    
    // Stress testing methods for 1M+ nodes
    async runStressTest(nodeCount) {
        console.log(`🚀 STRESS TEST: Generating ${nodeCount.toLocaleString()} nodes`);
        this.uiController.showLoading();
        
        const startTime = performance.now();
        
        try {
            let graphData;
            
            if (nodeCount <= 100000) {
                // Use real Objaverse data for smaller tests
                graphData = await StressTestData.loadObjaverseSubset(nodeCount);
            } else {
                // Generate synthetic data for massive tests
                graphData = StressTestData.generateMassiveDataset(nodeCount);
            }
            
            const generationTime = performance.now() - startTime;
            console.log(`📊 Data generation took ${generationTime.toFixed(2)}ms`);
            
            this.lastGraphData = graphData;
            await this.updateVisualization(graphData);
            
            const totalTime = performance.now() - startTime;
            console.log(`🎯 STRESS TEST COMPLETE: ${nodeCount.toLocaleString()} nodes in ${totalTime.toFixed(2)}ms`);
            
            // Update stats
            this.uiController.updateStats({
                nodes: graphData.nodes.length,
                links: graphData.links.length,
                generationTime: `${generationTime.toFixed(0)}ms`,
                totalTime: `${totalTime.toFixed(0)}ms`
            });
            
        } catch (error) {
            console.error('Stress test error:', error);
        } finally {
            this.uiController.hideLoading();
        }
    }
    
    async runSourceDiagnostics() {
        if (this.isRunningDiagnostics) return;
        
        const sources = Object.keys(this.dataManager.searchProviders);
        const defaultQueries = {
            icosa: 'animals',
            objaverse: 'robot',
            github: 'three.js',
            local: '',
            web: 'webgl'
        };
        
        this.isRunningDiagnostics = true;
        this.uiController.showDiagnosticsStatus('Running diagnostics...');
        const diagnosticsBtn = document.getElementById('diagnosticsBtn');
        if (diagnosticsBtn) {
            diagnosticsBtn.disabled = true;
        }
        this.uiController.showDiagnostics([]);
        
        const diagnostics = [];
        for (const source of sources) {
            const query = defaultQueries[source] ?? '3d';
            const startTime = performance.now();
            let status = 'success';
            let count = 0;
            let error = null;
            
            try {
                const results = await this.dataManager.search(query, [source]);
                count = Array.isArray(results) ? results.length : 0;
            } catch (err) {
                status = 'error';
                error = err.message || String(err);
            }
            
            diagnostics.push({
                source,
                query,
                count,
                status,
                duration: Math.round(performance.now() - startTime),
                error
            });
            
            this.uiController.showDiagnostics(diagnostics);
        }
        
        this.uiController.showDiagnosticsStatus('Diagnostics complete');
        this.isRunningDiagnostics = false;
        if (diagnosticsBtn) {
            diagnosticsBtn.disabled = false;
        }
    }
    
    async updateVisualization(graphData) {
        const layout = this.uiController.getSelectedLayout();
        
        // Clear existing visualization
        this.clearVisualization();
        
        switch (layout) {
            case 'force':
                this.createForceGraph(graphData);
                break;
            case 'city':
                await this.visualizationManager.createCityBlocks(graphData);
                break;
            case 'cosmos':
                await this.visualizationManager.createCosmos(graphData);
                break;
            case 'tree':
                await this.visualizationManager.createTree(graphData);
                break;
        }
    }
    
    createForceGraph(graphData) {
        // For now, use cosmos layout as force graph alternative
        this.visualizationManager.createCosmos(graphData);
    }
    
    createNodeTexture(node) {
        const canvas = document.createElement('canvas');
        canvas.width = 128;
        canvas.height = 128;
        const ctx = canvas.getContext('2d');
        
        const colors = {
            icosa: '#FF6B6B',
            objaverse: '#4ECDC4',
            github: '#95E1D3',
            local: '#F38181',
            web: '#AA96DA'
        };
        
        const color = colors[node.type] || '#FFFFFF';
        
        // Draw circle
        ctx.beginPath();
        ctx.arc(64, 64, 50, 0, Math.PI * 2);
        ctx.fillStyle = color;
        ctx.fill();
        
        // Add text
        ctx.fillStyle = '#FFFFFF';
        ctx.font = 'bold 24px Arial';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        
        const text = node.name ? node.name.substring(0, 3).toUpperCase() : '?';
        ctx.fillText(text, 64, 64);
        
        return new THREE.CanvasTexture(canvas);
    }
    
    clearVisualization() {
        // Remove force graph if exists
        if (this.forceGraph) {
            const graphElement = document.querySelector('.force-graph-3d');
            if (graphElement) {
                graphElement.remove();
            }
            this.forceGraph = null;
        }
        
        // Clear visualization manager
        this.visualizationManager.clear();
    }
    
    switchMode(mode) {
        this.currentMode = mode;
        
        switch (mode) {
            case 'graph':
                this.clearVisualization();
                if (this.lastGraphData) {
                    this.updateVisualization(this.lastGraphData);
                }
                break;
            case 'metavido':
                this.clearVisualization();
                this.visualizationManager.showMetavidoVFX();
                break;
            case 'icosa':
                this.uiController.showIcosaViewer();
                break;
        }
    }
    
    async loadFile(file) {
        const extension = file.name.split('.').pop().toLowerCase();
        
        if (extension === 'json') {
            // Handle JSON data
            const text = await file.text();
            const data = JSON.parse(text);
            
            if (Array.isArray(data) || (data.nodes && data.links)) {
                // Graph data
                const graphData = data.nodes ? data : this.dataManager.convertToGraphData(data);
                this.lastGraphData = graphData;
                await this.updateVisualization(graphData);
            }
        } else if (['gltf', 'glb', 'obj', 'fbx', 'usdz'].includes(extension)) {
            // 3D model
            this.uiController.showLoading();
            const url = URL.createObjectURL(file);
            try {
                const model = await this.modelLoader.load(url, extension);
                this.displayModel(model);
            } catch (error) {
                console.error('Failed to load model:', error);
            } finally {
                this.uiController.hideLoading();
                URL.revokeObjectURL(url);
            }
        }
    }
    
    async loadInitialData() {
        // Check if there's sample data to load
        try {
            console.log('Loading sample data...');
            const response = await fetch('/data/sample.json');
            if (response.ok) {
                const data = await response.json();
                console.log('Sample data loaded:', data);
                this.lastGraphData = this.dataManager.convertToGraphData(data);
                await this.updateVisualization(this.lastGraphData);
            } else {
                console.log('Sample data not found, showing welcome message');
                // Show some default content
                this.showWelcome();
            }
        } catch (error) {
            console.log('Error loading initial data:', error);
            this.showWelcome();
        }
    }
    
    showWelcome() {
        // Add a simple welcome mesh
        const geometry = new THREE.BoxGeometry(10, 10, 10);
        const material = new THREE.MeshStandardMaterial({ 
            color: 0x0066ff,
            emissive: 0x0066ff,
            emissiveIntensity: 0.2
        });
        const cube = new THREE.Mesh(geometry, material);
        cube.position.set(0, 0, 0);
        this.scene.add(cube);
        
        // Animate it
        const animate = () => {
            cube.rotation.x += 0.01;
            cube.rotation.y += 0.01;
            requestAnimationFrame(animate);
        };
        animate();
    }
    
    animate() {
        requestAnimationFrame(() => this.animate());
        
        // Update controls
        this.controls.update();
        
        // Update visualization animations
        this.visualizationManager.update();
        
        // Update FPS counter
        this.uiController.updateFPS();
        
        // Render scene only if not in force graph mode
        if (this.currentMode !== 'graph' || !this.forceGraph) {
            this.renderer.render(this.scene, this.camera);
        }
    }
}

// Initialize application
window.addEventListener('DOMContentLoaded', () => {
    window.cosmosVisualizer = new CosmosVisualizer();
    window.cosmosApp = window.cosmosVisualizer; // Alias for easier access
});
