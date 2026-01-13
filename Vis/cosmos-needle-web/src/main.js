import { Engine, GameObject, WebXR, SyncedTransform, PlayerSync } from '@needle-tools/engine';
import { Networking, NetworkConnection } from '@needle-tools/networking';
import * as THREE from 'three';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';
import { OBJLoader } from 'three/examples/jsm/loaders/OBJLoader.js';
import { FBXLoader } from 'three/examples/jsm/loaders/FBXLoader.js';
import { USDZLoader } from 'three/examples/jsm/loaders/USDZLoader.js';
import { DRACOLoader } from 'three/examples/jsm/loaders/DRACOLoader.js';
import ForceGraph3D from '3d-force-graph';
import * as d3 from 'd3-force-3d';
import pako from 'pako';
import { HypergraphLayout } from './layouts/HypergraphLayout.js';
import { CityBlocksLayout } from './layouts/CityBlocksLayout.js';
import { MetavidoVFX } from './components/MetavidoVFX.js';
import { SearchManager } from './data/SearchManager.js';
import { MultiplayerSync } from './multiplayer/MultiplayerSync.js';

// Initialize Needle Engine
const container = document.getElementById('needle-container');
const engine = new Engine(container);

// Initialize multiplayer
const networking = new Networking({
    url: 'wss://engine.needle.tools/socket',
    room: `cosmos-hypergraph-${Date.now()}`
});

const multiplayerSync = new MultiplayerSync(engine, networking);

// Initialize search manager
const searchManager = new SearchManager();

// Layout managers
let currentLayout = 'force';
let hypergraphLayout = new HypergraphLayout(engine.scene);
let cityBlocksLayout = new CityBlocksLayout(engine.scene);

// Model loaders
const gltfLoader = new GLTFLoader();
const dracoLoader = new DRACOLoader();
dracoLoader.setDecoderPath('https://www.gstatic.com/draco/versioned/decoders/1.5.7/');
gltfLoader.setDRACOLoader(dracoLoader);

const objLoader = new OBJLoader();
const fbxLoader = new FBXLoader();
const usdzLoader = new USDZLoader();

// Current visualization mode
let currentMode = 'hypergraph';
let metavidoVFX = null;
let icosaFrame = document.getElementById('icosa-viewer-frame');

// Graph data
let graphData = {
    nodes: [],
    links: []
};

// Initialize default scene
async function initializeScene() {
    // Add ambient light
    const ambientLight = new THREE.AmbientLight(0x404040, 0.5);
    engine.scene.add(ambientLight);
    
    // Add directional light
    const dirLight = new THREE.DirectionalLight(0xffffff, 1);
    dirLight.position.set(5, 10, 5);
    engine.scene.add(dirLight);
    
    // Add skybox with stars
    const skyGeometry = new THREE.SphereGeometry(1000, 60, 40);
    const skyMaterial = new THREE.MeshBasicMaterial({
        map: await createStarTexture(),
        side: THREE.BackSide
    });
    const skyMesh = new THREE.Mesh(skyGeometry, skyMaterial);
    engine.scene.add(skyMesh);
    
    // Initialize MetavidoVFX
    metavidoVFX = new MetavidoVFX(engine.scene);
    metavidoVFX.hide();
    
    // Setup WebXR
    const webxr = GameObject.addComponent(engine.context, WebXR);
    
    // Enable hand tracking
    webxr.handTracking = true;
    
    engine.start();
}

// Create star texture
function createStarTexture() {
    return new Promise((resolve) => {
        const canvas = document.createElement('canvas');
        canvas.width = 2048;
        canvas.height = 1024;
        const ctx = canvas.getContext('2d');
        
        // Black background
        ctx.fillStyle = '#000000';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        
        // Add stars
        for (let i = 0; i < 2000; i++) {
            const x = Math.random() * canvas.width;
            const y = Math.random() * canvas.height;
            const radius = Math.random() * 2;
            const opacity = Math.random() * 0.8 + 0.2;
            
            ctx.beginPath();
            ctx.arc(x, y, radius, 0, Math.PI * 2);
            ctx.fillStyle = `rgba(255, 255, 255, ${opacity})`;
            ctx.fill();
        }
        
        const texture = new THREE.CanvasTexture(canvas);
        resolve(texture);
    });
}

// Search functionality
async function performSearch(query, sources) {
    showLoading(true);
    
    try {
        const results = await searchManager.search(query, sources);
        graphData = convertToGraphData(results);
        updateVisualization();
        
        // Sync search results with other users
        multiplayerSync.broadcastSearchResults(query, sources, graphData);
    } catch (error) {
        console.error('Search error:', error);
    } finally {
        showLoading(false);
    }
}

