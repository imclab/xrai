import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.159.0/build/three.module.js';
import { OrbitControls } from 'https://cdn.jsdelivr.net/npm/three@0.159.0/examples/jsm/controls/OrbitControls.js';

class GalleryApp {
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
        this.projects = [];
        this.selectedProject = null;
        this.visualMode = 'galaxy';
        this.objects = [];
        
        this.init();
        this.loadProjects();
        this.setupEventListeners();
    }

    init() {
        this.renderer.setSize(window.innerWidth, window.innerHeight);
        this.renderer.setPixelRatio(window.devicePixelRatio);
        this.renderer.setClearColor(0x0a0a0a);

        this.camera.position.set(0, 500, 1000);
        this.camera.lookAt(0, 0, 0);

        this.controls = new OrbitControls(this.camera, this.renderer.domElement);
        this.controls.enableDamping = true;
        this.controls.dampingFactor = 0.05;

        const ambientLight = new THREE.AmbientLight(0xffffff, 0.4);
        this.scene.add(ambientLight);

        const directionalLight = new THREE.DirectionalLight(0xffffff, 0.6);
        directionalLight.position.set(100, 100, 50);
        this.scene.add(directionalLight);

        this.scene.fog = new THREE.Fog(0x0a0a0a, 1000, 3000);

        // Add stars
        this.addStars();

        this.animate();
    }

    addStars() {
        const geometry = new THREE.BufferGeometry();
        const vertices = [];
        
        for (let i = 0; i < 5000; i++) {
            vertices.push(
                THREE.MathUtils.randFloatSpread(3000),
                THREE.MathUtils.randFloatSpread(3000),
                THREE.MathUtils.randFloatSpread(3000)
            );
        }
        
        geometry.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3));
        
        const material = new THREE.PointsMaterial({
            color: 0xffffff,
            size: 2,
            transparent: true,
            opacity: 0.6
        });
        
        const stars = new THREE.Points(geometry, material);
        this.scene.add(stars);
    }

    async loadProjects() {
        try {
            const response = await fetch('/demo-data.json');
            this.projects = await response.json();
            this.displayProjectList();
            this.createGalaxyView();
        } catch (error) {
            console.error('Error loading projects:', error);
        }
    }

    displayProjectList() {
        const listContainer = document.getElementById('projectList');
        listContainer.innerHTML = '';
        
        this.projects.forEach((project, index) => {
            const totalAssets = Object.values(project.stats).reduce((sum, val) => sum + val, 0);
            
            const item = document.createElement('div');
            item.className = 'project-item';
            item.innerHTML = `
                <div class="project-name">${project.name}</div>
                <div class="project-stats">
                    Unity ${project.unityVersion} | ${totalAssets} assets | ${project.stats.scripts} scripts
                </div>
            `;
            
            item.addEventListener('click', () => this.selectProject(project, index));
            listContainer.appendChild(item);
        });
    }

    selectProject(project, index) {
        this.selectedProject = project;
        
        document.querySelectorAll('.project-item').forEach((item, i) => {
            if (i === index) {
                item.classList.add('active');
            } else {
                item.classList.remove('active');
            }
        });
        
        this.showProjectInfo(project);
        this.focusOnProject(index);
    }

    showProjectInfo(project) {
        const infoDiv = document.getElementById('info');
        const nameDiv = document.getElementById('projectName');
        const gridDiv = document.getElementById('infoGrid');
        const packageDiv = document.getElementById('packageList');
        
        infoDiv.style.display = 'block';
        nameDiv.textContent = project.name;
        
        gridDiv.innerHTML = '';
        const statTypes = ['scripts', 'scenes', 'vfx', 'materials', 'prefabs', 'shaders'];
        
        statTypes.forEach(type => {
            if (project.stats[type] > 0) {
                const item = document.createElement('div');
                item.className = 'info-item';
                item.innerHTML = `
                    <div class="info-label">${type.charAt(0).toUpperCase() + type.slice(1)}</div>
                    <div class="info-value">${project.stats[type]}</div>
                `;
                gridDiv.appendChild(item);
            }
        });
        
        packageDiv.innerHTML = '<div style="margin-top: 10px; font-size: 14px;">Key Packages:</div>';
        project.topPackages.forEach(pkg => {
            packageDiv.innerHTML += `<div style="margin-top: 5px; color: #888;">• ${pkg}</div>`;
        });
    }

    focusOnProject(index) {
        const object = this.objects[index];
        if (object) {
            const targetPosition = new THREE.Vector3(
                object.position.x,
                object.position.y + 200,
                object.position.z + 500
            );
            
            // Animate camera to focus on project
            const startPosition = this.camera.position.clone();
            const duration = 1000;
            const startTime = Date.now();
            
            const animateCamera = () => {
                const elapsed = Date.now() - startTime;
                const progress = Math.min(elapsed / duration, 1);
                const eased = this.easeInOutCubic(progress);
                
                this.camera.position.lerpVectors(startPosition, targetPosition, eased);
                this.controls.target.copy(object.position);
                this.controls.update();
                
                if (progress < 1) {
                    requestAnimationFrame(animateCamera);
                }
            };
            
            animateCamera();
        }
    }

    createGalaxyView() {
        this.clearScene();
        
        this.projects.forEach((project, index) => {
            const totalAssets = Object.values(project.stats).reduce((sum, val) => sum + val, 0);
            const size = Math.min(50 + totalAssets * 0.5, 200);
            
            // Position projects in a spiral galaxy pattern
            const angle = index * 0.5;
            const radius = 200 + index * 50;
            const height = Math.sin(index * 0.3) * 100;
            
            const group = new THREE.Group();
            
            // Central sphere representing the project
            const geometry = new THREE.SphereGeometry(size, 32, 32);
            const material = new THREE.MeshPhongMaterial({
                color: this.getProjectColor(project),
                emissive: this.getProjectColor(project),
                emissiveIntensity: 0.3,
                transparent: true,
                opacity: 0.8
            });
            const sphere = new THREE.Mesh(geometry, material);
            group.add(sphere);
            
            // Add orbiting elements for different asset types
            const assetTypes = [
                { key: 'scripts', color: 0x4CAF50, radius: size + 30 },
                { key: 'scenes', color: 0x2196F3, radius: size + 40 },
                { key: 'vfx', color: 0x00BCD4, radius: size + 50 },
                { key: 'materials', color: 0x9C27B0, radius: size + 60 }
            ];
            
            assetTypes.forEach((asset, assetIndex) => {
                if (project.stats[asset.key] > 0) {
                    const assetSize = Math.min(5 + project.stats[asset.key] * 0.5, 20);
                    const assetGeometry = new THREE.SphereGeometry(assetSize, 16, 16);
                    const assetMaterial = new THREE.MeshPhongMaterial({
                        color: asset.color,
                        emissive: asset.color,
                        emissiveIntensity: 0.5
                    });
                    const assetMesh = new THREE.Mesh(assetGeometry, assetMaterial);
                    
                    const assetAngle = assetIndex * Math.PI * 0.5;
                    assetMesh.position.set(
                        Math.cos(assetAngle) * asset.radius,
                        0,
                        Math.sin(assetAngle) * asset.radius
                    );
                    
                    group.add(assetMesh);
                }
            });
            
            group.position.set(
                Math.cos(angle) * radius,
                height,
                Math.sin(angle) * radius
            );
            
            group.userData = {
                project: project,
                index: index
            };
            
            this.scene.add(group);
            this.objects.push(group);
        });
    }

    createComparisonView() {
        this.clearScene();
        
        const barWidth = 40;
        const barSpacing = 100;
        const maxHeight = 300;
        
        const statTypes = ['scripts', 'scenes', 'prefabs', 'materials', 'vfx'];
        const colors = {
            scripts: 0x4CAF50,
            scenes: 0x2196F3,
            prefabs: 0xFF9800,
            materials: 0x9C27B0,
            vfx: 0x00BCD4
        };
        
        this.projects.forEach((project, projectIndex) => {
            const projectGroup = new THREE.Group();
            
            statTypes.forEach((stat, statIndex) => {
                const value = project.stats[stat] || 0;
                if (value > 0) {
                    const height = (value / 100) * maxHeight;
                    const geometry = new THREE.BoxGeometry(barWidth, height, barWidth);
                    const material = new THREE.MeshPhongMaterial({
                        color: colors[stat],
                        emissive: colors[stat],
                        emissiveIntensity: 0.2
                    });
                    const bar = new THREE.Mesh(geometry, material);
                    
                    bar.position.set(
                        statIndex * (barWidth + 10) - (statTypes.length * (barWidth + 10) / 2),
                        height / 2,
                        0
                    );
                    
                    projectGroup.add(bar);
                }
            });
            
            projectGroup.position.set(
                0,
                0,
                projectIndex * barSpacing - (this.projects.length * barSpacing / 2)
            );
            
            this.scene.add(projectGroup);
            this.objects.push(projectGroup);
        });
    }

    getProjectColor(project) {
        const scripts = project.stats.scripts || 0;
        const vfx = project.stats.vfx || 0;
        const scenes = project.stats.scenes || 0;
        
        if (vfx > scripts) return 0x00BCD4;
        if (scenes > 5) return 0x2196F3;
        if (scripts > 20) return 0x4CAF50;
        return 0xFF9800;
    }

    clearScene() {
        this.objects.forEach(obj => {
            this.scene.remove(obj);
        });
        this.objects = [];
    }

    setupEventListeners() {
        document.querySelectorAll('.mode-button').forEach(btn => {
            btn.addEventListener('click', (e) => {
                document.querySelectorAll('.mode-button').forEach(b => b.classList.remove('active'));
                e.target.classList.add('active');
                this.visualMode = e.target.dataset.mode;
                this.updateVisualization();
            });
        });

        window.addEventListener('resize', () => this.onWindowResize());
        window.addEventListener('mousemove', (e) => this.onMouseMove(e));
    }

    updateVisualization() {
        switch (this.visualMode) {
            case 'galaxy':
                this.createGalaxyView();
                break;
            case 'comparison':
                this.createComparisonView();
                break;
            case 'timeline':
                this.createTimelineView();
                break;
        }
    }

    createTimelineView() {
        this.clearScene();
        
        // Sort projects by Unity version
        const sortedProjects = [...this.projects].sort((a, b) => {
            const versionA = parseFloat(a.unityVersion) || 0;
            const versionB = parseFloat(b.unityVersion) || 0;
            return versionA - versionB;
        });
        
        const spacing = 300;
        
        sortedProjects.forEach((project, index) => {
            const totalAssets = Object.values(project.stats).reduce((sum, val) => sum + val, 0);
            const size = Math.min(50 + totalAssets * 0.3, 150);
            
            const geometry = new THREE.ConeGeometry(size, size * 1.5, 8);
            const material = new THREE.MeshPhongMaterial({
                color: this.getProjectColor(project),
                emissive: this.getProjectColor(project),
                emissiveIntensity: 0.3
            });
            const cone = new THREE.Mesh(geometry, material);
            
            cone.position.set(
                index * spacing - (sortedProjects.length * spacing / 2),
                0,
                0
            );
            
            cone.userData = {
                project: project,
                index: index
            };
            
            this.scene.add(cone);
            this.objects.push(cone);
        });
    }

    easeInOutCubic(t) {
        return t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
    }

    onMouseMove(event) {
        this.mouse.x = (event.clientX / window.innerWidth) * 2 - 1;
        this.mouse.y = -(event.clientY / window.innerHeight) * 2 + 1;
    }

    onWindowResize() {
        this.camera.aspect = window.innerWidth / window.innerHeight;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(window.innerWidth, window.innerHeight);
    }

    animate() {
        requestAnimationFrame(() => this.animate());
        
        this.controls.update();
        
        // Rotate objects
        this.objects.forEach((object, index) => {
            object.rotation.y += 0.001 + index * 0.0001;
            
            // Float animation
            const time = Date.now() * 0.001;
            object.position.y += Math.sin(time + index) * 0.1;
        });
        
        this.renderer.render(this.scene, this.camera);
    }
}

const app = new GalleryApp();