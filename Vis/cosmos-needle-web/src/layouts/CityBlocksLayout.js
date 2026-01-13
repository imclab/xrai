import * as THREE from 'three';

export class CityBlocksLayout {
    constructor(scene) {
        this.scene = scene;
        this.group = new THREE.Group();
        this.scene.add(this.group);
        this.buildings = [];
    }
    
    generate(graphData) {
        this.clear();
        
        // Convert graph to hierarchical structure
        const hierarchy = this.buildHierarchy(graphData);
        
        // Generate city blocks
        this.generateBlocks(hierarchy, 0, 0, 100);
        
        // Center the city
        const box = new THREE.Box3().setFromObject(this.group);
        const center = box.getCenter(new THREE.Vector3());
        this.group.position.sub(center);
        
        // Add ground plane
        const groundGeometry = new THREE.PlaneGeometry(200, 200);
        const groundMaterial = new THREE.MeshStandardMaterial({ 
            color: 0x222222,
            roughness: 0.8
        });
        const ground = new THREE.Mesh(groundGeometry, groundMaterial);
        ground.rotation.x = -Math.PI / 2;
        ground.position.y = -0.1;
        this.group.add(ground);
        
        // Add grid
        const gridHelper = new THREE.GridHelper(200, 50, 0x444444, 0x222222);
        this.group.add(gridHelper);
    }
    
    buildHierarchy(graphData) {
        // Group nodes by type/category
        const groups = new Map();
        
        graphData.nodes.forEach(node => {
            const type = node.type || 'default';
            if (!groups.has(type)) {
                groups.set(type, []);
            }
            groups.get(type).push(node);
        });
        
        return groups;
    }
    
    generateBlocks(hierarchy, startX, startZ, blockSize) {
        let x = startX;
        let z = startZ;
        const padding = 5;
        const blockColors = {
            icosa: 0xFF6B6B,
            objaverse: 0x4ECDC4,
            github: 0x95E1D3,
            local: 0xF38181,
            web: 0xAA96DA,
            default: 0x888888
        };
        
        hierarchy.forEach((nodes, type) => {
            const blockColor = blockColors[type] || blockColors.default;
            
            // Create district for this type
            const districtSize = Math.ceil(Math.sqrt(nodes.length));
            const buildingSize = (blockSize - padding * (districtSize - 1)) / districtSize;
            
            nodes.forEach((node, idx) => {
                const row = Math.floor(idx / districtSize);
                const col = idx % districtSize;
                
                const posX = x + col * (buildingSize + padding);
                const posZ = z + row * (buildingSize + padding);
                
                // Building height based on node importance/value
                const height = 5 + (node.val || 1) * 10;
                
                // Create building
                const building = this.createBuilding(
                    posX, 
                    posZ, 
                    buildingSize * 0.8, 
                    height, 
                    blockColor,
                    node
                );
                
                this.buildings.push(building);
                this.group.add(building);
            });
            
            // Move to next district
            x += blockSize + padding * 2;
            if (x > 100) {
                x = startX;
                z += blockSize + padding * 2;
            }
        });
    }
    
    createBuilding(x, z, size, height, color, nodeData) {
        const group = new THREE.Group();
        
        // Base building
        const geometry = new THREE.BoxGeometry(size, height, size);
        const material = new THREE.MeshStandardMaterial({ 
            color: color,
            emissive: color,
            emissiveIntensity: 0.2
        });
        const building = new THREE.Mesh(geometry, material);
        building.position.set(x, height / 2, z);
        building.userData = nodeData;
        
        // Add windows (emissive panels)
        const windowRows = Math.floor(height / 3);
        const windowCols = 4;
        const windowGeometry = new THREE.PlaneGeometry(size * 0.15, 1);
        const windowMaterial = new THREE.MeshBasicMaterial({
            color: 0xFFFFCC,
            emissive: 0xFFFFCC,
            emissiveIntensity: 1
        });
        
        for (let row = 0; row < windowRows; row++) {
            for (let col = 0; col < windowCols; col++) {
                for (let face = 0; face < 4; face++) {
                    const window = new THREE.Mesh(windowGeometry, windowMaterial);
                    const angle = (face * Math.PI / 2);
                    const radius = size / 2 + 0.01;
                    
                    window.position.set(
                        x + Math.cos(angle) * radius,
                        row * 3 + 2,
                        z + Math.sin(angle) * radius
                    );
                    window.rotation.y = angle;
                    
                    if (Math.random() > 0.3) { // Random window lighting
                        group.add(window);
                    }
                }
            }
        }
        
        // Add label
        if (nodeData.name) {
            // Would add text label here - requires TextGeometry or sprite
        }
        
        group.add(building);
        
        // Make clickable
        building.userData.clickable = true;
        building.userData.onClick = () => {
            console.log('Building clicked:', nodeData);
            // Animate building
            this.animateBuilding(building);
        };
        
        return group;
    }
    
    animateBuilding(building) {
        const startY = building.position.y;
        const endY = startY + 2;
        let progress = 0;
        
        const animate = () => {
            progress += 0.05;
            if (progress <= 1) {
                building.position.y = startY + Math.sin(progress * Math.PI) * (endY - startY);
                requestAnimationFrame(animate);
            }
        };
        
        animate();
    }
    
    clear() {
        this.buildings = [];
        while (this.group.children.length > 0) {
            this.group.remove(this.group.children[0]);
        }
    }
}