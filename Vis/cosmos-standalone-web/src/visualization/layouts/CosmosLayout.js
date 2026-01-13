import * as THREE from 'three';
import { gsap } from 'gsap';

export class CosmosLayout {
    constructor(scene) {
        this.scene = scene;
        this.group = new THREE.Group();
        this.group.name = 'Cosmos';
        this.particles = null;
        this.connections = [];
        this.time = 0;
        this.nodeObjects = new Map(); // Store individual node objects for interaction
    }
    
    async generate(graphData) {
        this.clear();
        this.scene.add(this.group);
        
        // Create particle system for nodes
        const particleCount = graphData.nodes.length;
        const positions = new Float32Array(particleCount * 3);
        const colors = new Float32Array(particleCount * 3);
        const sizes = new Float32Array(particleCount);
        
        const colorMap = {
            icosa: new THREE.Color(0xFF6B6B),      // Casa Gallery - Red/Pink
            casa: new THREE.Color(0xFF6B6B),        // Casa Gallery alias
            polli: new THREE.Color(0xFFB6E1),       // Polli - Light Pink  
            objaverse: new THREE.Color(0x4ECDC4),   // Objaverse - Teal
            github: new THREE.Color(0x95E1D3),      // GitHub - Light Green
            local: new THREE.Color(0xF38181),       // Local Files - Coral
            web: new THREE.Color(0xAA96DA),         // Web Search - Purple
            sketchfab: new THREE.Color(0x1CAAD9)    // Sketchfab - Blue
        };
        
        // Create different shapes based on media type
        const createNodeGeometry = (type) => {
            switch(type) {
                case 'image':
                    return new THREE.PlaneGeometry(5, 5); // Flat square for images
                case 'video':
                    return new THREE.BoxGeometry(5, 5, 1); // Thin box for videos
                case '3d_object':
                case 'model':
                case 'artwork':
                    return new THREE.IcosahedronGeometry(3, 1); // Icosahedron for 3D objects
                case 'person':
                case 'user':
                case 'artist':
                    return new THREE.ConeGeometry(3, 6, 8); // Cone for people
                case 'place':
                    return new THREE.CylinderGeometry(3, 3, 2, 6); // Cylinder for places
                case 'repository':
                case 'project':
                    return new THREE.OctahedronGeometry(4); // Octahedron for code/projects
                case 'file':
                case 'document':
                    return new THREE.TetrahedronGeometry(3); // Tetrahedron for files
                case 'folder':
                    return new THREE.BoxGeometry(4, 4, 4); // Cube for folders
                default:
                    return new THREE.SphereGeometry(3, 16, 16); // Default sphere
            }
        };
        
        // Create individual node objects with different shapes
        const nodeGroup = new THREE.Group();
        nodeGroup.name = 'NodeObjects';
        this.group.add(nodeGroup);
        
        // Distribute nodes in spherical pattern
        graphData.nodes.forEach((node, i) => {
            const phi = Math.acos(1 - 2 * (i + 0.5) / particleCount);
            const theta = Math.PI * (1 + Math.sqrt(5)) * i;
            const radius = 50 + Math.random() * 30;
            
            const x = radius * Math.sin(phi) * Math.cos(theta);
            const y = radius * Math.sin(phi) * Math.sin(theta);
            const z = radius * Math.cos(phi);
            
            positions[i * 3] = x;
            positions[i * 3 + 1] = y;
            positions[i * 3 + 2] = z;
            
            // Store position for connections
            node.cosmosPosition = new THREE.Vector3(x, y, z);
            
            // Set color based on type
            const color = colorMap[node.type] || new THREE.Color(0xFFFFFF);
            colors[i * 3] = color.r;
            colors[i * 3 + 1] = color.g;
            colors[i * 3 + 2] = color.b;
            
            // Size based on importance
            sizes[i] = 3 + (node.val || 1) * 2;
            
            // Create individual 3D object for each node
            const nodeGeometry = createNodeGeometry(node.type);
            
            // Special material for GitHub repositories with brightness based on stars
            let nodeMaterial;
            if (node.source === 'github' && node.type === 'repository') {
                const brightness = node.brightness || 0.2;
                const baseColor = colorMap[node.source] || 0xFFFFFF;
                
                nodeMaterial = new THREE.MeshPhongMaterial({
                    color: baseColor,
                    emissive: baseColor,
                    emissiveIntensity: 0.2 + (brightness * 0.8), // Brighter repos glow more
                    transparent: true,
                    opacity: 0.7 + (brightness * 0.3), // More popular repos are more opaque
                    shininess: 50 + (brightness * 50) // Shinier surface for popular repos
                });
            } else {
                nodeMaterial = new THREE.MeshPhongMaterial({
                    color: colorMap[node.source] || 0xFFFFFF,
                    emissive: colorMap[node.source] || 0xFFFFFF,
                    emissiveIntensity: 0.2,
                    transparent: true,
                    opacity: 0.8
                });
            }
            
            const nodeMesh = new THREE.Mesh(nodeGeometry, nodeMaterial);
            nodeMesh.position.set(x, y, z);
            
            // Scale based on importance (especially for GitHub repos)
            let scale = 0.5 + (node.val || 1) * 0.3;
            if (node.source === 'github' && node.type === 'repository') {
                // Make popular repos significantly larger
                scale = 0.5 + (node.val || 1) * 0.5;
            }
            nodeMesh.scale.setScalar(scale);
            
            // Add user data for interaction
            nodeMesh.userData.nodeData = node;
            nodeMesh.userData.originalScale = scale;
            
            // Add text label for important nodes
            if (node.val > 2 || node.type === 'artist' || node.type === 'user') {
                const canvas = document.createElement('canvas');
                const context = canvas.getContext('2d');
                canvas.width = 256;
                canvas.height = 64;
                
                context.fillStyle = 'rgba(0, 0, 0, 0.7)';
                context.fillRect(0, 0, 256, 64);
                
                context.font = 'bold 24px Arial';
                context.fillStyle = 'white';
                context.textAlign = 'center';
                context.textBaseline = 'middle';
                context.fillText(node.name.substring(0, 20), 128, 32);
                
                const texture = new THREE.CanvasTexture(canvas);
                const labelMaterial = new THREE.SpriteMaterial({ 
                    map: texture,
                    transparent: true
                });
                const label = new THREE.Sprite(labelMaterial);
                label.scale.set(10, 2.5, 1);
                label.position.y = 5;
                nodeMesh.add(label);
            }
            
            nodeGroup.add(nodeMesh);
            this.nodeObjects.set(node.id, nodeMesh);
            
            // Add rotating animation for certain types
            if (node.type === 'artwork' || node.type === '3d_object' || node.type === 'model') {
                nodeMesh.userData.rotate = true;
            }
            
            // Add activity indicator for GitHub repos
            if (node.source === 'github' && node.type === 'repository' && node.activity > 0) {
                // Add pulsing ring for active repos
                const ringGeometry = new THREE.RingGeometry(
                    scale * 4, 
                    scale * 5, 
                    32
                );
                const ringMaterial = new THREE.MeshBasicMaterial({
                    color: 0x00ff00,
                    transparent: true,
                    opacity: node.activity * 0.5,
                    side: THREE.DoubleSide
                });
                const ring = new THREE.Mesh(ringGeometry, ringMaterial);
                ring.lookAt(this.camera.position);
                nodeMesh.add(ring);
                nodeMesh.userData.activityRing = ring;
            }
            
            // Add fork indicators for highly forked repos
            if (node.source === 'github' && node.forks > 100) {
                const forkIndicator = new THREE.Group();
                const forkCount = Math.min(6, Math.floor(node.forks / 100));
                for (let f = 0; f < forkCount; f++) {
                    const angle = (f / forkCount) * Math.PI * 2;
                    const branchGeometry = new THREE.ConeGeometry(scale * 0.3, scale * 2, 4);
                    const branchMaterial = new THREE.MeshPhongMaterial({
                        color: 0x44ff44,
                        emissive: 0x44ff44,
                        emissiveIntensity: 0.3
                    });
                    const branch = new THREE.Mesh(branchGeometry, branchMaterial);
                    branch.position.x = Math.cos(angle) * scale * 3;
                    branch.position.z = Math.sin(angle) * scale * 3;
                    branch.rotation.z = angle + Math.PI / 2;
                    forkIndicator.add(branch);
                }
                nodeMesh.add(forkIndicator);
                nodeMesh.userData.forkIndicator = forkIndicator;
            }
        });
        
        // Create particle geometry
        const geometry = new THREE.BufferGeometry();
        geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
        geometry.setAttribute('color', new THREE.BufferAttribute(colors, 3));
        geometry.setAttribute('size', new THREE.BufferAttribute(sizes, 1));
        
        // Shader material for custom particle rendering
        const material = new THREE.ShaderMaterial({
            uniforms: {
                time: { value: 0 },
                pixelRatio: { value: window.devicePixelRatio }
            },
            vertexShader: `
                attribute float size;
                varying vec3 vColor;
                uniform float time;
                uniform float pixelRatio;
                
                void main() {
                    vColor = color;
                    vec3 pos = position;
                    
                    // Gentle floating animation
                    pos.y += sin(time + position.x * 0.01) * 2.0;
                    
                    vec4 mvPosition = modelViewMatrix * vec4(pos, 1.0);
                    gl_PointSize = size * pixelRatio * (300.0 / -mvPosition.z);
                    gl_Position = projectionMatrix * mvPosition;
                }
            `,
            fragmentShader: `
                varying vec3 vColor;
                uniform float time;
                
                void main() {
                    vec2 center = gl_PointCoord - 0.5;
                    float dist = length(center);
                    
                    if (dist > 0.5) discard;
                    
                    // Soft edges
                    float alpha = 1.0 - smoothstep(0.4, 0.5, dist);
                    
                    // Pulsing effect
                    alpha *= 0.7 + 0.3 * sin(time * 2.0);
                    
                    // Glow effect
                    vec3 glow = vColor * (1.0 + (1.0 - dist) * 0.5);
                    
                    gl_FragColor = vec4(glow, alpha);
                }
            `,
            blending: THREE.AdditiveBlending,
            depthTest: false,
            transparent: true,
            vertexColors: true
        });
        
        this.particles = new THREE.Points(geometry, material);
        this.group.add(this.particles);
        
        // Create connections
        this.createConnections(graphData);
        
        // Add nebula background
        this.createNebula();
        
        // Animate entrance
        this.animateEntrance();
    }
    
