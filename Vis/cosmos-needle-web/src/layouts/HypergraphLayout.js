import * as THREE from 'three';

export class HypergraphLayout {
    constructor(scene) {
        this.scene = scene;
        this.group = new THREE.Group();
        this.scene.add(this.group);
        this.particles = null;
        this.connections = [];
    }
    
    generateCosmos(graphData) {
        this.clear();
        
        // Create particle system for nodes
        const positions = new Float32Array(graphData.nodes.length * 3);
        const colors = new Float32Array(graphData.nodes.length * 3);
        const sizes = new Float32Array(graphData.nodes.length);
        
        const colorMap = {
            icosa: new THREE.Color(0xFF6B6B),
            objaverse: new THREE.Color(0x4ECDC4),
            github: new THREE.Color(0x95E1D3),
            local: new THREE.Color(0xF38181),
            web: new THREE.Color(0xAA96DA)
        };
        
        graphData.nodes.forEach((node, i) => {
            // Spherical distribution
            const phi = Math.acos(1 - 2 * Math.random());
            const theta = Math.random() * Math.PI * 2;
            const radius = 50 + Math.random() * 50;
            
            positions[i * 3] = radius * Math.sin(phi) * Math.cos(theta);
            positions[i * 3 + 1] = radius * Math.sin(phi) * Math.sin(theta);
            positions[i * 3 + 2] = radius * Math.cos(phi);
            
            const color = colorMap[node.type] || new THREE.Color(0xFFFFFF);
            colors[i * 3] = color.r;
            colors[i * 3 + 1] = color.g;
            colors[i * 3 + 2] = color.b;
            
            sizes[i] = 2 + (node.val || 1) * 3;
            
            node.position = new THREE.Vector3(
                positions[i * 3],
                positions[i * 3 + 1],
                positions[i * 3 + 2]
            );
        });
        
        const geometry = new THREE.BufferGeometry();
        geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
        geometry.setAttribute('color', new THREE.BufferAttribute(colors, 3));
        geometry.setAttribute('size', new THREE.BufferAttribute(sizes, 1));
        
        const material = new THREE.ShaderMaterial({
            uniforms: {
                time: { value: 0 },
                pixelRatio: { value: window.devicePixelRatio }
            },
            vertexShader: `
                attribute float size;
                varying vec3 vColor;
                uniform float pixelRatio;
                
                void main() {
                    vColor = color;
                    vec4 mvPosition = modelViewMatrix * vec4(position, 1.0);
                    gl_PointSize = size * pixelRatio * (300.0 / -mvPosition.z);
                    gl_Position = projectionMatrix * mvPosition;
                }
            `,
            fragmentShader: `
                uniform float time;
                varying vec3 vColor;
                
                void main() {
                    vec2 center = gl_PointCoord - 0.5;
                    float dist = length(center);
                    float alpha = 1.0 - smoothstep(0.4, 0.5, dist);
                    
                    // Pulsing glow
                    alpha *= 0.8 + 0.2 * sin(time * 2.0);
                    
                    // Corona effect
                    vec3 corona = vColor * (1.0 + 0.5 * (1.0 - dist));
                    
                    gl_FragColor = vec4(corona, alpha);
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
        
        // Start animation
        this.animateCosmos();
    }
    
    generateTree(graphData) {
        this.clear();
        
        // Build tree hierarchy
        const root = this.buildTreeStructure(graphData);
        
        // Layout tree in 3D
        this.layoutTree(root, 0, 0, 0, 100, 0);
        
        // Create visual representation
        this.visualizeTree(root);
    }
    
    buildTreeStructure(graphData) {
        // Find root nodes (no incoming links)
        const hasIncoming = new Set();
        graphData.links.forEach(link => {
            hasIncoming.add(link.target);
        });
        
        const roots = graphData.nodes.filter(node => !hasIncoming.has(node.id));
        
        // Build adjacency map
        const children = new Map();
        graphData.links.forEach(link => {
            if (!children.has(link.source)) {
                children.set(link.source, []);
            }
            children.get(link.source).push(link.target);
        });
        
        // Create tree structure
        const buildNode = (nodeId, depth = 0) => {
            const node = graphData.nodes.find(n => n.id === nodeId);
            if (!node) return null;
            
            const treeNode = {
                ...node,
                depth,
                children: []
            };
            
            const nodeChildren = children.get(nodeId) || [];
            treeNode.children = nodeChildren
                .map(childId => buildNode(childId, depth + 1))
                .filter(child => child !== null);
            
            return treeNode;
        };
        
        // Create artificial root if multiple roots
        if (roots.length === 1) {
            return buildNode(roots[0].id);
        } else {
            return {
                id: 'root',
                name: 'Root',
                type: 'root',
                depth: 0,
                children: roots.map(r => buildNode(r.id, 1))
            };
        }
    }
    
    layoutTree(node, x, y, z, spread, angle) {
        node.position = new THREE.Vector3(x, y, z);
        
        if (node.children.length === 0) return;
        
        const angleStep = Math.PI * 2 / node.children.length;
        const childSpread = spread * 0.7;
        
        node.children.forEach((child, i) => {
            const childAngle = angle + angleStep * i;
            const childX = x + Math.cos(childAngle) * spread;
            const childY = y - 20;
            const childZ = z + Math.sin(childAngle) * spread;
            
            this.layoutTree(child, childX, childY, childZ, childSpread, childAngle);
        });
    }
    
    visualizeTree(root) {
        const visited = new Set();
        
        const visualizeNode = (node) => {
            if (visited.has(node.id)) return;
            visited.add(node.id);
            
            // Create node sphere
            const geometry = new THREE.SphereGeometry(2 + (node.val || 1), 16, 16);
            const material = new THREE.MeshStandardMaterial({
                color: this.getNodeColor(node.type),
                emissive: this.getNodeColor(node.type),
                emissiveIntensity: 0.3
            });
            const sphere = new THREE.Mesh(geometry, material);
            sphere.position.copy(node.position);
            sphere.userData = node;
            this.group.add(sphere);
            
            // Create connections to children
            node.children.forEach(child => {
                const curve = new THREE.CatmullRomCurve3([
                    node.position,
                    new THREE.Vector3(
                        (node.position.x + child.position.x) / 2,
                        (node.position.y + child.position.y) / 2 + 10,
                        (node.position.z + child.position.z) / 2
                    ),
                    child.position
                ]);
                
                const points = curve.getPoints(20);
                const geometry = new THREE.BufferGeometry().setFromPoints(points);
                const material = new THREE.LineBasicMaterial({
                    color: 0x4444FF,
                    opacity: 0.6,
                    transparent: true
                });
                const line = new THREE.Line(geometry, material);
                this.connections.push(line);
                this.group.add(line);
                
                visualizeNode(child);
            });
        };
        
        visualizeNode(root);
    }
    
    createConnections(graphData) {
        const nodeMap = new Map();
        graphData.nodes.forEach(node => {
            nodeMap.set(node.id, node);
        });
        
        graphData.links.forEach(link => {
            const sourceNode = nodeMap.get(link.source);
            const targetNode = nodeMap.get(link.target);
            
            if (sourceNode && targetNode && sourceNode.position && targetNode.position) {
                const points = [sourceNode.position, targetNode.position];
                const geometry = new THREE.BufferGeometry().setFromPoints(points);
                const material = new THREE.LineBasicMaterial({
                    color: 0x4488FF,
                    opacity: 0.3,
                    transparent: true
                });
                const line = new THREE.Line(geometry, material);
                this.connections.push(line);
                this.group.add(line);
            }
        });
    }
    
    getNodeColor(type) {
        const colors = {
            icosa: 0xFF6B6B,
            objaverse: 0x4ECDC4,
            github: 0x95E1D3,
            local: 0xF38181,
            web: 0xAA96DA,
            root: 0xFFFFFF
        };
        return colors[type] || 0x888888;
    }
    
    animateCosmos() {
        const animate = () => {
            if (this.particles && this.particles.material.uniforms) {
                this.particles.material.uniforms.time.value += 0.01;
                this.particles.rotation.y += 0.0005;
            }
            requestAnimationFrame(animate);
        };
        animate();
    }
    
    clear() {
        this.particles = null;
        this.connections = [];
        while (this.group.children.length > 0) {
            const child = this.group.children[0];
            if (child.geometry) child.geometry.dispose();
            if (child.material) {
                if (Array.isArray(child.material)) {
                    child.material.forEach(m => m.dispose());
                } else {
                    child.material.dispose();
                }
            }
            this.group.remove(child);
        }
    }
}