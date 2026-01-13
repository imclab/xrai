export class UIController {
    constructor(app) {
        this.app = app;
        this.lastFrameTime = performance.now();
        this.frameCount = 0;
        this.fps = 0;
        
        this.init();
    }
    
    init() {
        this.setupSearchControls();
        this.setupLayoutControls();
        this.setupFileHandling();
        this.setupViewerControls();
        this.setupStressTests();
        this.setupStats();
    }
    
    setupSearchControls() {
        const searchInput = document.getElementById('searchInput');
        const searchBtn = document.getElementById('searchBtn');
        
        const performSearch = () => {
            const query = searchInput.value.trim();
            if (query) {
                const sources = this.getSelectedSources();
                this.app.performSearch(query, sources);
            }
        };
        
        searchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                performSearch();
            }
        });
        
        searchBtn.addEventListener('click', performSearch);
    }
    
    setupLayoutControls() {
        const layoutRadios = document.querySelectorAll('input[name="layout"]');
        
        layoutRadios.forEach(radio => {
            radio.addEventListener('change', (e) => {
                if (e.target.checked && this.app.lastGraphData) {
                    this.app.updateVisualization(this.app.lastGraphData);
                }
            });
        });
    }
    
    setupFileHandling() {
        const fileInput = document.getElementById('fileInput');
        const dropZone = document.getElementById('dropZone');
        
        // File input
        dropZone.addEventListener('click', () => {
            fileInput.click();
        });
        
        fileInput.addEventListener('change', (e) => {
            const files = e.target.files;
            if (files.length > 0) {
                this.handleFiles(files);
            }
        });
        
        // Drag and drop
        dropZone.addEventListener('dragover', (e) => {
            e.preventDefault();
            dropZone.classList.add('dragover');
        });
        
        dropZone.addEventListener('dragleave', () => {
            dropZone.classList.remove('dragover');
        });
        
        dropZone.addEventListener('drop', (e) => {
            e.preventDefault();
            dropZone.classList.remove('dragover');
            
            const files = e.dataTransfer.files;
            this.handleFiles(files);
        });
        
        // Global drag and drop
        document.addEventListener('dragover', (e) => {
            e.preventDefault();
        });
        
        document.addEventListener('drop', (e) => {
            if (e.target !== dropZone && !dropZone.contains(e.target)) {
                e.preventDefault();
                const files = e.dataTransfer.files;
                this.handleFiles(files);
            }
        });
    }
    
    setupViewerControls() {
        const viewerBtns = document.querySelectorAll('.viewer-btn');
        
        viewerBtns.forEach(btn => {
            btn.addEventListener('click', () => {
                // Update active state
                viewerBtns.forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                
                // Switch mode
                const mode = btn.dataset.viewer;
                this.app.switchMode(mode);
            });
        });
        
        // Close button for Icosa viewer
        window.closeIcosaViewer = () => {
            document.getElementById('icosaViewer').classList.remove('active');
            document.querySelector('[data-viewer="graph"]').click();
        };
    }
    
    setupStressTests() {
        // Add stress test buttons to the control panel if they don't exist
        const controlPanel = document.querySelector('.control-panel');
        
        if (!document.getElementById('stressTest100k')) {
            const stressSection = document.createElement('div');
            stressSection.innerHTML = `
                <div class="stress-test" style="margin-top: 20px; border-top: 1px solid #333; padding-top: 15px;">
                    <h3>Performance Testing</h3>
                    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 8px; margin-top: 10px;">
                        <button class="source-btn" id="stressTest10k">10K Nodes</button>
                        <button class="source-btn" id="stressTest100k">100K Nodes</button>
                        <button class="source-btn" id="stressTest1M">1M Nodes</button>
                        <button class="source-btn" id="stressTestReal">Real Data</button>
                    </div>
                    <p style="font-size: 12px; color: #888; margin-top: 8px;">
                        Test visualization performance with massive datasets
                    </p>
                </div>
            `;
            
            controlPanel.appendChild(stressSection);
            
            // Add event listeners
            document.getElementById('stressTest10k').addEventListener('click', () => {
                this.app.runStressTest(10000);
            });
            
            document.getElementById('stressTest100k').addEventListener('click', () => {
                this.app.runStressTest(100000);
            });
            
            document.getElementById('stressTest1M').addEventListener('click', () => {
                this.app.runStressTest(1000000);
            });
            
            document.getElementById('stressTestReal').addEventListener('click', async () => {
                // Search all sources to get maximum real data
                await this.app.performSearch('', ['icosa', 'objaverse', 'github', 'local', 'web']);
            });
        }
    }
    
    setupStats() {
        // Update FPS every second
        setInterval(() => {
            this.fps = this.frameCount;
            this.frameCount = 0;
            document.getElementById('fps').textContent = `FPS: ${this.fps}`;
        }, 1000);
        
        const statsContainer = document.querySelector('.stats');
        if (statsContainer && !document.getElementById('diagnosticsBtn')) {
            const diagnosticsBtn = document.createElement('button');
            diagnosticsBtn.id = 'diagnosticsBtn';
            diagnosticsBtn.className = 'source-btn';
            diagnosticsBtn.textContent = 'Run Source Diagnostics';
            diagnosticsBtn.style.marginTop = '10px';
            diagnosticsBtn.addEventListener('click', () => {
                if (window.cosmosApp) {
                    window.cosmosApp.runSourceDiagnostics();
                }
            });
            
            const diagnosticsStatus = document.createElement('div');
            diagnosticsStatus.id = 'diagnosticsStatus';
            diagnosticsStatus.style.marginTop = '8px';
            diagnosticsStatus.style.fontSize = '12px';
            diagnosticsStatus.style.color = '#aaa';
            
            const diagnosticsResults = document.createElement('div');
            diagnosticsResults.id = 'diagnosticsResults';
            diagnosticsResults.style.marginTop = '6px';
            diagnosticsResults.style.fontSize = '12px';
            diagnosticsResults.style.display = 'grid';
            diagnosticsResults.style.gap = '4px';
            
            statsContainer.appendChild(diagnosticsBtn);
            statsContainer.appendChild(diagnosticsStatus);
            statsContainer.appendChild(diagnosticsResults);
        }
    }
    
    getSelectedSources() {
        const checkboxes = document.querySelectorAll('.source:checked');
        return Array.from(checkboxes).map(cb => cb.value);
    }
    
    getSelectedLayout() {
        const selected = document.querySelector('input[name="layout"]:checked');
        return selected ? selected.value : 'force';
    }
    
    handleFiles(files) {
        for (const file of files) {
            this.app.loadFile(file);
        }
    }
    
    showLoading() {
        const loading = document.getElementById('loadingIndicator');
        loading.classList.add('active');
    }
    
    hideLoading() {
        const loading = document.getElementById('loadingIndicator');
        loading.classList.remove('active');
    }
    
    showDiagnosticsStatus(message) {
        const statusEl = document.getElementById('diagnosticsStatus');
        if (statusEl) {
            statusEl.textContent = message;
        }
    }
    
    showDiagnostics(results) {
        const resultsEl = document.getElementById('diagnosticsResults');
        if (!resultsEl) return;
        
        resultsEl.innerHTML = results.map(result => {
            const summary = `${result.source.toUpperCase()} (${result.query || 'default'}) → ${result.count} results in ${result.duration}ms`;
            const color = result.status === 'success' ? '#6BEFA3' : '#FF6B6B';
            const icon = result.status === 'success' ? '✅' : '⚠️';
            const errorText = result.error ? ` — ${result.error}` : '';
            return `<div style="color:${color};">${icon} ${summary}${errorText}</div>`;
        }).join('');
    }
    
    updateLoadingProgress(percent) {
        // Could add progress bar here
        console.log(`Loading: ${percent.toFixed(1)}%`);
    }
    
    updateStats(stats) {
        document.getElementById('nodeCount').textContent = `Nodes: ${(stats.nodes || 0).toLocaleString()}`;
        document.getElementById('linkCount').textContent = `Links: ${(stats.links || 0).toLocaleString()}`;
        
        // Add performance stats if available
        if (stats.generationTime) {
            const perfDiv = document.getElementById('performanceStats') || (() => {
                const div = document.createElement('div');
                div.id = 'performanceStats';
                div.style.cssText = 'font-size: 11px; color: #666; margin-top: 5px;';
                document.querySelector('.stats').appendChild(div);
                return div;
            })();
            
            perfDiv.innerHTML = `
                Generation: ${stats.generationTime}<br>
                Total: ${stats.totalTime}
            `;
        }
    }
    
    updateFPS() {
        this.frameCount++;
    }
    
    async showIcosaViewer() {
        const viewer = document.getElementById('icosaViewer');
        const frame = document.getElementById('icosaFrame');
        
        try {
            // Create an enhanced gallery viewer with both Icosa Gallery access and model viewer
            const galleryViewerHTML = `
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset="UTF-8">
                    <title>Icosa Gallery Viewer</title>
                    <script type="module" src="https://unpkg.com/@google/model-viewer/dist/model-viewer.min.js"></script>
                    <style>
                        body { 
                            margin: 0; 
                            background: #1a1a1a; 
                            font-family: Arial, sans-serif;
                            color: #fff;
                        }
                        .container {
                            display: flex;
                            height: 100vh;
                        }
                        .gallery-panel {
                            width: 300px;
                            background: #2a2a2a;
                            padding: 20px;
                            overflow-y: auto;
                            border-right: 1px solid #444;
                        }
                        .viewer-panel {
                            flex: 1;
                            position: relative;
                        }
                        model-viewer {
                            width: 100%;
                            height: 100%;
                            background-color: #1a1a1a;
                        }
                        .gallery-item {
                            margin: 10px 0;
                            padding: 10px;
                            background: #3a3a3a;
                            border-radius: 5px;
                            cursor: pointer;
                            transition: background 0.3s;
                        }
                        .gallery-item:hover {
                            background: #4a4a4a;
                        }
                        .gallery-title {
                            font-weight: bold;
                            margin-bottom: 5px;
                        }
                        .gallery-author {
                            font-size: 0.9em;
                            color: #aaa;
                        }
                        .info-panel {
                            position: absolute;
                            top: 20px;
                            right: 20px;
                            background: rgba(0,0,0,0.8);
                            padding: 15px;
                            border-radius: 5px;
                            max-width: 300px;
                        }
                        .loading {
                            text-align: center;
                            padding: 20px;
                            color: #888;
                        }
                        .external-link {
                            display: inline-block;
                            margin-top: 10px;
                            color: #4a9eff;
                            text-decoration: none;
                        }
                        .external-link:hover {
                            text-decoration: underline;
                        }
                    </style>
                </head>
                <body>
                    <div class="container">
                        <div class="gallery-panel">
                            <h2>3D Models</h2>
                            <div class="loading">Loading gallery...</div>
                            <div id="galleryItems"></div>
                            <hr style="margin: 20px 0; border-color: #444;">
                            <h3>External Galleries</h3>
                            <a href="https://sketchfab.com/3d-models?features=downloadable" target="_blank" class="external-link">
                                Browse Sketchfab →
                            </a><br>
                            <a href="https://github.com/KhronosGroup/glTF-Sample-Models/tree/master/2.0" target="_blank" class="external-link">
                                glTF Sample Models →
                            </a><br>
                            <a href="https://poly.pizza/" target="_blank" class="external-link">
                                Poly Pizza Archive →
                            </a>
                        </div>
                        <div class="viewer-panel">
                            <model-viewer
                                id="modelViewer"
                                src="https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/DamagedHelmet/glTF-Binary/DamagedHelmet.glb"
                                alt="3D Model"
                                auto-rotate
                                camera-controls
                                shadow-intensity="1"
                                exposure="0.5"
                                tone-mapping="neutral"
                                environment-image="https://modelviewer.dev/shared-assets/environments/spruit_sunrise_1k_HDR.hdr"
                                skybox-image="https://modelviewer.dev/shared-assets/environments/spruit_sunrise_1k_HDR.hdr"
                            ></model-viewer>
                            <div class="info-panel" id="infoPanel">
                                <div id="modelInfo">Select a model from the gallery</div>
                            </div>
                        </div>
                    </div>
                    <script>
                        // Sample models to display in gallery
                        const sampleModels = [
                            {
                                name: "Damaged Helmet",
                                author: "Khronos Group",
                                url: "https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/DamagedHelmet/glTF-Binary/DamagedHelmet.glb",
                                description: "Battle-damaged sci-fi helmet"
                            },
                            {
                                name: "Boom Box",
                                author: "Khronos Group",
                                url: "https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/BoomBox/glTF-Binary/BoomBox.glb",
                                description: "Retro boom box radio"
                            },
                            {
                                name: "Lantern",
                                author: "Khronos Group",
                                url: "https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/Lantern/glTF-Binary/Lantern.glb",
                                description: "Antique lantern"
                            },
                            {
                                name: "Water Bottle",
                                author: "Khronos Group",
                                url: "https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/WaterBottle/glTF-Binary/WaterBottle.glb",
                                description: "Realistic water bottle"
                            },
                            {
                                name: "Flight Helmet",
                                author: "Khronos Group",
                                url: "https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/FlightHelmet/glTF/FlightHelmet.gltf",
                                description: "Military flight helmet"
                            }
                        ];
                        
                        const galleryContainer = document.getElementById('galleryItems');
                        const modelViewer = document.getElementById('modelViewer');
                        const infoPanel = document.getElementById('modelInfo');
                        
                        // Clear loading message
                        document.querySelector('.loading').style.display = 'none';
                        
                        // Render gallery items
                        sampleModels.forEach(model => {
                            const item = document.createElement('div');
                            item.className = 'gallery-item';
                            item.innerHTML = \`
                                <div class="gallery-title">\${model.name}</div>
                                <div class="gallery-author">by \${model.author}</div>
                                <div style="font-size: 0.85em; color: #888; margin-top: 5px;">\${model.description}</div>
                            \`;
                            item.onclick = () => loadModel(model);
                            galleryContainer.appendChild(item);
                        });
                        
                        function loadModel(model) {
                            modelViewer.src = model.url;
                            infoPanel.innerHTML = \`
                                <h3>\${model.name}</h3>
                                <p>Author: \${model.author}</p>
                                <p>\${model.description}</p>
                                <p style="font-size: 0.85em; color: #888;">Click and drag to rotate</p>
                            \`;
                        }
                        
                        // Listen for messages from parent
                        window.addEventListener('message', (event) => {
                            if (event.data.modelUrl) {
                                modelViewer.src = event.data.modelUrl;
                                infoPanel.innerHTML = \`
                                    <h3>External Model</h3>
                                    <p>Loading model from parent application</p>
                                \`;
                            }
                        });
                        
                        // Try to load models from the parent's data if available
                        window.parent.postMessage({ type: 'galleryReady' }, '*');
                    </script>
                </body>
                </html>
            `;
            
            // Create blob URL for the HTML
            const blob = new Blob([galleryViewerHTML], { type: 'text/html' });
            const url = URL.createObjectURL(blob);
            frame.src = url;
            viewer.classList.add('active');
            
            console.log('Loading Icosa Gallery viewer...');
            
            // Clean up blob URL after iframe loads
            frame.onload = () => {
                setTimeout(() => URL.revokeObjectURL(url), 100);
                
                // If we have loaded models in the main app, send them to the gallery
                if (this.app && this.app.lastGraphData && this.app.lastGraphData.nodes) {
                    const models = this.app.lastGraphData.nodes
                        .filter(node => node.modelUrl)
                        .map(node => ({
                            name: node.name || node.id,
                            url: node.modelUrl,
                            author: node.author || 'Unknown',
                            description: node.description || ''
                        }));
                    
                    if (models.length > 0) {
                        setTimeout(() => {
                            frame.contentWindow.postMessage({ 
                                type: 'loadModels', 
                                models: models 
                            }, '*');
                        }, 1000);
                    }
                }
            };
        } catch (error) {
            console.error('Failed to load Icosa viewer:', error);
            // Fallback to external site
            frame.src = 'https://poly.pizza/';
            viewer.classList.add('active');
        }
    }
}
