// Dynamic Search UI with real-time results
export class DynamicSearchUI {
    constructor(visualizer) {
        this.visualizer = visualizer;
        this.searchResults = new Map();
        this.currentSearchHandle = null;
        this.resultsContainer = null;
        this.isUpdating = false;
        this.updateQueue = [];
        this.init();
    }
    
    init() {
        this.createUI();
        this.setupEventListeners();
        this.setupKeyboardShortcuts();
    }
    
    createUI() {
        // Enhanced search UI with live results
        const searchSection = document.querySelector('.search-section');
        if (!searchSection) return;
        
        // Add search status indicator
        const statusDiv = document.createElement('div');
        statusDiv.id = 'searchStatus';
        statusDiv.style.cssText = `
            display: none;
            margin-top: 10px;
            padding: 8px 12px;
            background: rgba(0, 102, 255, 0.1);
            border: 1px solid rgba(0, 102, 255, 0.3);
            border-radius: 6px;
            font-size: 12px;
            color: #0066ff;
        `;
        searchSection.appendChild(statusDiv);
        
        // Add live results counter
        const resultsCounter = document.createElement('div');
        resultsCounter.id = 'resultsCounter';
        resultsCounter.style.cssText = `
            margin-top: 10px;
            font-size: 13px;
            color: #888;
            transition: all 0.3s ease;
        `;
        searchSection.appendChild(resultsCounter);
        
        // Add quick filter buttons
        const filterBar = document.createElement('div');
        filterBar.id = 'quickFilters';
        filterBar.style.cssText = `
            display: flex;
            gap: 5px;
            margin-top: 10px;
            flex-wrap: wrap;
        `;
        
        const filters = [
            { label: '🎨 Art', query: 'art sculpture' },
            { label: '🏗️ Architecture', query: 'building architecture' },
            { label: '🤖 Tech', query: 'technology robot' },
            { label: '🪑 Furniture', query: 'furniture chair table' },
            { label: '🚗 Vehicles', query: 'car vehicle transport' },
            { label: '👤 Characters', query: 'character person human' }
        ];
        
        filters.forEach(filter => {
            const btn = document.createElement('button');
            btn.style.cssText = `
                padding: 4px 10px;
                border: 1px solid rgba(255, 255, 255, 0.2);
                background: rgba(255, 255, 255, 0.1);
                color: #fff;
                border-radius: 15px;
                font-size: 12px;
                cursor: pointer;
                transition: all 0.2s;
            `;
            btn.textContent = filter.label;
            btn.onclick = () => this.quickSearch(filter.query);
            filterBar.appendChild(btn);
        });
        
        searchSection.appendChild(filterBar);
        
        // Create floating results preview
        this.createResultsPreview();
    }
    
    createResultsPreview() {
        const preview = document.createElement('div');
        preview.id = 'resultsPreview';
        preview.style.cssText = `
            position: absolute;
            top: 70px;
            right: 20px;
            width: 300px;
            max-height: 400px;
            background: rgba(0, 0, 0, 0.95);
            backdrop-filter: blur(20px);
            border: 1px solid rgba(255, 255, 255, 0.1);
            border-radius: 12px;
            padding: 15px;
            display: none;
            overflow-y: auto;
            z-index: 1000;
            box-shadow: 0 10px 30px rgba(0, 0, 0, 0.5);
        `;
        
        preview.innerHTML = `
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px;">
                <h3 style="margin: 0; font-size: 14px; color: #fff;">Live Results</h3>
                <button id="closePreview" style="
                    background: transparent;
                    border: none;
                    color: #666;
                    font-size: 20px;
                    cursor: pointer;
                    padding: 0;
                    width: 25px;
                    height: 25px;
                ">×</button>
            </div>
            <div id="previewContent"></div>
        `;
        
        document.body.appendChild(preview);
        
        document.getElementById('closePreview').onclick = () => {
            preview.style.display = 'none';
        };
    }
    
