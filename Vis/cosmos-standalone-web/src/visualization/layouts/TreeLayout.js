import * as THREE from 'three';
import { gsap } from 'gsap';

export class TreeLayout {
    constructor(scene) {
        this.scene = scene;
        this.group = new THREE.Group();
        this.group.name = 'TreeLayout';
        this.nodes = [];
        this.edges = [];
    }
    
    async generate(graphData) {
        this.clear();
        this.scene.add(this.group);
        
        // Build hierarchical structure
        const tree = this.buildTree(graphData);
        
        // Layout tree in 3D space
        this.layoutTree(tree, 0, 0, 0, 60, 0);
        
        // Create visual elements
        this.visualizeTree(tree);
        
        // Animate entrance
        this.animateEntrance();
    }
    
    buildTree(graphData) {
        // Find root nodes (nodes with no incoming edges)
        const hasIncoming = new Set();
        graphData.links.forEach(link => {
            hasIncoming.add(link.target);
        });
        
        const roots = graphData.nodes.filter(node => !hasIncoming.has(node.id));
        
        // Build adjacency list
        const children = new Map();
        graphData.links.forEach(link => {
            if (!children.has(link.source)) {
                children.set(link.source, []);
            }
            children.get(link.source).push(link.target);
        });
        
        // Create node map for quick lookup
        const nodeMap = new Map();
        graphData.nodes.forEach(node => {
            nodeMap.set(node.id, node);
        });
        
        // Recursive function to build tree
        const buildNode = (nodeId, depth = 0, visited = new Set()) => {
            if (visited.has(nodeId)) return null;
            visited.add(nodeId);
            
            const node = nodeMap.get(nodeId);
            if (!node) return null;
            
            const treeNode = {
                ...node,
                depth,
                children: []
            };
            
            const nodeChildren = children.get(nodeId) || [];
            treeNode.children = nodeChildren
                .map(childId => buildNode(childId, depth + 1, visited))
                .filter(child => child !== null);
            
            return treeNode;
        };
        
        // If multiple roots, create artificial root
        if (roots.length === 0) {
            // No clear root, pick first node
            return buildNode(graphData.nodes[0].id);
        } else if (roots.length === 1) {
            return buildNode(roots[0].id);
        } else {
            // Multiple roots - create super root
            return {
                id: 'super-root',
                name: 'Root',
                type: 'root',
                depth: 0,
                children: roots.map(root => buildNode(root.id, 1))
            };
        }
    }
    
    layoutTree(node, x, y, z, spread, angle) {
        if (!node) return;
        
        // Position node
        node.position = new THREE.Vector3(x, y, z);
        
        if (node.children.length === 0) return;
        
        // Calculate positions for children
        const angleStep = (Math.PI * 2) / node.children.length;
        const verticalStep = 30;
        const childSpread = spread * 0.8;
        
        node.children.forEach((child, index) => {
            const childAngle = angle + angleStep * index - (angleStep * (node.children.length - 1)) / 2;
            const childX = x + Math.cos(childAngle) * spread;
            const childY = y - verticalStep;
            const childZ = z + Math.sin(childAngle) * spread;
            
            this.layoutTree(child, childX, childY, childZ, childSpread, childAngle);
        });
    }
    
