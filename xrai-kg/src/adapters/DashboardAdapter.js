/**
 * XRAI Knowledge Graph - Dashboard Adapter
 * Bridges xrai-kg modules with the existing XRAI Dashboard
 *
 * Provides:
 * - KnowledgeGraph as data store (replaces mcpData)
 * - SearchEngine for fuzzy search
 * - ChatParser for command interface
 * - Sync with Cytoscape/3D Force Graph/ECharts
 */

import { KnowledgeGraph } from '../data/KnowledgeGraph.js';
import { SearchEngine } from '../search/SearchEngine.js';
import { ChatParser } from '../ai/ChatParser.js';
import { EChartsRenderer } from '../viz/EChartsRenderer.js';
import { MermaidExporter } from '../viz/MermaidExporter.js';

/**
 * DashboardAdapter - Integrates xrai-kg with existing dashboard
 */
export class DashboardAdapter {
    constructor(options = {}) {
        // Core modules
        this.kg = new KnowledgeGraph();
        this.search = null;
        this.chat = new ChatParser();
        this.mermaid = new MermaidExporter();

        // Visualization renderers
        this.echartsRenderer = null;
        this.cytoscapeInstance = null;
        this.force3DInstance = null;

        // Options
        this.options = {
            echartsContainer: options.echartsContainer || '#echarts-view',
            cytoscapeContainer: options.cytoscapeContainer || '#cy',
            force3DContainer: options.force3DContainer || '#graph3d',
            theme: options.theme || 'dark',
            onDataChange: options.onDataChange || null,
            onSearch: options.onSearch || null,
            ...options
        };

        // Initialize search when Fuse.js is available
        this._initSearch();

        // Listen for data changes
        this.kg.on(event => this._handleDataChange(event));
    }

    _initSearch() {
        // Check for Fuse.js
        if (typeof Fuse !== 'undefined') {
            SearchEngine.setFuseLibrary(Fuse);
        }
        this.search = new SearchEngine(this.kg);
    }

    // ========== DATA OPERATIONS ==========

    /**
     * Load MCP-format data (compatible with existing dashboard)
     */
    loadMCPData(mcpData) {
        this.kg.clear();
        const result = this.kg.bulkAdd(mcpData);
        return result;
    }

    /**
     * Get data in MCP format (for compatibility)
     */
    getMCPData() {
        return this.kg.toJSON();
    }

    /**
     * Sync with external mcpData variable
     */
    syncToMCPData(mcpDataRef) {
        const data = this.kg.toJSON();
        mcpDataRef.entities = data.entities;
        mcpDataRef.relations = data.relations;
        return mcpDataRef;
    }

    /**
     * Import from external mcpData
     */
    syncFromMCPData(mcpData) {
        return this.loadMCPData(mcpData);
    }

    // ========== SEARCH ==========

    /**
     * Fuzzy search entities
     */
    searchEntities(query, options = {}) {
        const results = this.search.search(query, options);
        if (this.options.onSearch) {
            this.options.onSearch(results);
        }
        return results;
    }

    /**
     * Get suggestions for autocomplete
     */
    getSuggestions(partial, limit = 5) {
        return this.search.suggest(partial, limit);
    }

    /**
     * Filter by entity types
     */
    filterByTypes(types) {
        return this.search.filterByTypes(types);
    }

    /**
     * Get type counts
     */
    getTypeCounts() {
        return this.search.getTypes();
    }

    // ========== CHAT COMMANDS ==========

    /**
     * Execute a chat command
     */
    runCommand(input) {
        return this.chat.execute(input, {
            find: (q, opts) => this.search.search(q, opts),
            addEntity: (data) => this.kg.addEntity(data),
            addRelation: (from, to, type) => this.kg.addRelation({ from, to, relationType: type }),
            filterByTypes: (types) => this.search.filterByTypes(types),
            getNeighbors: (name, depth) => this.kg.getNeighbors(name, depth),
            getStats: () => this.kg.getStats(),
            toJSON: () => this.kg.toJSON(),
            kg: this.kg
        });
    }

    /**
     * Parse command without executing
     */
    parseCommand(input) {
        return this.chat.parse(input);
    }

    // ========== VISUALIZATION ==========

    /**
     * Initialize ECharts view
     */
    initECharts(container, options = {}) {
        if (typeof echarts === 'undefined') {
            console.warn('ECharts not loaded');
            return null;
        }

        EChartsRenderer.setEChartsLibrary(echarts);
        this.echartsRenderer = new EChartsRenderer(container, {
            theme: this.options.theme,
            ...options
        });

        if (this.kg.entityCount > 0) {
            this.echartsRenderer.render(this.kg);
        }

        return this.echartsRenderer;
    }

    /**
     * Update ECharts with current data
     */
    updateECharts() {
        if (this.echartsRenderer) {
            this.echartsRenderer.render(this.kg);
        }
    }

    /**
     * Convert KG to Cytoscape format
     */
    toCytoscapeFormat() {
        const data = this.kg.toJSON();
        const elements = [];

        // Nodes
        data.entities.forEach(e => {
            elements.push({
                data: {
                    id: e.name,
                    label: e.name,
                    type: e.entityType,
                    observations: e.observations || []
                }
            });
        });

        // Edges
        data.relations.forEach(r => {
            elements.push({
                data: {
                    id: `${r.from}-${r.to}`,
                    source: r.from,
                    target: r.to,
                    label: r.relationType
                }
            });
        });

        return elements;
    }

    /**
     * Convert KG to 3D Force Graph format
     */
    to3DForceFormat() {
        const data = this.kg.toJSON();

        const nodes = data.entities.map(e => ({
            id: e.name,
            name: e.name,
            type: e.entityType,
            val: (e.observations?.length || 0) + 1
        }));

        const links = data.relations.map(r => ({
            source: r.from,
            target: r.to,
            type: r.relationType
        }));

        return { nodes, links };
    }

    // ========== EXPORT ==========

    /**
     * Export to Mermaid
     */
    toMermaid(options = {}) {
        return this.mermaid.export(this.kg, options);
    }

    /**
     * Export to JSON
     */
    toJSON() {
        return this.kg.toJSON();
    }

    /**
     * Get statistics
     */
    getStats() {
        return this.kg.getStats();
    }

    // ========== EVENTS ==========

    _handleDataChange(event) {
        // Update visualizations
        if (this.echartsRenderer && ['entityAdded', 'entityRemoved', 'relationAdded', 'bulkAdd', 'cleared'].includes(event.type)) {
            this.updateECharts();
        }

        // Callback
        if (this.options.onDataChange) {
            this.options.onDataChange(event);
        }
    }

    /**
     * Subscribe to data changes
     */
    onDataChange(callback) {
        return this.kg.on(callback);
    }

    // ========== UTILITIES ==========

    /**
     * Get entity by name
     */
    getEntity(name) {
        return this.kg.getEntityByName(name);
    }

    /**
     * Get neighbors
     */
    getNeighbors(name, depth = 1) {
        return this.kg.getNeighbors(name, depth);
    }

    /**
     * Get relations for entity
     */
    getRelations(name) {
        return this.kg.getRelationsFor(name);
    }

    /**
     * Clear all data
     */
    clear() {
        this.kg.clear();
    }

    /**
     * Dispose all renderers
     */
    dispose() {
        if (this.echartsRenderer) {
            this.echartsRenderer.dispose();
        }
    }
}

export default DashboardAdapter;
