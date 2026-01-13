import * as THREE from 'three';
import { gsap } from 'gsap';

export class CityBlocksLayout {
    constructor(scene) {
        this.scene = scene;
        this.group = new THREE.Group();
        this.group.name = 'CityBlocks';
        this.buildings = [];
        this.lights = [];
    }
    
    async generate(graphData) {
        this.clear();
        this.scene.add(this.group);
        
        // Group nodes by type for districts
        const districts = this.createDistricts(graphData);
        
        // Create ground
        this.createGround();
        
        // Generate city blocks
        await this.generateDistricts(districts);
        
        // Add city lights
        this.addCityLights();
        
        // Add connections as roads/bridges
        this.createConnections(graphData.links);
        
        // Animate entrance
        this.animateEntrance();
    }
    
    createDistricts(graphData) {
        const districts = new Map();
        
        graphData.nodes.forEach(node => {
            const type = node.type || 'default';
            if (!districts.has(type)) {
                districts.set(type, {
                    name: type,
                    nodes: [],
                    color: this.getDistrictColor(type)
                });
            }
            districts.get(type).nodes.push(node);
        });
        
        return districts;
    }
    
    getDistrictColor(type) {
        const colors = {
            icosa: 0xFF6B6B,
            objaverse: 0x4ECDC4,
            github: 0x95E1D3,
            local: 0xF38181,
            web: 0xAA96DA,
            default: 0x888888
        };
        return colors[type] || colors.default;
    }
    
    createGround() {
        // Main ground plane
        const groundGeometry = new THREE.PlaneGeometry(300, 300);
        const groundMaterial = new THREE.MeshStandardMaterial({ 
            color: 0x111111,
            roughness: 0.9,
            metalness: 0.1
        });
        const ground = new THREE.Mesh(groundGeometry, groundMaterial);
        ground.rotation.x = -Math.PI / 2;
        ground.receiveShadow = true;
        this.group.add(ground);
        
        // Grid lines for streets
        const gridHelper = new THREE.GridHelper(300, 30, 0x222222, 0x111111);
        this.group.add(gridHelper);
    }
    
    async generateDistricts(districts) {
        const districtSize = 80;
        const padding = 20;
        let districtIndex = 0;
        
        districts.forEach((district, type) => {
            const row = Math.floor(districtIndex / 3);
            const col = districtIndex % 3;
            
            const x = (col - 1) * (districtSize + padding);
            const z = (row - 1) * (districtSize + padding);
            
            this.createDistrictBlock(district, x, z, districtSize);
            districtIndex++;
        });
    }
    
    createDistrictBlock(district, offsetX, offsetZ, size) {
        const blockSize = size / Math.ceil(Math.sqrt(district.nodes.length));
        
        district.nodes.forEach((node, index) => {
            const row = Math.floor(index / Math.ceil(Math.sqrt(district.nodes.length)));
            const col = index % Math.ceil(Math.sqrt(district.nodes.length));
            
            const x = offsetX + (col - Math.floor(Math.sqrt(district.nodes.length) / 2)) * blockSize;
            const z = offsetZ + (row - Math.floor(Math.sqrt(district.nodes.length) / 2)) * blockSize;
            
            const building = this.createBuilding(node, x, z, blockSize * 0.8, district.color);
            this.buildings.push(building);
            this.group.add(building);
        });
    }
    