    createConnections(graphData) {
        const nodeMap = new Map();
        graphData.nodes.forEach(node => {
            nodeMap.set(node.id, node);
        });
        
        graphData.links.forEach(link => {
            const sourceNode = nodeMap.get(link.source);
            const targetNode = nodeMap.get(link.target);
            
            if (sourceNode?.cosmosPosition && targetNode?.cosmosPosition) {
                // Create curved connection
                const curve = new THREE.CatmullRomCurve3([
                    sourceNode.cosmosPosition,
                    new THREE.Vector3(
                        (sourceNode.cosmosPosition.x + targetNode.cosmosPosition.x) / 2,
                        (sourceNode.cosmosPosition.y + targetNode.cosmosPosition.y) / 2,
                        (sourceNode.cosmosPosition.z + targetNode.cosmosPosition.z) / 2
                    ).multiplyScalar(0.8),
                    targetNode.cosmosPosition
                ]);
                
                const points = curve.getPoints(20);
                const geometry = new THREE.BufferGeometry().setFromPoints(points);
                
                const material = new THREE.LineBasicMaterial({
                    color: 0x4488ff,
                    transparent: true,
                    opacity: 0.3,
                    blending: THREE.AdditiveBlending
                });
                
                const line = new THREE.Line(geometry, material);
                this.connections.push(line);
                this.group.add(line);
            }
        });
    }
    
