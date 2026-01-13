/**
 * XRAI Knowledge Graph - Main Entry Point
 *
 * Modular, fast, platform-agnostic knowledge graph library
 *
 * LAYERS:
 * - Data Layer: KnowledgeGraph, Entity, Relation
 * - Search Layer: SearchEngine (Fuse.js compatible)
 * - AI Layer: ChatParser (for natural language commands)
 * - Viz Layer: EChartsRenderer, MermaidExporter
 * - Adapter Layer: VSCode, Chrome, Browser
 *
 * @example
 * // Basic usage - separate layers
 * import { KnowledgeGraph, SearchEngine } from 'xrai-kg';
 * const kg = new KnowledgeGraph();
 * kg.bulkAdd(mcpData);
 * const search = new SearchEngine(kg);
 * const results = search.search('unity');
 *
 * @example
 * // Quick start - facade pattern
 * import XRAI from 'xrai-kg';
 * const xrai = XRAI.create({ data: mcpData });
 * xrai.search('ARFoundation');
 */

// Data Layer
export { KnowledgeGraph, Entity, Relation } from './data/KnowledgeGraph.js';

// Search Layer
export { SearchEngine } from './search/SearchEngine.js';

// AI Layer
export { ChatParser, CommandType } from './ai/ChatParser.js';

// Visualization Layer
export { EChartsRenderer } from './viz/EChartsRenderer.js';
export { MermaidExporter } from './viz/MermaidExporter.js';

// Re-import for facade
import { KnowledgeGraph } from './data/KnowledgeGraph.js';
import { SearchEngine } from './search/SearchEngine.js';
import { ChatParser } from './ai/ChatParser.js';
import { EChartsRenderer } from './viz/EChartsRenderer.js';
import { MermaidExporter } from './viz/MermaidExporter.js';

/**
 * XRAI Facade - Convenience wrapper combining all layers
 * Use this for simple applications; use individual layers for more control
 */
export class XRAI {
    constructor(options = {}) {
        // Data Layer
        this.kg = new KnowledgeGraph();

        // Search Layer
        this.search = new SearchEngine(this.kg, options.search || {});

        // AI Layer
        this.chat = new ChatParser(options.chat || {});

        // Event listeners
        this._listeners = new Set();

        // Forward KG events
        this.kg.on(event => this._emit(event));
    }

    // ========== DATA OPERATIONS ==========

    /**
     * Load data from MCP format
     */
    load(data) {
        return this.kg.bulkAdd(data);
    }

    /**
     * Add a single entity
     */
    addEntity(data) {
        return this.kg.addEntity(data);
    }

    /**
     * Add a relation between entities
     */
    addRelation(from, to, type = 'related_to') {
        return this.kg.addRelation({ from, to, relationType: type });
    }

    /**
     * Get entity by ID
     */
    getEntity(id) {
        return this.kg.getEntity(id);
    }

    /**
     * Get entity by name
     */
    getEntityByName(name) {
        return this.kg.getEntityByName(name);
    }

    // ========== SEARCH OPERATIONS ==========

    /**
     * Fuzzy search entities
     * @param {string} query - Search query
     * @param {Object} options - { limit, threshold, types, minScore }
     */
    find(query, options = {}) {
        return this.search.search(query, options);
    }

    /**
     * Get autocomplete suggestions
     */
    suggest(partial, limit = 5) {
        return this.search.suggest(partial, limit);
    }

    /**
     * Filter entities by type
     */
    filterByTypes(types) {
        return this.search.filterByTypes(types);
    }

    /**
     * Get all entity types with counts
     */
    getTypes() {
        return this.search.getTypes();
    }

    // ========== GRAPH OPERATIONS ==========

    /**
     * Get neighboring entities
     */
    getNeighbors(entityName, depth = 1) {
        return this.kg.getNeighbors(entityName, depth);
    }

    /**
     * Get relations for an entity
     */
    getRelations(entityName) {
        return this.kg.getRelationsFor(entityName);
    }

    // ========== EXPORT ==========

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

    /**
     * Export to Mermaid diagram code
     * @param {Object} options - { diagramType, direction, showRelationLabels }
     */
    toMermaid(options = {}) {
        return MermaidExporter.toMermaid(this.kg, options);
    }

    // ========== VISUALIZATION ==========

    /**
     * Create ECharts renderer for visualization
     * @param {HTMLElement|string} container - DOM element or selector
     * @param {Object} options - Renderer options
     */
    createRenderer(container, options = {}) {
        const renderer = new EChartsRenderer(container, options);
        renderer.render(this.kg);
        return renderer;
    }

    // ========== CHAT COMMANDS ==========

    /**
     * Run a natural language command
     * @param {string} input - Natural language command
     * @returns {Object} Result with success, data, and message
     */
    run(input) {
        return this.chat.execute(input, this);
    }

    /**
     * Parse a command without executing
     */
    parseCommand(input) {
        return this.chat.parse(input);
    }

    // ========== EVENTS ==========

    /**
     * Subscribe to events
     */
    on(callback) {
        this._listeners.add(callback);
        return () => this._listeners.delete(callback);
    }

    _emit(event) {
        this._listeners.forEach(cb => {
            try { cb(event); }
            catch (e) { console.error('Event handler error:', e); }
        });
    }

    // ========== STATIC FACTORY ==========

    /**
     * Create XRAI instance with data
     */
    static create(options = {}) {
        const xrai = new XRAI(options);
        if (options.data) {
            xrai.load(options.data);
        }
        return xrai;
    }

    /**
     * Create from JSON string or object
     */
    static fromJSON(json) {
        const data = typeof json === 'string' ? JSON.parse(json) : json;
        return XRAI.create({ data });
    }
}

// Default export
export default XRAI;

// Version
export const VERSION = '1.0.0';
