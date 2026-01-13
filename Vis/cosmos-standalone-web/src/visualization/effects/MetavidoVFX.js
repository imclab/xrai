import * as THREE from 'three';
import { gsap } from 'gsap';

export class MetavidoVFX {
    constructor(scene) {
        this.scene = scene;
        this.group = new THREE.Group();
        this.group.name = 'MetavidoVFX';
        this.particles = [];
        this.time = 0;
        this.isVisible = false;
        
        this.init();
    }
    
    init() {
        // Create multiple particle effects
        this.createFlowField();
        this.createOrbitingParticles();
        this.createEnergyCore();
        this.createPlasmaRings();
        this.createAuroraEffect();
        
        // Initially hidden
        this.group.visible = false;
    }
    
    createFlowField() {
        const particleCount = 10000;
        const positions = new Float32Array(particleCount * 3);
        const colors = new Float32Array(particleCount * 3);
        const sizes = new Float32Array(particleCount);
        const randoms = new Float32Array(particleCount);
        
        for (let i = 0; i < particleCount; i++) {
            // Distribute in sphere
            const theta = Math.random() * Math.PI * 2;
            const phi = Math.acos(1 - 2 * Math.random());
            const radius = 20 + Math.random() * 40;
            
            positions[i * 3] = radius * Math.sin(phi) * Math.cos(theta);
            positions[i * 3 + 1] = radius * Math.sin(phi) * Math.sin(theta);
            positions[i * 3 + 2] = radius * Math.cos(phi);
            
            // Gradient colors
            const t = i / particleCount;
            colors[i * 3] = 0.5 + 0.5 * Math.sin(t * Math.PI);
            colors[i * 3 + 1] = 0.3 + 0.3 * Math.sin(t * Math.PI + 1);
            colors[i * 3 + 2] = 0.8 + 0.2 * Math.sin(t * Math.PI + 2);
            
            sizes[i] = Math.random() * 3 + 1;
            randoms[i] = Math.random();
        }
        
        const geometry = new THREE.BufferGeometry();
        geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
        geometry.setAttribute('color', new THREE.BufferAttribute(colors, 3));
        geometry.setAttribute('size', new THREE.BufferAttribute(sizes, 1));
        geometry.setAttribute('random', new THREE.BufferAttribute(randoms, 1));
        
        const material = new THREE.ShaderMaterial({
            uniforms: {
                time: { value: 0 },
                pixelRatio: { value: window.devicePixelRatio }
            },
            vertexShader: `
                attribute float size;
                attribute float random;
                varying vec3 vColor;
                uniform float time;
                uniform float pixelRatio;
                
                void main() {
                    vColor = color;
                    vec3 pos = position;
                    
                    // Flow field animation
                    float flow = time * 0.1;
                    pos.x += sin(pos.y * 0.1 + flow + random * 6.28) * 10.0;
                    pos.y += sin(pos.z * 0.1 + flow * 1.5 + random * 6.28) * 5.0;
                    pos.z += cos(pos.x * 0.1 + flow * 2.0 + random * 6.28) * 10.0;
                    
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
        this.particles.push({ mesh: flowField, material, type: 'flow' });
        this.group.add(flowField);
    }
    
    createOrbitingParticles() {
        for (let orbit = 0; orbit < 5; orbit++) {
            const particlesPerOrbit = 50;
            const positions = new Float32Array(particlesPerOrbit * 3);
            const radius = 10 + orbit * 8;
            const color = new THREE.Color().setHSL(orbit / 5 * 0.3, 1, 0.6);
            
            for (let i = 0; i < particlesPerOrbit; i++) {
                const angle = (i / particlesPerOrbit) * Math.PI * 2;
                positions[i * 3] = Math.cos(angle) * radius;
                positions[i * 3 + 1] = 0;
                positions[i * 3 + 2] = Math.sin(angle) * radius;
            }
            
            const geometry = new THREE.BufferGeometry();
            geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
            
            const material = new THREE.PointsMaterial({
                size: 4,
                color: color,
                blending: THREE.AdditiveBlending,
                transparent: true,
                opacity: 0.8,
                map: this.createGlowTexture()
            });
            
            const points = new THREE.Points(geometry, material);
            points.userData = { 
                orbit, 
                speed: 0.5 + orbit * 0.2,
                tilt: Math.random() * 0.5
            };
            
            this.particles.push({ mesh: points, type: 'orbit' });
            this.group.add(points);
        }
    }
    
    createEnergyCore() {
        // Inner core
        const coreGeometry = new THREE.IcosahedronGeometry(5, 3);
        const coreMaterial = new THREE.MeshBasicMaterial({
            color: 0xffffff,
            emissive: 0xffffff,
            emissiveIntensity: 2
        });
        const core = new THREE.Mesh(coreGeometry, coreMaterial);
        
        // Outer shell
        const shellGeometry = new THREE.IcosahedronGeometry(8, 1);
        const shellMaterial = new THREE.MeshBasicMaterial({
            color: 0x00ffff,
            wireframe: true,
            transparent: true,
            opacity: 0.3
        });
        const shell = new THREE.Mesh(shellGeometry, shellMaterial);
        
        // Energy field
        const fieldGeometry = new THREE.SphereGeometry(10, 32, 32);
        const fieldMaterial = new THREE.ShaderMaterial({
            uniforms: {
                time: { value: 0 }
            },
            vertexShader: `
                varying vec3 vNormal;
                varying vec3 vPosition;
                
                void main() {
                    vNormal = normal;
                    vPosition = position;
                    gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
                }
            `,
            fragmentShader: `
                uniform float time;
                varying vec3 vNormal;
                varying vec3 vPosition;
                
                void main() {
                    float intensity = pow(0.6 - dot(vNormal, vec3(0, 0, 1.0)), 2.0);
                    vec3 color = vec3(0.3, 0.6, 1.0) * intensity;
                    
                    // Energy waves
                    float wave = sin(vPosition.y * 2.0 + time * 3.0) * 0.5 + 0.5;
                    color *= 1.0 + wave * 0.5;
                    
                    gl_FragColor = vec4(color, intensity * 0.7);
                }
            `,
            transparent: true,
            blending: THREE.AdditiveBlending,
            side: THREE.BackSide
        });
        const field = new THREE.Mesh(fieldGeometry, fieldMaterial);
        
        const coreGroup = new THREE.Group();
        coreGroup.add(core);
        coreGroup.add(shell);
        coreGroup.add(field);
        
        this.particles.push({ 
            mesh: coreGroup, 
            type: 'core',
            material: fieldMaterial
        });
        this.group.add(coreGroup);
    }
    
    createPlasmaRings() {
        for (let i = 0; i < 3; i++) {
            const radius = 15 + i * 10;
            const tubeRadius = 1 + i * 0.5;
            const geometry = new THREE.TorusGeometry(radius, tubeRadius, 16, 100);
            
            const material = new THREE.ShaderMaterial({
                uniforms: {
                    time: { value: 0 },
                    color1: { value: new THREE.Color(0xff0080) },
                    color2: { value: new THREE.Color(0x0080ff) }
                },
                vertexShader: `
                    varying vec3 vPosition;
                    
                    void main() {
                        vPosition = position;
                        gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
                    }
                `,
                fragmentShader: `
                    uniform float time;
                    uniform vec3 color1;
                    uniform vec3 color2;
                    varying vec3 vPosition;
                    
                    void main() {
                        float t = sin(vPosition.x * 0.1 + time * 2.0) * 0.5 + 0.5;
                        vec3 color = mix(color1, color2, t);
                        float alpha = 0.8 + sin(time * 3.0 + vPosition.y * 0.2) * 0.2;
                        gl_FragColor = vec4(color, alpha);
                    }
                `,
                transparent: true,
                blending: THREE.AdditiveBlending,
                side: THREE.DoubleSide
            });
            
            const ring = new THREE.Mesh(geometry, material);
            ring.rotation.x = Math.random() * Math.PI;
            ring.rotation.y = Math.random() * Math.PI;
            ring.userData = {
                rotationSpeed: new THREE.Vector3(
                    (Math.random() - 0.5) * 0.02,
                    (Math.random() - 0.5) * 0.02,
                    (Math.random() - 0.5) * 0.02
                )
            };
            
            this.particles.push({ mesh: ring, type: 'plasma', material });
            this.group.add(ring);
        }
    }
    
    createAuroraEffect() {
        const geometry = new THREE.PlaneGeometry(100, 50, 50, 25);
        const material = new THREE.ShaderMaterial({
            uniforms: {
                time: { value: 0 }
            },
            vertexShader: `
                varying vec2 vUv;
                varying float vY;
                uniform float time;
                
                void main() {
                    vUv = uv;
                    vY = position.y;
                    
                    vec3 pos = position;
                    pos.z += sin(pos.x * 0.1 + time) * 5.0;
                    pos.y += sin(pos.x * 0.05 + time * 0.5) * 3.0;
                    
                    gl_Position = projectionMatrix * modelViewMatrix * vec4(pos, 1.0);
                }
            `,
            fragmentShader: `
                uniform float time;
                varying vec2 vUv;
                varying float vY;
                
                void main() {
                    vec3 color1 = vec3(0.0, 1.0, 0.3);
                    vec3 color2 = vec3(0.3, 0.0, 1.0);
                    
                    float t = sin(vUv.x * 10.0 + time * 2.0) * 0.5 + 0.5;
                    vec3 color = mix(color1, color2, t);
                    
                    float alpha = smoothstep(0.0, 1.0, vUv.y) * 0.3;
                    alpha *= sin(vUv.x * 20.0 + time * 3.0) * 0.5 + 0.5;
                    
                    gl_FragColor = vec4(color, alpha);
                }
            `,
            transparent: true,
            blending: THREE.AdditiveBlending,
            side: THREE.DoubleSide
        });
        
        const aurora = new THREE.Mesh(geometry, material);
        aurora.position.y = 30;
        aurora.rotation.x = -Math.PI / 4;
        
        this.particles.push({ mesh: aurora, type: 'aurora', material });
        this.group.add(aurora);
    }
    
    createGlowTexture() {
        const canvas = document.createElement('canvas');
        canvas.width = 64;
        canvas.height = 64;
        const ctx = canvas.getContext('2d');
        
        const gradient = ctx.createRadialGradient(32, 32, 0, 32, 32, 32);
        gradient.addColorStop(0, 'rgba(255, 255, 255, 1)');
        gradient.addColorStop(0.5, 'rgba(255, 255, 255, 0.5)');
        gradient.addColorStop(1, 'rgba(255, 255, 255, 0)');
        
        ctx.fillStyle = gradient;
        ctx.fillRect(0, 0, 64, 64);
        
        const texture = new THREE.CanvasTexture(canvas);
        return texture;
    }
    
    show() {
        this.isVisible = true;
        this.scene.add(this.group);
        this.group.visible = true;
        
        // Animate entrance
        this.group.scale.set(0.1, 0.1, 0.1);
        gsap.to(this.group.scale, {
            x: 1, y: 1, z: 1,
            duration: 2,
            ease: 'power2.out'
        });
    }
    
    hide() {
        this.isVisible = false;
        this.scene.remove(this.group);
        this.group.visible = false;
    }
    
    update() {
        if (!this.isVisible) return;
        
        this.time += 0.01;
        
        this.particles.forEach(particle => {
            // Update shader uniforms
            if (particle.material && particle.material.uniforms) {
                particle.material.uniforms.time.value = this.time;
            }
            
            // Type-specific animations
            switch (particle.type) {
                case 'orbit':
                    particle.mesh.rotation.y += particle.mesh.userData.speed * 0.01;
                    particle.mesh.rotation.x = Math.sin(this.time * particle.mesh.userData.tilt) * 0.3;
                    break;
                    
                case 'core':
                    particle.mesh.rotation.x += 0.005;
                    particle.mesh.rotation.y += 0.003;
                    particle.mesh.scale.setScalar(1 + Math.sin(this.time * 2) * 0.05);
                    break;
                    
                case 'plasma':
                    particle.mesh.rotation.x += particle.mesh.userData.rotationSpeed.x;
                    particle.mesh.rotation.y += particle.mesh.userData.rotationSpeed.y;
                    particle.mesh.rotation.z += particle.mesh.userData.rotationSpeed.z;
                    break;
                    
                case 'aurora':
                    particle.mesh.position.z = Math.sin(this.time * 0.5) * 10;
                    break;
            }
        });
    }
}