// Convert search results to graph data
function convertToGraphData(results) {
    const nodes = [];
    const links = [];
    const nodeMap = new Map();
    
    results.forEach((result, idx) => {
        const node = {
            id: result.id || `node-${idx}`,
            name: result.name || result.title || 'Untitled',
            type: result.source,
            data: result,
            val: result.relevance || 1
        };
        
        nodes.push(node);
        nodeMap.set(node.id, node);
        
        // Create links based on relationships
        if (result.relationships) {
            result.relationships.forEach(rel => {
                if (nodeMap.has(rel.target)) {
                    links.push({
                        source: node.id,
                        target: rel.target,
                        value: rel.strength || 1
                    });
                }
            });
        }
    });
    
    return { nodes, links };
}

// Update visualization based on current layout
function updateVisualization() {
    // Clear existing visualization
    clearVisualization();
    
    switch (currentLayout) {
        case 'force':
            createForceGraph();
            break;
        case 'city':
            cityBlocksLayout.generate(graphData);
            break;
        case 'cosmos':
            hypergraphLayout.generateCosmos(graphData);
            break;
        case 'tree':
            hypergraphLayout.generateTree(graphData);
            break;
    }
}

// Create force-directed graph
function createForceGraph() {
    const graph = ForceGraph3D()(container)
        .graphData(graphData)
        .nodeLabel('name')
        .nodeAutoColorBy('type')
        .nodeThreeObject(node => {
            const sprite = new THREE.Sprite(
                new THREE.SpriteMaterial({
                    map: createNodeTexture(node),
                    alphaTest: 0.5
                })
            );
            sprite.scale.set(12, 12, 1);
            return sprite;
        })
        .linkWidth(link => Math.sqrt(link.value))
        .linkOpacity(0.5)
        .onNodeClick(handleNodeClick);
    
    // Add to engine scene
    engine.scene.add(graph.scene());
}

// Create node texture based on type
function createNodeTexture(node) {
    const canvas = document.createElement('canvas');
    canvas.width = 128;
    canvas.height = 128;
    const ctx = canvas.getContext('2d');
    
    // Node color based on type
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
    
    // Add icon or text
    ctx.fillStyle = '#FFFFFF';
    ctx.font = '24px Arial';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(node.name.substring(0, 3).toUpperCase(), 64, 64);
    
    return new THREE.CanvasTexture(canvas);
}

// Handle node clicks
async function handleNodeClick(node) {
    if (node.data.modelUrl) {
        await loadModel(node.data.modelUrl, node.data.format);
    } else if (node.data.url) {
        window.open(node.data.url, '_blank');
    }
    
    // Sync node click with other users
    multiplayerSync.broadcastNodeClick(node);
}

// Load 3D model
async function loadModel(url, format) {
    showLoading(true);
    
    try {
        let loader;
        switch (format) {
            case 'gltf':
            case 'glb':
                loader = gltfLoader;
                break;
            case 'obj':
                loader = objLoader;
                break;
            case 'fbx':
                loader = fbxLoader;
                break;
            case 'usdz':
                loader = usdzLoader;
                break;
            default:
                loader = gltfLoader;
        }
        
        const model = await loader.loadAsync(url);
        const object = model.scene || model;
        
        // Position and scale model
        object.position.set(0, 0, -5);
        const box = new THREE.Box3().setFromObject(object);
        const size = box.getSize(new THREE.Vector3());
        const scale = 2 / Math.max(size.x, size.y, size.z);
        object.scale.multiplyScalar(scale);
        
        // Add to scene with sync
        const syncedObject = GameObject.addComponent(object, SyncedTransform);
        engine.scene.add(object);
        
    } catch (error) {
        console.error('Model loading error:', error);
    } finally {
        showLoading(false);
    }
}

// Clear visualization
function clearVisualization() {
    // Remove force graph if exists
    const forceGraphElement = container.querySelector('.force-graph-3d');
    if (forceGraphElement) {
        forceGraphElement.remove();
    }
    
    // Clear layout groups
    hypergraphLayout.clear();
    cityBlocksLayout.clear();
}