    visualizeTree(root) {
        if (!root) return;
        
        const visited = new Set();
        
        const visualizeNode = (node, parentPosition = null) => {
            if (!node || visited.has(node.id)) return;
            visited.add(node.id);
            
            // Create node sphere
            const nodeGroup = new THREE.Group();
            
            // Node geometry
            const radius = 3 + (node.val || 1) * 2;
            const geometry = new THREE.SphereGeometry(radius, 32, 16);
            
            // Node material with type-based color
            const colors = {
                icosa: 0xFF6B6B,
                objaverse: 0x4ECDC4,
                github: 0x95E1D3,
                local: 0xF38181,
                web: 0xAA96DA,
                root: 0xFFFFFF
            };
            
            const material = new THREE.MeshStandardMaterial({
                color: colors[node.type] || 0x888888,
                emissive: colors[node.type] || 0x888888,
                emissiveIntensity: 0.2,
                roughness: 0.5,
                metalness: 0.3
            });
            
            const sphere = new THREE.Mesh(geometry, material);
            sphere.position.copy(node.position);
            sphere.castShadow = true;
            sphere.receiveShadow = true;
            sphere.userData.nodeData = node;
            
            nodeGroup.add(sphere);
            
            // Add glow effect
            const glowGeometry = new THREE.SphereGeometry(radius * 1.2, 16, 8);
            const glowMaterial = new THREE.MeshBasicMaterial({
                color: colors[node.type] || 0x888888,
                transparent: true,
                opacity: 0.1,
                side: THREE.BackSide
            });
            const glow = new THREE.Mesh(glowGeometry, glowMaterial);
            glow.position.copy(node.position);
            nodeGroup.add(glow);
            
            this.nodes.push(nodeGroup);
            this.group.add(nodeGroup);
            
            // Create edge to parent
            if (parentPosition) {
                const edge = this.createEdge(parentPosition, node.position, node.type);
                this.edges.push(edge);
                this.group.add(edge);
            }
            
            // Visualize children
            node.children.forEach(child => {
                visualizeNode(child, node.position);
            });
        };
        
        visualizeNode(root);
    }
    
    createEdge(start, end, type) {
        // Create curved edge
        const midPoint = new THREE.Vector3(
            (start.x + end.x) / 2,
            (start.y + end.y) / 2 + 10,
            (start.z + end.z) / 2
        );
        
        const curve = new THREE.QuadraticBezierCurve3(start, midPoint, end);
        const points = curve.getPoints(30);
        const geometry = new THREE.BufferGeometry().setFromPoints(points);
        
        // Edge color based on connection type
        const colors = {
            icosa: 0xFF6B6B,
            objaverse: 0x4ECDC4,
            github: 0x95E1D3,
            local: 0xF38181,
            web: 0xAA96DA
        };
        
        const material = new THREE.LineBasicMaterial({
            color: colors[type] || 0x4444ff,
            transparent: true,
            opacity: 0.6,
            linewidth: 2
        });
        
        const line = new THREE.Line(geometry, material);
        
        // Add flow effect
        const flowMaterial = new THREE.LineBasicMaterial({
            color: 0xffffff,
            transparent: true,
            opacity: 0,
            linewidth: 3
        });
        const flowLine = new THREE.Line(geometry, flowMaterial);
        line.add(flowLine);
        
        return line;
    }
    
    animateEntrance() {
        // Animate nodes appearing
        this.nodes.forEach((nodeGroup, index) => {
            const sphere = nodeGroup.children[0];
            const originalY = sphere.position.y;
            
            sphere.position.y = originalY + 50;
            sphere.scale.set(0.1, 0.1, 0.1);
            
            gsap.to(sphere.position, {
                y: originalY,
                duration: 1,
                delay: index * 0.05,
                ease: 'bounce.out'
            });
            
            gsap.to(sphere.scale, {
                x: 1,
                y: 1,
                z: 1,
                duration: 0.5,
                delay: index * 0.05,
                ease: 'back.out'
            });
        });
        
        // Animate edges drawing
        this.edges.forEach((edge, index) => {
            edge.material.opacity = 0;
            gsap.to(edge.material, {
                opacity: 0.6,
                duration: 0.5,
                delay: 0.5 + index * 0.02
            });
        });
    }
    
    update() {
        // Optional: Add floating animation to nodes
        this.nodes.forEach((nodeGroup, index) => {
            const sphere = nodeGroup.children[0];
            sphere.rotation.y += 0.01;
            
            // Gentle floating
            sphere.position.y += Math.sin(Date.now() * 0.001 + index) * 0.01;
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
        
        // Reset
        this.nodes = [];
        this.edges = [];
        
        while (this.group.children.length > 0) {
            this.group.remove(this.group.children[0]);
        }
    }
}