    createNebula() {
        // Create nebula cloud effect
        const nebulaCount = 500;
        const nebulaPositions = new Float32Array(nebulaCount * 3);
        const nebulaColors = new Float32Array(nebulaCount * 3);
        
        for (let i = 0; i < nebulaCount; i++) {
            const theta = Math.random() * Math.PI * 2;
            const phi = Math.random() * Math.PI;
            const radius = 80 + Math.random() * 40;
            
            nebulaPositions[i * 3] = radius * Math.sin(phi) * Math.cos(theta);
            nebulaPositions[i * 3 + 1] = radius * Math.sin(phi) * Math.sin(theta);
            nebulaPositions[i * 3 + 2] = radius * Math.cos(phi);
            
            // Purple/blue nebula colors
            const hue = 0.7 + Math.random() * 0.2;
            const color = new THREE.Color().setHSL(hue, 0.8, 0.5);
            nebulaColors[i * 3] = color.r;
            nebulaColors[i * 3 + 1] = color.g;
            nebulaColors[i * 3 + 2] = color.b;
        }
        
        const nebulaGeometry = new THREE.BufferGeometry();
        nebulaGeometry.setAttribute('position', new THREE.BufferAttribute(nebulaPositions, 3));
        nebulaGeometry.setAttribute('color', new THREE.BufferAttribute(nebulaColors, 3));
        
        const nebulaMaterial = new THREE.PointsMaterial({
            size: 15,
            vertexColors: true,
            transparent: true,
            opacity: 0.1,
            blending: THREE.AdditiveBlending,
            depthTest: false
        });
        
        const nebula = new THREE.Points(nebulaGeometry, nebulaMaterial);
        this.group.add(nebula);
    }
    
