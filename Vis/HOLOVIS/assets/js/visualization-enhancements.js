import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.159.0/build/three.module.js';

export class VisualizationEnhancements {
    static createGlowMaterial(color, intensity = 0.5) {
        return new THREE.MeshPhongMaterial({
            color: color,
            emissive: color,
            emissiveIntensity: intensity,
            transparent: true,
            opacity: 0.9
        });
    }

    static createParticleSystem(count, color, radius) {
        const geometry = new THREE.BufferGeometry();
        const positions = new Float32Array(count * 3);
        
        for (let i = 0; i < count * 3; i += 3) {
            const theta = Math.random() * Math.PI * 2;
            const phi = Math.random() * Math.PI;
            const r = radius * (0.5 + Math.random() * 0.5);
            
            positions[i] = r * Math.sin(phi) * Math.cos(theta);
            positions[i + 1] = r * Math.sin(phi) * Math.sin(theta);
            positions[i + 2] = r * Math.cos(phi);
        }
        
        geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
        
        const material = new THREE.PointsMaterial({
            color: color,
            size: 2,
            transparent: true,
            opacity: 0.6,
            blending: THREE.AdditiveBlending
        });
        
        return new THREE.Points(geometry, material);
    }

    static createConnectionCurve(start, end, color = 0x00ff88) {
        const midPoint = new THREE.Vector3(
            (start.x + end.x) / 2,
            (start.y + end.y) / 2 + 50,
            (start.z + end.z) / 2
        );
        
        const curve = new THREE.QuadraticBezierCurve3(start, midPoint, end);
        const points = curve.getPoints(50);
        
        const geometry = new THREE.BufferGeometry().setFromPoints(points);
        const material = new THREE.LineBasicMaterial({
            color: color,
            opacity: 0.6,
            transparent: true,
            linewidth: 2
        });
        
        return new THREE.Line(geometry, material);
    }

    static createNestedCubeStructure(data, size = 100) {
        const group = new THREE.Group();
        
        // Outer cube (transparent)
        const outerGeometry = new THREE.BoxGeometry(size, size, size);
        const outerMaterial = new THREE.MeshPhongMaterial({
            color: 0x00ff88,
            transparent: true,
            opacity: 0.2,
            side: THREE.DoubleSide
        });
        const outerCube = new THREE.Mesh(outerGeometry, outerMaterial);
        
        // Inner cubes for categories
        const innerSize = size * 0.3;
        const positions = [
            { x: -innerSize, y: innerSize, z: innerSize },
            { x: innerSize, y: innerSize, z: innerSize },
            { x: -innerSize, y: -innerSize, z: innerSize },
            { x: innerSize, y: -innerSize, z: innerSize },
            { x: -innerSize, y: innerSize, z: -innerSize },
            { x: innerSize, y: innerSize, z: -innerSize },
            { x: -innerSize, y: -innerSize, z: -innerSize },
            { x: innerSize, y: -innerSize, z: -innerSize }
        ];
        
        const categoryColors = {
            scripts: 0x4CAF50,
            scenes: 0x2196F3,
            prefabs: 0xFF9800,
            materials: 0x9C27B0,
            vfx: 0x00BCD4,
            shaders: 0xF44336
        };
        
        let index = 0;
        Object.entries(categoryColors).forEach(([category, color]) => {
            if (data[category] && data[category].length > 0 && index < positions.length) {
                const innerGeometry = new THREE.BoxGeometry(innerSize, innerSize, innerSize);
                const innerMaterial = this.createGlowMaterial(color, 0.3);
                const innerCube = new THREE.Mesh(innerGeometry, innerMaterial);
                
                innerCube.position.set(
                    positions[index].x * 0.7,
                    positions[index].y * 0.7,
                    positions[index].z * 0.7
                );
                
                innerCube.userData = {
                    type: 'category',
                    name: category,
                    count: data[category].length
                };
                
                group.add(innerCube);
                index++;
            }
        });
        
        group.add(outerCube);
        return group;
    }

    static create3DFlowChart(scenes, scripts) {
        const group = new THREE.Group();
        const sceneHeight = 100;
        const scriptOffset = 150;
        
        scenes.forEach((scene, sceneIndex) => {
            // Scene platform
            const platformGeometry = new THREE.CylinderGeometry(80, 80, 10, 32);
            const platformMaterial = this.createGlowMaterial(0x2196F3, 0.4);
            const platform = new THREE.Mesh(platformGeometry, platformMaterial);
            
            platform.position.set(
                sceneIndex * 300 - (scenes.length * 150),
                sceneIndex * sceneHeight,
                0
            );
            
            platform.userData = {
                type: 'scene',
                name: scene.name,
                path: scene.path
            };
            
            group.add(platform);
            
            // Add floating scripts around the scene
            const relatedScripts = scripts.filter(s => 
                s.path.toLowerCase().includes(scene.name.toLowerCase().replace('.unity', ''))
            );
            
            relatedScripts.forEach((script, scriptIndex) => {
                const angle = (scriptIndex / relatedScripts.length) * Math.PI * 2;
                const scriptGeometry = new THREE.OctahedronGeometry(20);
                const scriptMaterial = this.createGlowMaterial(0x4CAF50, 0.5);
                const scriptMesh = new THREE.Mesh(scriptGeometry, scriptMaterial);
                
                scriptMesh.position.set(
                    platform.position.x + Math.cos(angle) * scriptOffset,
                    platform.position.y + 30,
                    platform.position.z + Math.sin(angle) * scriptOffset
                );
                
                scriptMesh.userData = {
                    type: 'script',
                    name: script.name,
                    path: script.path
                };
                
                group.add(scriptMesh);
                
                // Connection
                const connection = this.createConnectionCurve(
                    platform.position,
                    scriptMesh.position,
                    0x4CAF50
                );
                group.add(connection);
            });
        });
        
        return group;
    }

    static animateRotation(object, speed = 0.001) {
        object.rotation.y += speed;
        if (object.children) {
            object.children.forEach(child => {
                if (child.type === 'Mesh') {
                    child.rotation.y -= speed * 0.5;
                }
            });
        }
    }

    static animateFloat(object, time, amplitude = 10, speed = 1) {
        object.position.y += Math.sin(time * speed) * amplitude * 0.01;
    }
}