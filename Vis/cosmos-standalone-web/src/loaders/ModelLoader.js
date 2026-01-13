import * as THREE from 'three';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';
import { OBJLoader } from 'three/examples/jsm/loaders/OBJLoader.js';
import { FBXLoader } from 'three/examples/jsm/loaders/FBXLoader.js';
import { USDZLoader } from 'three/examples/jsm/loaders/USDZLoader.js';
import { DRACOLoader } from 'three/examples/jsm/loaders/DRACOLoader.js';
import { KTX2Loader } from 'three/examples/jsm/loaders/KTX2Loader.js';
import { MTLLoader } from 'three/examples/jsm/loaders/MTLLoader.js';

export class ModelLoader {
    constructor() {
        this.loaders = new Map();
        this.cache = new Map();
        
        // Initialize loaders
        this.initializeLoaders();
        
        // Callbacks
        this.onProgress = null;
        this.onComplete = null;
        this.onError = null;
    }
    
    initializeLoaders() {
        // GLTF/GLB loader with Draco support
        const dracoLoader = new DRACOLoader();
        dracoLoader.setDecoderPath('https://www.gstatic.com/draco/versioned/decoders/1.5.7/');
        
        const gltfLoader = new GLTFLoader();
        gltfLoader.setDRACOLoader(dracoLoader);
        
        // KTX2 support for compressed textures
        const ktx2Loader = new KTX2Loader();
        ktx2Loader.setTranscoderPath('https://www.gstatic.com/basis-universal/versioned/2021-04-15-ba1c3e4/');
        gltfLoader.setKTX2Loader(ktx2Loader);
        
        this.loaders.set('gltf', gltfLoader);
        this.loaders.set('glb', gltfLoader);
        
        // OBJ loader
        this.loaders.set('obj', new OBJLoader());
        
        // FBX loader
        this.loaders.set('fbx', new FBXLoader());
        
        // USDZ loader
        this.loaders.set('usdz', new USDZLoader());
        
        // MTL loader for OBJ materials
        this.loaders.set('mtl', new MTLLoader());
    }
    
    async load(url, format) {
        // Check cache
        if (this.cache.has(url)) {
            return this.cache.get(url).clone();
        }
        
        const loader = this.loaders.get(format.toLowerCase());
        if (!loader) {
            throw new Error(`Unsupported format: ${format}`);
        }
        
        return new Promise((resolve, reject) => {
            // Handle OBJ with MTL
            if (format.toLowerCase() === 'obj') {
                this.loadOBJWithMTL(url, resolve, reject);
                return;
            }
            
            loader.load(
                url,
                (result) => {
                    let model;
                    
                    // Extract the actual 3D object based on format
                    if (format === 'gltf' || format === 'glb') {
                        model = result.scene;
                        
                        // Handle animations
                        if (result.animations && result.animations.length > 0) {
                            const mixer = new THREE.AnimationMixer(model);
                            model.userData.mixer = mixer;
                            model.userData.animations = result.animations;
                            
                            // Play first animation by default
                            const action = mixer.clipAction(result.animations[0]);
                            action.play();
                        }
                    } else if (format === 'fbx') {
                        model = result;
                        
                        // FBX animations
                        if (result.animations && result.animations.length > 0) {
                            const mixer = new THREE.AnimationMixer(model);
                            model.userData.mixer = mixer;
                            model.userData.animations = result.animations;
                        }
                    } else {
                        model = result;
                    }
                    
                    // Process model
                    this.processModel(model);
                    
                    // Cache the model
                    this.cache.set(url, model);
                    
                    // Mark as loaded model
                    model.userData.isLoadedModel = true;
                    
                    if (this.onComplete) {
                        this.onComplete(model);
                    }
                    
                    resolve(model);
                },
                (progress) => {
                    const percentComplete = (progress.loaded / progress.total) * 100;
                    if (this.onProgress) {
                        this.onProgress(percentComplete);
                    }
                },
                (error) => {
                    if (this.onError) {
                        this.onError(error);
                    }
                    reject(error);
                }
            );
        });
    }
    
    async loadOBJWithMTL(objUrl, resolve, reject) {
        // Try to load MTL file first
        const mtlUrl = objUrl.replace('.obj', '.mtl');
        const mtlLoader = this.loaders.get('mtl');
        const objLoader = this.loaders.get('obj');
        
        // Try loading MTL
        mtlLoader.load(
            mtlUrl,
            (materials) => {
                materials.preload();
                objLoader.setMaterials(materials);
                
                // Load OBJ with materials
                this.loadOBJFile(objUrl, objLoader, resolve, reject);
            },
            undefined,
            () => {
                // MTL failed, load OBJ without materials
                console.warn('MTL file not found, loading OBJ without materials');
                this.loadOBJFile(objUrl, objLoader, resolve, reject);
            }
        );
    }
    
    loadOBJFile(url, loader, resolve, reject) {
        loader.load(
            url,
            (object) => {
                this.processModel(object);
                object.userData.isLoadedModel = true;
                
                if (this.onComplete) {
                    this.onComplete(object);
                }
                
                resolve(object);
            },
            (progress) => {
                const percentComplete = (progress.loaded / progress.total) * 100;
                if (this.onProgress) {
                    this.onProgress(percentComplete);
                }
            },
            (error) => {
                if (this.onError) {
                    this.onError(error);
                }
                reject(error);
            }
        );
    }
    
    processModel(model) {
        // Apply default material if none exists
        model.traverse((child) => {
            if (child.isMesh) {
                // Enable shadows
                child.castShadow = true;
                child.receiveShadow = true;
                
                // Apply default material if missing
                if (!child.material) {
                    child.material = new THREE.MeshStandardMaterial({
                        color: 0x808080,
                        roughness: 0.7,
                        metalness: 0.3
                    });
                }
                
                // Fix materials that might not work well
                if (child.material && !child.material.map) {
                    const color = child.material.color || new THREE.Color(0x808080);
                    child.material = new THREE.MeshStandardMaterial({
                        color: color,
                        roughness: 0.7,
                        metalness: 0.3
                    });
                }
            }
        });
        
        // Center the model
        const box = new THREE.Box3().setFromObject(model);
        const center = box.getCenter(new THREE.Vector3());
        model.position.sub(center);
        
        // Normalize scale
        const size = box.getSize(new THREE.Vector3());
        const maxDim = Math.max(size.x, size.y, size.z);
        const scale = 10 / maxDim;
        model.scale.multiplyScalar(scale);
    }
    
    async loadFromFile(file) {
        const url = URL.createObjectURL(file);
        const extension = file.name.split('.').pop().toLowerCase();
        
        try {
            const model = await this.load(url, extension);
            URL.revokeObjectURL(url);
            return model;
        } catch (error) {
            URL.revokeObjectURL(url);
            throw error;
        }
    }
    
    clearCache() {
        this.cache.clear();
    }
    
    dispose() {
        // Dispose of cached models
        this.cache.forEach(model => {
            model.traverse(child => {
                if (child.geometry) {
                    child.geometry.dispose();
                }
                if (child.material) {
                    if (Array.isArray(child.material)) {
                        child.material.forEach(mat => mat.dispose());
                    } else {
                        child.material.dispose();
                    }
                }
            });
        });
        
        this.cache.clear();
    }
}