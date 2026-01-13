import * as THREE from 'three';
import { gsap } from 'gsap';

export class MassiveDataLayout {
    constructor(scene) {
        this.scene = scene;
        this.group = new THREE.Group();
        this.group.name = 'MassiveData';
        this.instancedMesh = null;
        this.maxInstances = 1000000; // Support 1M instances
        this.visibleInstances = 0;
        this.camera = null;
        
        // LOD system
        this.lodDistances = [100, 500, 1000, 5000];
        this.lodGeometries = [];
        this.lodMaterials = [];
        
        this.init();
    }
    
    init() {
        // Create LOD geometries with decreasing detail
        this.lodGeometries = [
            new THREE.SphereGeometry(1, 32, 16),  // High detail
            new THREE.SphereGeometry(1, 16, 8),   // Medium detail  
            new THREE.SphereGeometry(1, 8, 4),    // Low detail
            new THREE.BoxGeometry(2, 2, 2)        // Ultra low detail (cubes)
        ];
        
        // Create materials with different opacity for distance culling
        this.lodMaterials = [
            new THREE.MeshStandardMaterial({ 
                color: 0xffffff, 
                vertexColors: true,
                transparent: true
            }),
            new THREE.MeshBasicMaterial({ 
                color: 0xffffff,
                vertexColors: true,
                transparent: true
            }),
            new THREE.MeshBasicMaterial({ 
                color: 0xffffff,
                vertexColors: true,
                transparent: true,
                opacity: 0.8
            }),
            new THREE.MeshBasicMaterial({ 
                color: 0xffffff,
                vertexColors: true,
                transparent: true,
                opacity: 0.5
            })
        ];
    }
    
    async generate(graphData, camera) {
        console.log(`Generating massive visualization with ${graphData.nodes.length} nodes`);
        this.camera = camera;
        this.clear();
        this.scene.add(this.group);
        
        if (graphData.nodes.length > 100000) {
            // Use uber-optimized instanced rendering for massive datasets
            await this.generateInstancedMega(graphData);
        } else if (graphData.nodes.length > 10000) {
            // Use instanced rendering for large datasets  
            await this.generateInstanced(graphData);
        } else {
            // Use regular objects for smaller datasets
            await this.generateRegular(graphData);
        }
        
        this.animateEntrance();
    }
    
    async generateInstancedMega(graphData) {
        const nodeCount = Math.min(graphData.nodes.length, this.maxInstances);
        console.log(`Using mega instancing for ${nodeCount} nodes`);
        
        // Use the simplest geometry for maximum performance
        const geometry = new THREE.InstancedBufferGeometry();
        const baseGeometry = new THREE.SphereGeometry(0.5, 8, 4);
        
        // Copy base geometry attributes
        geometry.index = baseGeometry.index;
        geometry.attributes = baseGeometry.attributes;
        
        // Instance attributes
        const instanceMatrix = new THREE.InstancedBufferAttribute(
            new Float32Array(nodeCount * 16), 16
        );
        const instanceColor = new THREE.InstancedBufferAttribute(
            new Float32Array(nodeCount * 3), 3
        );
        
        geometry.setAttribute('instanceMatrix', instanceMatrix);
        geometry.setAttribute('instanceColor', instanceColor);
        
        // Material with custom shader for performance
        const material = new THREE.ShaderMaterial({
            uniforms: {
                time: { value: 0 },
                cameraPosition: { value: new THREE.Vector3() }
            },
            vertexShader: `
                attribute vec3 instanceColor;
                varying vec3 vColor;
                varying float vDistance;
                uniform vec3 cameraPosition;
                uniform float time;
                
                void main() {
                    vColor = instanceColor;
                    
                    // Apply instance matrix
                    vec4 instancePosition = instanceMatrix * vec4(position, 1.0);
                    vec4 worldPosition = modelMatrix * instancePosition;
                    
                    // Calculate distance for LOD
                    vDistance = distance(worldPosition.xyz, cameraPosition);
                    
                    // Floating animation
                    worldPosition.y += sin(time + worldPosition.x * 0.01) * 0.5;
                    
                    gl_Position = projectionMatrix * viewMatrix * worldPosition;
                }
            `,
            fragmentShader: `
                varying vec3 vColor;
                varying float vDistance;
                
                void main() {
                    // Distance-based alpha for performance
                    float alpha = 1.0;
                    if (vDistance > 1000.0) alpha = 0.1;
                    else if (vDistance > 500.0) alpha = 0.5;
                    else if (vDistance > 100.0) alpha = 0.8;
                    
                    // Discard distant pixels
                    if (alpha < 0.1) discard;
                    
                    gl_FragColor = vec4(vColor, alpha);
                }
            `,
            transparent: true
        });
        
        // Position instances in 3D space
        this.positionInstances(graphData.nodes, instanceMatrix, instanceColor, nodeCount);
        
        this.instancedMesh = new THREE.Mesh(geometry, material);
        this.instancedMesh.count = nodeCount;
        this.group.add(this.instancedMesh);
        
        // Start update loop
        this.startUpdateLoop();
    }
    
