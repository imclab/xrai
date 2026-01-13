import * as THREE from 'three';

export class MetavidoVFX {
    constructor(scene) {
        this.scene = scene;
        this.group = new THREE.Group();
        this.scene.add(this.group);
        this.isVisible = false;
        this.particles = [];
        this.time = 0;
        
        this.init();
    }
    
    init() {
        // Create multiple particle systems for Metavido-style VFX
        this.createFlowField();
        this.createOrbitingParticles();
        this.createCentralCore();
        this.createEnergyRings();
        
        // Initially hidden
        this.group.visible = false;
    }
    
    createFlowField() {
        const particleCount = 5000;
        const positions = new Float32Array(particleCount * 3);
        const colors = new Float32Array(particleCount * 3);
        const sizes = new Float32Array(particleCount);
        
        for (let i = 0; i < particleCount; i++) {
            const theta = Math.random() * Math.PI * 2;
            const phi = Math.random() * Math.PI;
            const radius = 20 + Math.random() * 30;
            
            positions[i * 3] = radius * Math.sin(phi) * Math.cos(theta);
            positions[i * 3 + 1] = radius * Math.sin(phi) * Math.sin(theta);
            positions[i * 3 + 2] = radius * Math.cos(phi);
            
            // Gradient colors
            const t = i / particleCount;
            colors[i * 3] = 0.5 + 0.5 * Math.sin(t * Math.PI);
            colors[i * 3 + 1] = 0.3 + 0.3 * Math.sin(t * Math.PI + 1);
            colors[i * 3 + 2] = 0.8 + 0.2 * Math.sin(t * Math.PI + 2);
            
            sizes[i] = Math.random() * 2 + 0.5;
        }
        
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
                uniform float time;
                uniform float pixelRatio;
                
                void main() {
                    vColor = color;
                    vec3 pos = position;
                    
                    // Flow field animation
                    float flow = time * 0.1;
                    pos.x += sin(pos.y * 0.1 + flow) * 5.0;
                    pos.z += cos(pos.x * 0.1 + flow) * 5.0;
                    pos.y += sin(pos.z * 0.1 + flow * 2.0) * 2.0;
                    
                    vec4 mvPosition = modelViewMatrix * vec4(pos, 1.0);
                    gl_PointSize = size * pixelRatio * (200.0 / -mvPosition.z);
                    gl_Position = projectionMatrix * mvPosition;
                }
            `,
            fragmentShader: `
                varying vec3 vColor;
                
                void main() {
                    vec2 center = gl_PointCoord - 0.5;
                    float dist = length(center);
                    if (dist > 0.5) discard;
                    
                    float alpha = 1.0 - smoothstep(0.3, 0.5, dist);
                    gl_FragColor = vec4(vColor, alpha * 0.6);
                }
            `,
            blending: THREE.AdditiveBlending,
            depthTest: false,
            transparent: true,
            vertexColors: true
        });
        
        const flowField = new THREE.Points(geometry, material);
        this.group.add(flowField);
        this.particles.push({ mesh: flowField, material });
    }
    
    createOrbitingParticles() {
        const orbitCount = 3;
        const particlesPerOrbit = 100;
        
        for (let orbit = 0; orbit < orbitCount; orbit++) {
            const positions = new Float32Array(particlesPerOrbit * 3);
            const radius = 15 + orbit * 10;
            const color = new THREE.Color().setHSL(orbit / orbitCount, 0.8, 0.6);
            
            for (let i = 0; i < particlesPerOrbit; i++) {
                const angle = (i / particlesPerOrbit) * Math.PI * 2;
                positions[i * 3] = Math.cos(angle) * radius;
                positions[i * 3 + 1] = 0;
                positions[i * 3 + 2] = Math.sin(angle) * radius;
            }
            
            const geometry = new THREE.BufferGeometry();
            geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
            
            const material = new THREE.PointsMaterial({
                size: 3,
                color: color,
                blending: THREE.AdditiveBlending,
                transparent: true,
                opacity: 0.8
            });
            
            const points = new THREE.Points(geometry, material);
            points.userData = { orbit, speed: 0.5 + orbit * 0.2 };
            this.group.add(points);
            this.particles.push({ mesh: points, type: 'orbit' });
        }
    }
    
    createCentralCore() {
        // Inner glowing core
        const coreGeometry = new THREE.IcosahedronGeometry(5, 2);
        const coreMaterial = new THREE.MeshBasicMaterial({
            color: 0xFFFFFF,
            emissive: 0xFFFFFF,
            emissiveIntensity: 2,
            transparent: true,
            opacity: 0.8
        });
        const core = new THREE.Mesh(coreGeometry, coreMaterial);
        
        // Outer wireframe
        const wireframeGeometry = new THREE.IcosahedronGeometry(8, 1);
        const wireframeMaterial = new THREE.MeshBasicMaterial({
            color: 0x00FFFF,
            wireframe: true,
            transparent: true,
            opacity: 0.4
        });
        const wireframe = new THREE.Mesh(wireframeGeometry, wireframeMaterial);
        
        const coreGroup = new THREE.Group();
        coreGroup.add(core);
        coreGroup.add(wireframe);
        this.group.add(coreGroup);
        
        this.particles.push({ mesh: coreGroup, type: 'core' });
    }
    
    createEnergyRings() {
        const ringCount = 5;
        
        for (let i = 0; i < ringCount; i++) {
            const radius = 10 + i * 8;
            const segments = 64;
            const curve = new THREE.EllipseCurve(
                0, 0,
                radius, radius,
                0, 2 * Math.PI,
                false,
                0
            );
            
            const points = curve.getPoints(segments);
            const geometry = new THREE.BufferGeometry().setFromPoints(points);
            
            const material = new THREE.LineBasicMaterial({
                color: new THREE.Color().setHSL(i / ringCount * 0.3 + 0.5, 1, 0.5),
                blending: THREE.AdditiveBlending,
                transparent: true,
                opacity: 0.6,
                linewidth: 2
            });
            
            const ring = new THREE.LineLoop(geometry, material);
            ring.rotation.x = Math.random() * Math.PI;
            ring.rotation.y = Math.random() * Math.PI;
            ring.userData = { 
                rotationSpeed: new THREE.Vector3(
                    Math.random() * 0.01 - 0.005,
                    Math.random() * 0.01 - 0.005,
                    Math.random() * 0.01 - 0.005
                )
            };
            
            this.group.add(ring);
            this.particles.push({ mesh: ring, type: 'ring' });
        }
    }
    
    show() {
        this.isVisible = true;
        this.group.visible = true;
        this.animate();
    }
    
    hide() {
        this.isVisible = false;
        this.group.visible = false;
    }
    
    animate() {
        if (!this.isVisible) return;
        
        this.time += 0.01;
        
        // Update shader uniforms
        this.particles.forEach(particle => {
            if (particle.material && particle.material.uniforms) {
                particle.material.uniforms.time.value = this.time;
            }
            
            // Animate based on type
            switch (particle.type) {
                case 'orbit':
                    particle.mesh.rotation.y += particle.mesh.userData.speed * 0.01;
                    particle.mesh.rotation.x = Math.sin(this.time * 0.5) * 0.2;
                    break;
                    
                case 'core':
                    particle.mesh.rotation.x += 0.01;
                    particle.mesh.rotation.y += 0.005;
                    particle.mesh.scale.setScalar(1 + Math.sin(this.time * 2) * 0.1);
                    break;
                    
                case 'ring':
                    particle.mesh.rotation.x += particle.mesh.userData.rotationSpeed.x;
                    particle.mesh.rotation.y += particle.mesh.userData.rotationSpeed.y;
                    particle.mesh.rotation.z += particle.mesh.userData.rotationSpeed.z;
                    break;
            }
        });
        
        requestAnimationFrame(() => this.animate());
    }
}