import * as THREE from 'three';
import { gsap } from 'gsap';
import { CityBlocksLayout } from './layouts/CityBlocksLayout.js';
import { CosmosLayout } from './layouts/CosmosLayout.js';
import { TreeLayout } from './layouts/TreeLayout.js';
import { MassiveDataLayout } from './layouts/MassiveDataLayout.js';
import { MetavidoVFX } from './effects/MetavidoVFX.js';

export class VisualizationManager {
    constructor(scene, camera) {
        this.scene = scene;
        this.camera = camera;
        
        // Layout instances
        this.cityLayout = new CityBlocksLayout(scene);
        this.cosmosLayout = new CosmosLayout(scene);
        this.treeLayout = new TreeLayout(scene);
        this.massiveLayout = new MassiveDataLayout(scene);
        this.metavidoVFX = new MetavidoVFX(scene);
        
        this.currentLayout = null;
    }
    
    async createCityBlocks(graphData) {
        this.clear();
        this.currentLayout = this.cityLayout;
        await this.cityLayout.generate(graphData);
        
        // Animate camera to city view
        gsap.to(this.camera.position, {
            x: 100,
            y: 100,
            z: 100,
            duration: 2,
            ease: 'power2.inOut',
            onUpdate: () => this.camera.lookAt(0, 0, 0)
        });
    }
    
    async createCosmos(graphData) {
        this.clear();
        
        // Automatically choose best layout based on data size
        if (graphData.nodes.length > 10000) {
            console.log(`Large dataset detected: ${graphData.nodes.length} nodes. Using MassiveDataLayout.`);
            this.currentLayout = this.massiveLayout;
            await this.massiveLayout.generate(graphData, this.camera);
        } else {
            this.currentLayout = this.cosmosLayout;
            await this.cosmosLayout.generate(graphData);
        }
        
        // Animate camera to cosmos view
        gsap.to(this.camera.position, {
            x: 0,
            y: 50,
            z: 150,
            duration: 2,
            ease: 'power2.inOut',
            onUpdate: () => this.camera.lookAt(0, 0, 0)
        });
    }
    
    async createTree(graphData) {
        this.clear();
        this.currentLayout = this.treeLayout;
        await this.treeLayout.generate(graphData);
        
        // Animate camera to tree view
        gsap.to(this.camera.position, {
            x: 0,
            y: 80,
            z: 120,
            duration: 2,
            ease: 'power2.inOut',
            onUpdate: () => this.camera.lookAt(0, 0, 0)
        });
    }
    
    showMetavidoVFX() {
        this.clear();
        this.metavidoVFX.show();
        
        // Optimal camera position for VFX
        gsap.to(this.camera.position, {
            x: 30,
            y: 30,
            z: 80,
            duration: 2,
            ease: 'power2.inOut',
            onUpdate: () => this.camera.lookAt(0, 0, 0)
        });
    }
    
    clear() {
        // Hide all layouts
        this.cityLayout.clear();
        this.cosmosLayout.clear();
        this.treeLayout.clear();
        this.massiveLayout.clear();
        this.metavidoVFX.hide();
        
        this.currentLayout = null;
    }
    
    // Fast update methods for streaming results
    async updateCosmos(graphData) {
        if (this.currentLayout === this.cosmosLayout) {
            await this.cosmosLayout.updateDynamic(graphData);
        } else if (this.currentLayout === this.massiveLayout) {
            await this.massiveLayout.updateDynamic(graphData);
        } else {
            // Switch to cosmos if no current layout
            await this.createCosmos(graphData);
        }
    }
    
    async updateCityBlocks(graphData) {
        if (this.currentLayout === this.cityLayout) {
            await this.cityLayout.updateDynamic(graphData);
        } else {
            await this.createCityBlocks(graphData);
        }
    }
    
    async updateTree(graphData) {
        if (this.currentLayout === this.treeLayout) {
            await this.treeLayout.updateDynamic(graphData);
        } else {
            await this.createTree(graphData);
        }
    }
    
    update() {
        // Update current layout
        if (this.currentLayout && this.currentLayout.update) {
            this.currentLayout.update();
        }
        
        // Update VFX if visible
        this.metavidoVFX.update();
    }
}