    createBuilding(node, x, z, size, color) {
        const group = new THREE.Group();
        
        // Building height based on node importance
        const height = 10 + (node.val || 1) * 15 + Math.random() * 10;
        
        // Main structure
        const buildingGeometry = new THREE.BoxGeometry(size, height, size);
        const buildingMaterial = new THREE.MeshStandardMaterial({
            color: color,
            roughness: 0.7,
            metalness: 0.3
        });
        const building = new THREE.Mesh(buildingGeometry, buildingMaterial);
        building.position.set(x, height / 2, z);
        building.castShadow = true;
        building.receiveShadow = true;
        building.userData.nodeData = node;
        
        group.add(building);
        
        // Windows with emission
        const windowRows = Math.floor(height / 3);
        const windowCols = 4;
        
        for (let row = 0; row < windowRows; row++) {
            for (let col = 0; col < windowCols; col++) {
                if (Math.random() > 0.3) { // Random lit windows
                    const windowLight = this.createWindow(
                        x + (col - 1.5) * (size / 4),
                        row * 3 + 2,
                        z + size / 2 + 0.1,
                        size / 8,
                        1
                    );
                    group.add(windowLight);
                }
            }
        }
        
        // Rooftop details
        const rooftopGeometry = new THREE.BoxGeometry(size * 0.1, 2, size * 0.1);
        const rooftopMaterial = new THREE.MeshStandardMaterial({ color: 0x333333 });
        const rooftop = new THREE.Mesh(rooftopGeometry, rooftopMaterial);
        rooftop.position.set(x, height + 1, z);
        group.add(rooftop);
        
        // Store original position for animation
        group.userData.targetY = building.position.y;
        building.position.y = -height;
        
        return group;
    }
    
    createWindow(x, y, z, width, height) {
        const windowGeometry = new THREE.PlaneGeometry(width, height);
        const windowMaterial = new THREE.MeshBasicMaterial({
            color: 0xFFFFAA,
            emissive: 0xFFFFAA,
            emissiveIntensity: 0.5
        });
        const window = new THREE.Mesh(windowGeometry, windowMaterial);
        window.position.set(x, y, z);
        
        // Add point light for glow effect
        const light = new THREE.PointLight(0xFFFFAA, 0.1, 5);
        light.position.copy(window.position);
        this.lights.push(light);
        
        return window;
    }
    
    createConnections(links) {
        // Create roads/bridges between connected buildings
        links.forEach(link => {
            const sourceBuilding = this.buildings.find(b => 
                b.children[0]?.userData.nodeData?.id === link.source
            );
            const targetBuilding = this.buildings.find(b => 
                b.children[0]?.userData.nodeData?.id === link.target
            );
            
            if (sourceBuilding && targetBuilding) {
                const start = sourceBuilding.children[0].position;
                const end = targetBuilding.children[0].position;
                
                // Create curved bridge
                const curve = new THREE.CatmullRomCurve3([
                    new THREE.Vector3(start.x, 0.1, start.z),
                    new THREE.Vector3(
                        (start.x + end.x) / 2,
                        Math.max(start.y, end.y) * 0.3,
                        (start.z + end.z) / 2
                    ),
                    new THREE.Vector3(end.x, 0.1, end.z)
                ]);
                
                const tubeGeometry = new THREE.TubeGeometry(curve, 20, 0.5, 8, false);
                const tubeMaterial = new THREE.MeshStandardMaterial({
                    color: 0x4444AA,
                    emissive: 0x4444AA,
                    emissiveIntensity: 0.2,
                    transparent: true,
                    opacity: 0.6
                });
                const bridge = new THREE.Mesh(tubeGeometry, tubeMaterial);
                this.group.add(bridge);
            }
        });
    }
    
    addCityLights() {
        // Streetlights
        for (let i = -100; i <= 100; i += 40) {
            for (let j = -100; j <= 100; j += 40) {
                const streetLight = new THREE.PointLight(0xFFAA88, 0.5, 20);
                streetLight.position.set(i, 5, j);
                this.group.add(streetLight);
            }
        }
        
        // Add window lights to scene
        this.lights.forEach(light => this.group.add(light));
    }
    
    animateEntrance() {
        // Animate buildings rising from ground
        this.buildings.forEach((building, index) => {
            const mainBuilding = building.children[0];
            const targetY = building.userData.targetY;
            
            gsap.to(mainBuilding.position, {
                y: targetY,
                duration: 1.5,
                delay: index * 0.02,
                ease: 'power2.out'
            });
        });
    }
    
    clear() {
        // Remove from scene
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
        
        // Clear arrays
        this.buildings = [];
        this.lights = [];
        
        // Clear group
        while (this.group.children.length > 0) {
            this.group.remove(this.group.children[0]);
        }
    }
    
    update() {
        // Optional: animate lights, traffic, etc.
    }
}