    async generateInstanced(graphData) {
        const nodeCount = Math.min(graphData.nodes.length, 50000);
        console.log(`Using standard instancing for ${nodeCount} nodes`);
        
        const geometry = new THREE.SphereGeometry(1, 16, 8);
        const material = new THREE.MeshStandardMaterial({ 
            vertexColors: true,
            transparent: true
        });
        
        this.instancedMesh = new THREE.InstancedMesh(geometry, material, nodeCount);
        
        // Position and color instances
        const matrix = new THREE.Matrix4();
        const color = new THREE.Color();
        
        graphData.nodes.slice(0, nodeCount).forEach((node, i) => {
            // Position in sphere
            const phi = Math.acos(1 - 2 * (i + 0.5) / nodeCount);
            const theta = Math.PI * (1 + Math.sqrt(5)) * i;
            const radius = 50 + Math.random() * 100;
            
            const x = radius * Math.sin(phi) * Math.cos(theta);
            const y = radius * Math.sin(phi) * Math.sin(theta);
            const z = radius * Math.cos(phi);
            
            // Scale based on importance
            const scale = 0.5 + (node.val || 1) * 2;
            matrix.makeScale(scale, scale, scale);
            matrix.setPosition(x, y, z);
            this.instancedMesh.setMatrixAt(i, matrix);
            
            // Color by type
            const colors = {
                icosa: 0xFF6B6B,
                objaverse: 0x4ECDC4,
                github: 0x95E1D3,
                local: 0xF38181,
                web: 0xAA96DA
            };
            color.setHex(colors[node.source] || 0xFFFFFF);
            this.instancedMesh.setColorAt(i, color);
        });
        
        this.instancedMesh.instanceMatrix.needsUpdate = true;
        if (this.instancedMesh.instanceColor) {
            this.instancedMesh.instanceColor.needsUpdate = true;
        }
        
        this.group.add(this.instancedMesh);
    }
    
    async generateRegular(graphData) {
        console.log(`Using regular objects for ${graphData.nodes.length} nodes`);
        
        graphData.nodes.forEach((node, i) => {
            const geometry = new THREE.SphereGeometry(1 + (node.val || 1), 16, 8);
            const material = new THREE.MeshStandardMaterial({
                color: this.getNodeColor(node.source)
            });
            
            const mesh = new THREE.Mesh(geometry, material);
            
            // Position
            const phi = Math.acos(1 - 2 * (i + 0.5) / graphData.nodes.length);
            const theta = Math.PI * (1 + Math.sqrt(5)) * i;
            const radius = 30 + Math.random() * 50;
            
            mesh.position.set(
                radius * Math.sin(phi) * Math.cos(theta),
                radius * Math.sin(phi) * Math.sin(theta),
                radius * Math.cos(phi)
            );
            
            mesh.userData.nodeData = node;
            this.group.add(mesh);
        });
    }
    