    setupEventListeners() {
        const searchInput = document.getElementById('searchInput');
        if (!searchInput) return;
        
        let searchTimer;
        
        // Real-time search as user types
        searchInput.addEventListener('input', (e) => {
            const query = e.target.value.trim();
            
            clearTimeout(searchTimer);
            
            if (query.length === 0) {
                this.clearSearch();
                return;
            }
            
            // Show typing indicator
            this.updateSearchStatus('Typing...', 'waiting');
            
            // Debounced search
            searchTimer = setTimeout(() => {
                this.performDynamicSearch(query);
            }, 300);
        });
        
        // Immediate search on Enter
        searchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                clearTimeout(searchTimer);
                const query = e.target.value.trim();
                if (query) {
                    this.performDynamicSearch(query, true);
                }
            }
        });
    }
    
    setupKeyboardShortcuts() {
        document.addEventListener('keydown', (e) => {
            // Cmd/Ctrl + K to focus search
            if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
                e.preventDefault();
                document.getElementById('searchInput')?.focus();
            }
            
            // Escape to clear search
            if (e.key === 'Escape') {
                this.clearSearch();
            }
        });
    }
    
    async performDynamicSearch(query, immediate = false) {
        // Cancel previous search
        if (this.currentSearchHandle) {
            this.currentSearchHandle.cancel();
        }
        
        // Get selected sources
        const sources = this.getSelectedSources();
        
        this.updateSearchStatus(`Searching ${sources.join(', ')}...`, 'searching');
        document.getElementById('resultsPreview').style.display = 'block';
        
        // Use streaming search
        const streamingManager = this.visualizer.dataManager;
        this.currentSearchHandle = immediate 
            ? await streamingManager.searchStreaming(query, sources)
            : await streamingManager.searchDebounced(query, sources);
        
        // Clear previous results
        this.searchResults.clear();
        this.updateQueue = [];
        
        // Setup progress handler
        this.currentSearchHandle.onProgress(({ newResults, totalResults, count }) => {
            // Queue updates to avoid overwhelming the UI
            this.updateQueue.push({ newResults, totalResults, count });
            this.processUpdateQueue();
        });
        
        // Setup completion handler
        this.currentSearchHandle.onComplete(({ results, count }) => {
            this.updateSearchStatus(`Found ${count} results`, 'complete');
            this.finalizeResults(results);
        });
    }
    
    processUpdateQueue() {
        if (this.isUpdating || this.updateQueue.length === 0) return;
        
        this.isUpdating = true;
        
        requestAnimationFrame(() => {
            const updates = this.updateQueue.splice(0, 5); // Process up to 5 updates
            
            updates.forEach(({ newResults, totalResults, count }) => {
                // Update counter
                document.getElementById('resultsCounter').textContent = 
                    `${count} results found`;
                
                // Update preview
                this.updateResultsPreview(newResults, count);
                
                // Update visualization progressively
                if (count % 100 === 0 || count < 100) {
                    this.updateVisualizationProgressive(totalResults);
                }
            });
            
            this.isUpdating = false;
            
            // Continue processing if more updates
            if (this.updateQueue.length > 0) {
                this.processUpdateQueue();
            }
        });
    }
    
    updateResultsPreview(newResults, totalCount) {
        const content = document.getElementById('previewContent');
        if (!content) return;
        
        // Add new results to preview
        newResults.slice(0, 10).forEach(result => {
            if (this.searchResults.has(result.id)) return;
            
            this.searchResults.set(result.id, result);
            
            const item = document.createElement('div');
            item.style.cssText = `
                padding: 8px;
                margin: 5px 0;
                background: rgba(255, 255, 255, 0.05);
                border-radius: 6px;
                cursor: pointer;
                transition: all 0.2s;
                animation: fadeIn 0.3s ease;
            `;
            
            const sourceColor = this.visualizer.getSourceColor(result.source);
            
            item.innerHTML = `
                <div style="display: flex; align-items: center; gap: 8px;">
                    <div style="
                        width: 8px;
                        height: 8px;
                        background: ${sourceColor};
                        border-radius: 50%;
                    "></div>
                    <div style="flex: 1;">
                        <div style="font-size: 13px; font-weight: 500;">${result.name}</div>
                        <div style="font-size: 11px; color: #666; margin-top: 2px;">
                            ${result.source} • ${result.type || 'item'}
                        </div>
                    </div>
                </div>
            `;
            
            item.onclick = () => this.selectResult(result);
            content.appendChild(item);
        });
        
        // Show total count
        if (totalCount > 10) {
            const more = document.getElementById('moreResults') || (() => {
                const div = document.createElement('div');
                div.id = 'moreResults';
                div.style.cssText = `
                    text-align: center;
                    margin-top: 10px;
                    color: #666;
                    font-size: 12px;
                `;
                content.appendChild(div);
                return div;
            })();
            
            more.textContent = `...and ${totalCount - 10} more results`;
        }
        
        // Add fade in animation
        if (!document.getElementById('fadeInStyle')) {
            const style = document.createElement('style');
            style.id = 'fadeInStyle';
            style.textContent = `
                @keyframes fadeIn {
                    from { opacity: 0; transform: translateY(-5px); }
                    to { opacity: 1; transform: translateY(0); }
                }
            `;
            document.head.appendChild(style);
        }
    }
    
    updateVisualizationProgressive(results) {
        const graphData = this.visualizer.dataManager.convertToGraphData(results);
        
        // Use requestIdleCallback for smooth updates
        if (typeof requestIdleCallback !== 'undefined') {
            requestIdleCallback(() => {
                this.visualizer.updateVisualization(graphData);
            });
        } else {
            setTimeout(() => {
                this.visualizer.updateVisualization(graphData);
            }, 100);
        }
    }
    
    selectResult(result) {
        // Find node in scene
        this.visualizer.scene.traverse((child) => {
            if (child.userData?.nodeData?.id === result.id) {
                // Simulate click on the node
                const event = { clientX: window.innerWidth / 2, clientY: window.innerHeight / 2 };
                this.visualizer.mouse.x = 0;
                this.visualizer.mouse.y = 0;
                
                // Zoom to node
                this.visualizer.zoomToNode(child, child.position, result);
                this.visualizer.showNodeDetails(result);
                return;
            }
        });
    }
    
    quickSearch(query) {
        document.getElementById('searchInput').value = query;
        this.performDynamicSearch(query, true);
    }
    
    clearSearch() {
        // Cancel active search
        if (this.currentSearchHandle) {
            this.currentSearchHandle.cancel();
        }
        
        // Clear UI
        document.getElementById('searchInput').value = '';
        document.getElementById('searchStatus').style.display = 'none';
        document.getElementById('resultsCounter').textContent = '';
        document.getElementById('resultsPreview').style.display = 'none';
        document.getElementById('previewContent').innerHTML = '';
        
        // Clear results
        this.searchResults.clear();
        this.updateQueue = [];
    }
    
    updateSearchStatus(message, state) {
        const status = document.getElementById('searchStatus');
        if (!status) return;
        
        status.style.display = 'block';
        status.textContent = message;
        
        // Update styling based on state
        switch (state) {
            case 'waiting':
                status.style.borderColor = 'rgba(255, 255, 255, 0.2)';
                status.style.color = '#888';
                break;
            case 'searching':
                status.style.borderColor = 'rgba(0, 102, 255, 0.5)';
                status.style.color = '#0066ff';
                status.innerHTML = `<span style="animation: pulse 1s infinite;">${message}</span>`;
                break;
            case 'complete':
                status.style.borderColor = 'rgba(0, 255, 0, 0.3)';
                status.style.color = '#00ff00';
                setTimeout(() => {
                    status.style.display = 'none';
                }, 3000);
                break;
        }
        
        // Add pulse animation if not present
        if (!document.getElementById('pulseStyle')) {
            const style = document.createElement('style');
            style.id = 'pulseStyle';
            style.textContent = `
                @keyframes pulse {
                    0% { opacity: 1; }
                    50% { opacity: 0.5; }
                    100% { opacity: 1; }
                }
            `;
            document.head.appendChild(style);
        }
    }
    
    getSelectedSources() {
        const checkboxes = document.querySelectorAll('.source:checked');
        return Array.from(checkboxes).map(cb => cb.value);
    }
    
    finalizeResults(results) {
        // Update stats
        this.visualizer.uiController.updateStats({
            nodes: results.length,
            links: 0,
            searchTime: this.currentSearchHandle ? 
                `${(Date.now() - this.currentSearchHandle.startTime) / 1000}s` : 'N/A'
        });
        
        console.log(`Search complete: ${results.length} total results`);
    }
}