// Mode switching
function switchMode(mode) {
    currentMode = mode;
    
    // Hide all modes
    clearVisualization();
    metavidoVFX?.hide();
    icosaFrame.style.display = 'none';
    
    switch (mode) {
        case 'hypergraph':
            updateVisualization();
            break;
        case 'metavido':
            metavidoVFX?.show();
            break;
        case 'icosa':
            loadIcosaGallery();
            break;
        case 'split':
            // Split view implementation
            container.style.width = '50%';
            icosaFrame.style.display = 'block';
            icosaFrame.style.left = '50%';
            icosaFrame.style.width = '50%';
            updateVisualization();
            loadIcosaGallery();
            break;
    }
    
    // Update UI
    document.querySelectorAll('.mode-btn').forEach(btn => {
        btn.classList.toggle('active', btn.dataset.mode === mode);
    });
}

// Load Icosa gallery
async function loadIcosaGallery() {
    try {
        // Fetch random artwork from Icosa API
        const response = await fetch('https://api.icosa.foundation/artworks/random');
        const artwork = await response.json();
        
        // Load in iframe
        icosaFrame.src = `https://icosa.foundation/artworks/${artwork.id}/embed`;
        icosaFrame.style.display = 'block';
        
    } catch (error) {
        console.error('Failed to load Icosa gallery:', error);
    }
}

// File handling
function setupFileHandling() {
    const fileInput = document.getElementById('fileInput');
    const dropZone = document.getElementById('fileDropZone');
    
    fileInput.addEventListener('change', handleFileSelect);
    
    // Drag and drop
    document.addEventListener('dragover', (e) => {
        e.preventDefault();
        dropZone.classList.add('active');
    });
    
    document.addEventListener('dragleave', (e) => {
        if (e.target === dropZone) {
            dropZone.classList.remove('active');
        }
    });
    
    document.addEventListener('drop', (e) => {
        e.preventDefault();
        dropZone.classList.remove('active');
        handleFiles(e.dataTransfer.files);
    });
}

async function handleFileSelect(event) {
    const files = event.target.files;
    if (files.length > 0) {
        await handleFiles(files);
    }
}

async function handleFiles(files) {
    for (const file of files) {
        const extension = file.name.split('.').pop().toLowerCase();
        
        if (extension === 'json') {
            // Handle JSON data
            const text = await file.text();
            const data = JSON.parse(text);
            graphData = convertToGraphData(Array.isArray(data) ? data : [data]);
            updateVisualization();
        } else if (['gltf', 'glb', 'obj', 'fbx', 'usdz'].includes(extension)) {
            // Handle 3D model
            const url = URL.createObjectURL(file);
            await loadModel(url, extension);
        }
    }
}

// UI event handlers
function setupUI() {
    // Search
    const searchInput = document.getElementById('searchInput');
    searchInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            const sources = Array.from(document.querySelectorAll('.source-btn.active'))
                .map(btn => btn.dataset.source);
            performSearch(searchInput.value, sources);
        }
    });
    
    // Source buttons
    document.querySelectorAll('.source-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            btn.classList.toggle('active');
        });
    });
    
    // Layout buttons
    document.querySelectorAll('.layout-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.layout-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            currentLayout = btn.dataset.layout;
            updateVisualization();
        });
    });
    
    // Mode buttons
    document.querySelectorAll('.mode-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            switchMode(btn.dataset.mode);
        });
    });
}

// Multiplayer status
function updateMultiplayerStatus(connected, userCount) {
    const statusDot = document.getElementById('statusDot');
    const statusText = document.getElementById('statusText');
    const userCountEl = document.getElementById('userCount');
    
    statusDot.classList.toggle('connected', connected);
    statusText.textContent = connected ? 'Connected' : 'Connecting...';
    userCountEl.textContent = `(${userCount} users)`;
}

// Loading overlay
function showLoading(show) {
    const overlay = document.getElementById('loadingOverlay');
    overlay.style.display = show ? 'flex' : 'none';
}

// Initialize
async function init() {
    await initializeScene();
    setupUI();
    setupFileHandling();
    
    // Connect to multiplayer
    networking.on('connected', () => {
        updateMultiplayerStatus(true, networking.connectedUsers.size);
    });
    
    networking.on('disconnected', () => {
        updateMultiplayerStatus(false, 0);
    });
    
    networking.on('userCountChanged', (count) => {
        updateMultiplayerStatus(true, count);
    });
    
    // Listen for multiplayer events
    multiplayerSync.on('searchResults', (data) => {
        graphData = data.graphData;
        updateVisualization();
    });
    
    multiplayerSync.on('nodeClick', (node) => {
        handleNodeClick(node);
    });
    
    await networking.connect();
}

// Start the application
init().catch(console.error);