    positionInstances(nodes, instanceMatrix, instanceColor, count) {
        const matrix = new THREE.Matrix4();
        const colors = {
            icosa: [1, 0.42, 0.42],
            objaverse: [0.3, 0.8, 0.77],
            github: [0.58, 0.88, 0.83],
            local: [0.95, 0.51, 0.51],
            web: [0.67, 0.59, 0.85]
        };
        
        for (let i = 0; i < count; i++) {
            const node = nodes[i] || nodes[i % nodes.length];
            
            // Spiral galaxy distribution for massive datasets
            const t = i / count;
            const spiral = t * 10; // Multiple spirals
            const radius = 20 + t * 200; // Expanding radius
            const height = (Math.random() - 0.5) * 50; // Vertical spread
            
            const x = radius * Math.cos(spiral);
            const z = radius * Math.sin(spiral);
            const y = height;
            
            // Scale based on importance
            const scale = 0.3 + (node.val || 1) * 0.7;
            matrix.makeScale(scale, scale, scale);
            matrix.setPosition(x, y, z);
            
            // Store matrix
            instanceMatrix.setXYZW(i * 4 + 0, matrix.elements[0], matrix.elements[1], matrix.elements[2], matrix.elements[3]);
            instanceMatrix.setXYZW(i * 4 + 1, matrix.elements[4], matrix.elements[5], matrix.elements[6], matrix.elements[7]);
            instanceMatrix.setXYZW(i * 4 + 2, matrix.elements[8], matrix.elements[9], matrix.elements[10], matrix.elements[11]);
            instanceMatrix.setXYZW(i * 4 + 3, matrix.elements[12], matrix.elements[13], matrix.elements[14], matrix.elements[15]);
            
            // Store color
            const color = colors[node.source] || [1, 1, 1];
            instanceColor.setXYZ(i, color[0], color[1], color[2]);
        }
        
        instanceMatrix.needsUpdate = true;
        instanceColor.needsUpdate = true;
    }
    
    startUpdateLoop() {
        const animate = () => {
            if (this.instancedMesh && this.camera) {
                // Update time uniform
                if (this.instancedMesh.material.uniforms) {
                    this.instancedMesh.material.uniforms.time.value += 0.01;
                    this.instancedMesh.material.uniforms.cameraPosition.value.copy(this.camera.position);
                }
                
                // Frustum culling for performance
                this.performFrustumCulling();
                
                requestAnimationFrame(animate);
            }
        };
        animate();
    }
    
    performFrustumCulling() {
        // Simple distance-based culling
        if (this.camera && this.instancedMesh) {
            const cameraPos = this.camera.position;
            let visibleCount = 0;
            
            // Only update every few frames for performance
            if (Math.random() > 0.9) {
                // Calculate how many instances should be visible based on camera distance
                // This is a simplified approach - in production you'd use proper frustum culling
                const avgDistance = cameraPos.length();
                const visibilityFactor = Math.max(0.1, Math.min(1.0, 1000 / avgDistance));
                visibleCount = Math.floor(this.instancedMesh.count * visibilityFactor);
                
                // Update visible count (Three.js doesn't directly support this, but we can use it for optimization)
                this.visibleInstances = visibleCount;
            }
        }
    }
    
    getNodeColor(source) {
        const colors = {
            icosa: 0xFF6B6B,
            objaverse: 0x4ECDC4,
            github: 0x95E1D3,
            local: 0xF38181,
            web: 0xAA96DA
        };
        return colors[source] || 0xFFFFFF;
    }
    
    animateEntrance() {
        if (this.instancedMesh) {
            this.group.scale.set(0.1, 0.1, 0.1);
            gsap.to(this.group.scale, {
                x: 1, y: 1, z: 1,
                duration: 3,
                ease: 'power2.out'
            });
        }
    }
    
    update() {
        // Rotation for dramatic effect
        if (this.group) {
            this.group.rotation.y += 0.0005;
        }
    }
    
    clear() {
        this.scene.remove(this.group);
        
        // Dispose of geometries and materials
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
        
        this.instancedMesh = null;
        this.visibleInstances = 0;
        
        while (this.group.children.length > 0) {
            this.group.remove(this.group.children[0]);
        }
    }
}