    animateEntrance() {
        // Scale up from center
        this.group.scale.set(0.1, 0.1, 0.1);
        gsap.to(this.group.scale, {
            x: 1,
            y: 1,
            z: 1,
            duration: 2,
            ease: 'power2.out'
        });
        
        // Fade in connections
        this.connections.forEach((line, index) => {
            line.material.opacity = 0;
            gsap.to(line.material, {
                opacity: 0.3,
                duration: 1,
                delay: 0.5 + index * 0.01,
                ease: 'power2.in'
            });
        });
    }
    
    update() {
        if (this.particles) {
            this.time += 0.01;
            this.particles.material.uniforms.time.value = this.time;
            
            // Slow rotation
            this.group.rotation.y += 0.001;
        }
        
        // Animate individual nodes
        this.nodeObjects.forEach((nodeMesh, nodeId) => {
            // Rotate certain types
            if (nodeMesh.userData.rotate) {
                nodeMesh.rotation.y += 0.01;
                nodeMesh.rotation.x += 0.005;
            }
            
            // Pulse effect for important nodes
            if (nodeMesh.userData.nodeData.val > 3) {
                const scale = nodeMesh.userData.originalScale * (1 + Math.sin(this.time * 2) * 0.1);
                nodeMesh.scale.setScalar(scale);
            }
            
            // Float animation for people/artists
            if (nodeMesh.userData.nodeData.type === 'artist' || nodeMesh.userData.nodeData.type === 'user') {
                nodeMesh.position.y += Math.sin(this.time + nodeMesh.position.x * 0.01) * 0.02;
            }
            
            // Animate GitHub repo indicators
            if (nodeMesh.userData.nodeData.source === 'github' && nodeMesh.userData.nodeData.type === 'repository') {
                // Pulse activity ring
                if (nodeMesh.userData.activityRing) {
                    const pulseScale = 1 + Math.sin(this.time * 3) * 0.1;
                    nodeMesh.userData.activityRing.scale.setScalar(pulseScale);
                    nodeMesh.userData.activityRing.material.opacity = nodeMesh.userData.nodeData.activity * 0.5 * (0.5 + Math.sin(this.time * 2) * 0.5);
                }
                
                // Rotate fork indicators
                if (nodeMesh.userData.forkIndicator) {
                    nodeMesh.userData.forkIndicator.rotation.y += 0.01;
                }
                
                // Extra glow for very popular repos (1000+ stars)
                if (nodeMesh.userData.nodeData.stars > 1000) {
                    const glowIntensity = 0.3 + Math.sin(this.time) * 0.1;
                    nodeMesh.material.emissiveIntensity = 0.2 + (nodeMesh.userData.nodeData.brightness * 0.8) + glowIntensity;
                }
            }
        });
    }
    
    clear() {
        this.scene.remove(this.group);
        
        // Dispose resources
        this.group.traverse((child) => {
            if (child.geometry) child.geometry.dispose();
            if (child.material) {
                if (Array.isArray(child.material)) {
                    child.material.forEach(mat => mat.dispose());
                } else {
                    child.material.dispose();
                }
            }
        });
        
        // Dispose textures for labels
        this.group.traverse((child) => {
            if (child.material && child.material.map) {
                child.material.map.dispose();
            }
        });
        
        // Reset
        this.particles = null;
        this.connections = [];
        this.time = 0;
        this.nodeObjects.clear();
        
        while (this.group.children.length > 0) {
            this.group.remove(this.group.children[0]);
        }
    }
}