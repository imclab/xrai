import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.159.0/build/three.module.js';
import { OrbitControls } from 'https://cdn.jsdelivr.net/npm/three@0.159.0/examples/jsm/controls/OrbitControls.js';
import { VisualizationEnhancements } from './visualization-enhancements.js';

class HolovisApp {
    constructor() {
        this.scene = new THREE.Scene();
        this.camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 10000);
        this.renderer = new THREE.WebGLRenderer({ 
            canvas: document.getElementById('canvas'),
            antialias: true,
            alpha: true
        });
        
        this.controls = null;
        this.raycaster = new THREE.Raycaster();
        this.mouse = new THREE.Vector2();
        this.projectData = null;
        this.visualMode = 'treegraph';
        this.objects = [];
        
        this.init();
        this.setupEventListeners();
    }

    init() {
        this.renderer.setSize(window.innerWidth, window.innerHeight);
        this.renderer.setPixelRatio(window.devicePixelRatio);
        this.renderer.setClearColor(0x0a0a0a);

        this.camera.position.set(0, 200, 500);
        this.camera.lookAt(0, 0, 0);

        this.controls = new OrbitControls(this.camera, this.renderer.domElement);
        this.controls.enableDamping = true;
        this.controls.dampingFactor = 0.05;

        const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
        this.scene.add(ambientLight);

        const directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
        directionalLight.position.set(100, 100, 50);
        this.scene.add(directionalLight);

        this.scene.fog = new THREE.Fog(0x0a0a0a, 500, 2000);

        this.animate();
    }

    setupEventListeners() {
        document.getElementById('analyzeBtn').addEventListener('click', () => this.analyzeProject());
        
        document.querySelectorAll('.mode-button').forEach(btn => {
            btn.addEventListener('click', (e) => {
                document.querySelectorAll('.mode-button').forEach(b => b.classList.remove('active'));
                e.target.classList.add('active');
                this.visualMode = e.target.dataset.mode;
                if (this.projectData) {
                    this.visualizeData();
                }
            });
        });

        window.addEventListener('resize', () => this.onWindowResize());
        window.addEventListener('mousemove', (e) => this.onMouseMove(e));
        window.addEventListener('click', (e) => this.onMouseClick(e));
    }

    async analyzeProject() {
        const projectPath = document.getElementById('projectPath').value;
        if (!projectPath) {
            alert('Please enter a project path');
            return;
        }

        try {
            const response = await fetch('/analyze', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ projectPath })
            });

            if (!response.ok) {
                throw new Error('Analysis failed');
            }

            this.projectData = await response.json();
            this.updateStats();
            this.visualizeData();
        } catch (error) {
            alert('Error analyzing project: ' + error.message);
        }
    }

    updateStats() {
        document.getElementById('stats').style.display = 'block';
        document.getElementById('scriptCount').textContent = this.projectData.scripts?.length || 0;
        document.getElementById('sceneCount').textContent = this.projectData.scenes?.length || 0;
        document.getElementById('prefabCount').textContent = this.projectData.prefabs?.length || 0;
        document.getElementById('materialCount').textContent = this.projectData.materials?.length || 0;
        document.getElementById('vfxCount').textContent = this.projectData.vfx?.length || 0;
        document.getElementById('shaderCount').textContent = this.projectData.shaders?.length || 0;
        document.getElementById('packageCount').textContent = this.projectData.packages?.length || 0;
    }

    visualizeData() {
        this.clearScene();

        switch (this.visualMode) {
            case 'treegraph':
                this.createTreeGraph();
                break;
            case 'nodegraph':
                this.createNodeGraph();
                break;
            case 'flowchart':
                this.createFlowChart();
                break;
        }
    }

    clearScene() {
        this.objects.forEach(obj => {
            this.scene.remove(obj);
        });
        this.objects = [];
    }

    createTreeGraph() {
        const centerCube = this.createCube(100, 0x00ff88, { x: 0, y: 0, z: 0 });
        centerCube.userData = { 
            type: 'project', 
            name: this.projectData.name 
        };
        this.objects.push(centerCube);

        const categories = [
            { key: 'scripts', color: 0x4CAF50, offset: { x: -200, y: 0, z: 0 } },
            { key: 'scenes', color: 0x2196F3, offset: { x: 200, y: 0, z: 0 } },
            { key: 'prefabs', color: 0xFF9800, offset: { x: 0, y: 0, z: -200 } },
            { key: 'materials', color: 0x9C27B0, offset: { x: 0, y: 0, z: 200 } },
            { key: 'shaders', color: 0xF44336, offset: { x: -150, y: 150, z: 0 } },
            { key: 'packages', color: 0x795548, offset: { x: 150, y: 150, z: 0 } }
        ];

        categories.forEach(category => {
            if (this.projectData[category.key] && this.projectData[category.key].length > 0) {
                const categoryGroup = new THREE.Group();
                
                const categoryCube = this.createCube(60, category.color, { x: 0, y: 0, z: 0 });
                categoryCube.userData = {
                    type: 'category',
                    name: category.key,
                    count: this.projectData[category.key].length
                };
                categoryGroup.add(categoryCube);

                this.projectData[category.key].slice(0, 10).forEach((item, index) => {
                    const angle = (index / 10) * Math.PI * 2;
                    const radius = 80;
                    const itemCube = this.createCube(20, category.color, {
                        x: Math.cos(angle) * radius,
                        y: 20,
                        z: Math.sin(angle) * radius
                    });
                    itemCube.userData = {
                        type: category.key,
                        name: item.name,
                        path: item.path
                    };
                    categoryGroup.add(itemCube);
                });

                categoryGroup.position.set(category.offset.x, category.offset.y, category.offset.z);
                this.scene.add(categoryGroup);
                this.objects.push(categoryGroup);

                const line = this.createConnection(
                    new THREE.Vector3(0, 0, 0),
                    new THREE.Vector3(category.offset.x, category.offset.y, category.offset.z)
                );
                this.objects.push(line);
            }
        });
    }

    createNodeGraph() {
        const nodes = new Map();
        
        const centerNode = this.createSphere(50, 0x00ff88, { x: 0, y: 0, z: 0 });
        centerNode.userData = { type: 'project', name: this.projectData.name };
        this.objects.push(centerNode);
        nodes.set('root', centerNode);

        this.projectData.packages.forEach((pkg, index) => {
            const angle = (index / this.projectData.packages.length) * Math.PI * 2;
            const radius = 300;
            const color = pkg.type === 'unity' ? 0x2196F3 : 0xFF9800;
            
            const packageNode = this.createSphere(30, color, {
                x: Math.cos(angle) * radius,
                y: 0,
                z: Math.sin(angle) * radius
            });
            packageNode.userData = {
                type: 'package',
                name: pkg.name,
                version: pkg.version
            };
            this.objects.push(packageNode);
            
            const connection = this.createConnection(
                centerNode.position,
                packageNode.position
            );
            this.objects.push(connection);
        });
    }

    createFlowChart() {
        const sceneSpacing = 400;
        const levelSpacing = 200;

        this.projectData.scenes.forEach((scene, sceneIndex) => {
            const sceneNode = this.createBox(120, 80, 40, 0x2196F3, {
                x: sceneIndex * sceneSpacing - (this.projectData.scenes.length * sceneSpacing / 2),
                y: 0,
                z: 0
            });
            sceneNode.userData = {
                type: 'scene',
                name: scene.name,
                path: scene.path
            };
            this.objects.push(sceneNode);

            const relatedScripts = this.projectData.scripts.filter(script => 
                script.path.includes(scene.name.replace('.unity', ''))
            );

            relatedScripts.forEach((script, scriptIndex) => {
                const scriptNode = this.createBox(80, 50, 30, 0x4CAF50, {
                    x: sceneNode.position.x + (scriptIndex - relatedScripts.length / 2) * 100,
                    y: -levelSpacing,
                    z: 0
                });
                scriptNode.userData = {
                    type: 'script',
                    name: script.name,
                    path: script.path
                };
                this.objects.push(scriptNode);

                const connection = this.createConnection(
                    sceneNode.position,
                    scriptNode.position
                );
                this.objects.push(connection);
            });
        });
    }

    createCube(size, color, position) {
        const geometry = new THREE.BoxGeometry(size, size, size);
        const material = new THREE.MeshPhongMaterial({ 
            color: color,
            emissive: color,
            emissiveIntensity: 0.2
        });
        const cube = new THREE.Mesh(geometry, material);
        cube.position.set(position.x, position.y, position.z);
        this.scene.add(cube);
        return cube;
    }

    createSphere(radius, color, position) {
        const geometry = new THREE.SphereGeometry(radius, 32, 32);
        const material = new THREE.MeshPhongMaterial({ 
            color: color,
            emissive: color,
            emissiveIntensity: 0.2
        });
        const sphere = new THREE.Mesh(geometry, material);
        sphere.position.set(position.x, position.y, position.z);
        this.scene.add(sphere);
        return sphere;
    }

    createBox(width, height, depth, color, position) {
        const geometry = new THREE.BoxGeometry(width, height, depth);
        const material = new THREE.MeshPhongMaterial({ 
            color: color,
            emissive: color,
            emissiveIntensity: 0.2
        });
        const box = new THREE.Mesh(geometry, material);
        box.position.set(position.x, position.y, position.z);
        this.scene.add(box);
        return box;
    }

    createConnection(start, end) {
        const points = [];
        points.push(start);
        points.push(end);
        
        const geometry = new THREE.BufferGeometry().setFromPoints(points);
        const material = new THREE.LineBasicMaterial({ 
            color: 0x00ff88,
            opacity: 0.5,
            transparent: true
        });
        const line = new THREE.Line(geometry, material);
        this.scene.add(line);
        return line;
    }

    onMouseMove(event) {
        this.mouse.x = (event.clientX / window.innerWidth) * 2 - 1;
        this.mouse.y = -(event.clientY / window.innerHeight) * 2 + 1;
    }

    onMouseClick(event) {
        this.raycaster.setFromCamera(this.mouse, this.camera);
        const intersects = this.raycaster.intersectObjects(this.scene.children, true);
        
        if (intersects.length > 0) {
            const object = intersects[0].object;
            if (object.userData && object.userData.name) {
                this.showInfo(object.userData);
            }
        }
    }

    showInfo(data) {
        const infoDiv = document.getElementById('info');
        const infoTitle = document.getElementById('infoTitle');
        const infoContent = document.getElementById('infoContent');
        
        infoDiv.style.display = 'block';
        infoTitle.textContent = data.name;
        
        let content = `Type: ${data.type}`;
        if (data.path) content += `\nPath: ${data.path}`;
        if (data.version) content += `\nVersion: ${data.version}`;
        if (data.count) content += `\nCount: ${data.count}`;
        
        infoContent.textContent = content;
    }

    onWindowResize() {
        this.camera.aspect = window.innerWidth / window.innerHeight;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(window.innerWidth, window.innerHeight);
    }

    animate() {
        requestAnimationFrame(() => this.animate());
        
        this.controls.update();
        
        this.scene.children.forEach(child => {
            if (child.type === 'Mesh' || child.type === 'Group') {
                child.rotation.y += 0.001;
            }
        });
        
        this.renderer.render(this.scene, this.camera);
    }
}

const app = new HolovisApp();