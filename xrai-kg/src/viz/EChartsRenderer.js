/**
 * XRAI Knowledge Graph - Visualization Layer
 * ECharts/ECharts-GL renderer for knowledge graph visualization
 *
 * Principle: Use proven libraries, inject dependencies, don't bundle
 *
 * Supports:
 * - Regular graph (< 1000 nodes)
 * - GraphGL with WebGL acceleration (1000+ nodes)
 * - ForceAtlas2 GPU-accelerated layout
 * - Community detection (modularity)
 * - Interactive highlighting
 */

/**
 * Default color palette for entity types (30 distinct colors)
 */
const DEFAULT_COLORS = [
    '#5470c6', '#91cc75', '#fac858', '#ee6666', '#73c0de',
    '#3ba272', '#fc8452', '#9a60b4', '#ea7ccc', '#48b8d0',
    '#ff9f7f', '#87cefa', '#da70d6', '#32cd32', '#6495ed',
    '#ff69b4', '#ba55d3', '#cd5c5c', '#ffa500', '#40e0d0',
    '#1e90ff', '#ff6347', '#7b68ee', '#00fa9a', '#ffd700',
    '#6a5acd', '#ff1493', '#00ced1', '#ff4500', '#2e8b57'
];

/**
 * Performance presets based on node count
 */
const PERFORMANCE_PRESETS = {
    small: {      // < 500 nodes
        type: 'graph',
        layout: 'force',
        force: {
            repulsion: 100,
            gravity: 0.1,
            edgeLength: 100,
            layoutAnimation: true
        },
        label: { show: true },
        lineStyle: { opacity: 0.6 }
    },
    medium: {     // 500 - 2000 nodes
        type: 'graph',
        layout: 'force',
        force: {
            repulsion: 50,
            gravity: 0.2,
            edgeLength: 50,
            layoutAnimation: false
        },
        label: { show: false },
        lineStyle: { opacity: 0.3 }
    },
    large: {      // 2000 - 10000 nodes
        type: 'graphGL',
        forceAtlas2: {
            steps: 5,
            maxSteps: 3000,
            jitterTolerence: 10,
            gravity: 5,
            scaling: 1,
            edgeWeightInfluence: 0
        },
        label: { show: false },
        lineStyle: { opacity: 0.1 }
    },
    huge: {       // 10000+ nodes
        type: 'graphGL',
        forceAtlas2: {
            steps: 1,
            maxSteps: 1000,
            jitterTolerence: 10,
            gravity: 1,
            scaling: 0.2,
            edgeWeightInfluence: 1
        },
        label: { show: false },
        lineStyle: { opacity: 0.05 }
    }
};

/**
 * EChartsRenderer - Renders knowledge graphs using Apache ECharts
 */
export class EChartsRenderer {
    constructor(container, options = {}) {
        this.container = typeof container === 'string'
            ? document.querySelector(container)
            : container;

        this.options = {
            // ECharts instance (injected)
            echarts: options.echarts || null,

            // Auto-select graph type based on size
            autoPerformance: true,

            // Force specific type ('graph' or 'graphGL')
            forceType: null,

            // Colors for entity types
            colors: options.colors || DEFAULT_COLORS,

            // Theme ('light' or 'dark')
            theme: options.theme || 'dark',

            // Device pixel ratio (lower = better performance)
            devicePixelRatio: options.devicePixelRatio || 1,

            // Symbol size range
            symbolSize: options.symbolSize || [5, 30],

            // Show labels threshold (show if value > this)
            labelThreshold: options.labelThreshold || 10,

            // Enable community detection
            modularity: options.modularity !== false,

            // Click to focus neighbors
            focusOnClick: options.focusOnClick !== false,

            ...options
        };

        this.chart = null;
        this.kg = null;
        this._categories = [];
        this._nodeIndex = new Map();
    }

    /**
     * Set ECharts library (dependency injection)
     */
    static setEChartsLibrary(echartsLib) {
        EChartsRenderer._echarts = echartsLib;
    }

    /**
     * Get ECharts library
     */
    _getECharts() {
        return this.options.echarts || EChartsRenderer._echarts || window.echarts;
    }

    /**
     * Initialize the chart
     */
    init() {
        const echarts = this._getECharts();
        if (!echarts) {
            throw new Error('ECharts library not found. Set via EChartsRenderer.setEChartsLibrary() or pass in options.');
        }

        if (this.chart) {
            this.chart.dispose();
        }

        this.chart = echarts.init(this.container, this.options.theme, {
            devicePixelRatio: this.options.devicePixelRatio
        });

        // Handle resize
        this._resizeHandler = () => this.chart?.resize();
        window.addEventListener('resize', this._resizeHandler);

        return this;
    }

    /**
     * Render knowledge graph
     * @param {KnowledgeGraph} kg - Knowledge graph instance
     * @param {Object} options - Render options
     */
    render(kg, options = {}) {
        if (!this.chart) {
            this.init();
        }

        this.kg = kg;
        const data = this._transformData(kg);
        const preset = this._selectPreset(data.nodes.length);
        const chartOptions = this._buildOptions(data, preset, options);

        this.chart.setOption(chartOptions, true);

        return this;
    }

    /**
     * Update with new data (incremental)
     */
    update(kg) {
        return this.render(kg || this.kg);
    }

    /**
     * Transform KnowledgeGraph to ECharts format
     */
    _transformData(kg) {
        const entities = kg.getAllEntities();
        const relations = kg.getAllRelations();

        // Build categories from entity types
        const typeSet = new Set();
        entities.forEach(e => typeSet.add(e.entityType));
        this._categories = Array.from(typeSet).map((name, i) => ({
            name,
            itemStyle: { color: this.options.colors[i % this.options.colors.length] }
        }));

        const categoryIndex = {};
        this._categories.forEach((c, i) => categoryIndex[c.name] = i);

        // Build nodes
        this._nodeIndex.clear();
        const nodes = entities.map((entity, i) => {
            this._nodeIndex.set(entity.name, i);

            const value = (entity.observations?.length || 0) + 1;
            const node = {
                id: i,
                name: entity.name,
                value: value,
                category: categoryIndex[entity.entityType] || 0,
                symbolSize: this._calculateSymbolSize(value, entities.length),
                itemStyle: {
                    color: this.options.colors[categoryIndex[entity.entityType] % this.options.colors.length]
                }
            };

            // Conditional labels based on importance
            if (value > this.options.labelThreshold) {
                node.label = { show: true };
            }

            return node;
        });

        // Build edges
        const edges = relations
            .map(r => {
                const source = this._nodeIndex.get(r.from);
                const target = this._nodeIndex.get(r.to);

                if (source === undefined || target === undefined) {
                    return null;
                }

                return {
                    source,
                    target,
                    value: r.weight || 1,
                    lineStyle: {
                        type: r.relationType === 'related_to' ? 'solid' : 'dashed'
                    }
                };
            })
            .filter(Boolean);

        return { nodes, edges, categories: this._categories };
    }

    /**
     * Calculate symbol size based on value and total nodes
     */
    _calculateSymbolSize(value, totalNodes) {
        const [min, max] = this.options.symbolSize;

        if (totalNodes > 5000) {
            // Smaller sizes for huge graphs
            return Math.max(2, Math.min(10, Math.sqrt(value)));
        }

        // Scale based on value
        const scaled = Math.sqrt(value) * 3;
        return Math.max(min, Math.min(max, scaled));
    }

    /**
     * Select performance preset based on node count
     */
    _selectPreset(nodeCount) {
        if (this.options.forceType) {
            // Use forced type with appropriate settings
            if (this.options.forceType === 'graphGL') {
                return nodeCount > 5000 ? PERFORMANCE_PRESETS.huge : PERFORMANCE_PRESETS.large;
            }
            return nodeCount > 500 ? PERFORMANCE_PRESETS.medium : PERFORMANCE_PRESETS.small;
        }

        if (!this.options.autoPerformance) {
            return PERFORMANCE_PRESETS.medium;
        }

        if (nodeCount < 500) return PERFORMANCE_PRESETS.small;
        if (nodeCount < 2000) return PERFORMANCE_PRESETS.medium;
        if (nodeCount < 10000) return PERFORMANCE_PRESETS.large;
        return PERFORMANCE_PRESETS.huge;
    }

    /**
     * Build ECharts options
     */
    _buildOptions(data, preset, userOptions = {}) {
        const isGraphGL = preset.type === 'graphGL';

        const baseOptions = {
            backgroundColor: this.options.theme === 'dark' ? '#1a1a2e' : '#ffffff',

            tooltip: {
                trigger: 'item',
                formatter: (params) => {
                    if (params.dataType === 'node') {
                        const entity = this.kg?.getEntityByName(params.name);
                        if (entity) {
                            let html = `<strong>${entity.name}</strong><br/>`;
                            html += `Type: ${entity.entityType}<br/>`;
                            if (entity.observations?.length) {
                                html += `Observations: ${entity.observations.length}`;
                            }
                            return html;
                        }
                    }
                    return params.name;
                }
            },

            legend: {
                data: data.categories.map(c => c.name),
                orient: 'vertical',
                right: 10,
                top: 20,
                textStyle: {
                    color: this.options.theme === 'dark' ? '#ccc' : '#333'
                }
            },

            series: [{
                type: preset.type,
                name: 'Knowledge Graph',
                nodes: data.nodes,
                edges: data.edges,
                categories: data.categories,

                // Layout settings
                ...(isGraphGL ? {
                    forceAtlas2: {
                        ...preset.forceAtlas2,
                        ...userOptions.forceAtlas2
                    },
                    modularity: this.options.modularity ? {
                        resolution: 2,
                        sort: true
                    } : false
                } : {
                    layout: preset.layout,
                    force: {
                        ...preset.force,
                        ...userOptions.force
                    },
                    roam: true,
                    draggable: true
                }),

                // Interaction
                focusNodeAdjacencyOn: this.options.focusOnClick ? 'click' : 'none',

                // Styling
                label: {
                    show: preset.label.show,
                    position: 'right',
                    formatter: '{b}',
                    fontSize: 10,
                    color: this.options.theme === 'dark' ? '#fff' : '#333'
                },

                lineStyle: {
                    color: this.options.theme === 'dark'
                        ? 'rgba(255,255,255,0.3)'
                        : 'rgba(0,0,0,0.3)',
                    opacity: preset.lineStyle.opacity,
                    width: 1,
                    curveness: 0.1
                },

                emphasis: {
                    focus: 'adjacency',
                    label: { show: true },
                    lineStyle: {
                        opacity: 0.8,
                        width: 2
                    }
                },

                // Animation
                animation: !isGraphGL,
                animationDuration: 1000,
                animationEasingUpdate: 'quinticInOut'
            }]
        };

        return baseOptions;
    }

    // ========== CONTROL METHODS ==========

    /**
     * Start layout animation (GraphGL only)
     */
    startLayout() {
        this.chart?.dispatchAction({ type: 'graphGLStartLayout' });
        return this;
    }

    /**
     * Stop layout animation (GraphGL only)
     */
    stopLayout() {
        this.chart?.dispatchAction({ type: 'graphGLStopLayout' });
        return this;
    }

    /**
     * Focus on a specific node
     */
    focusNode(name) {
        const index = this._nodeIndex.get(name);
        if (index !== undefined) {
            this.chart?.dispatchAction({
                type: 'highlight',
                seriesIndex: 0,
                dataIndex: index
            });
        }
        return this;
    }

    /**
     * Clear focus
     */
    clearFocus() {
        this.chart?.dispatchAction({
            type: 'downplay',
            seriesIndex: 0
        });
        return this;
    }

    /**
     * Filter by entity types (show only specified types)
     */
    filterTypes(types) {
        const typeSet = new Set(types.map(t => t.toLowerCase()));

        this._categories.forEach(cat => {
            this.chart?.dispatchAction({
                type: typeSet.has(cat.name.toLowerCase()) ? 'legendSelect' : 'legendUnSelect',
                name: cat.name
            });
        });

        return this;
    }

    /**
     * Show all types
     */
    showAllTypes() {
        this._categories.forEach(cat => {
            this.chart?.dispatchAction({
                type: 'legendSelect',
                name: cat.name
            });
        });
        return this;
    }

    // ========== THEME ==========

    /**
     * Set theme
     */
    setTheme(theme) {
        this.options.theme = theme;
        if (this.kg) {
            this.render(this.kg);
        }
        return this;
    }

    // ========== EXPORT ==========

    /**
     * Export as image
     * @param {Object} options - { type: 'png'|'jpeg', pixelRatio, backgroundColor }
     */
    toImage(options = {}) {
        return this.chart?.getDataURL({
            type: options.type || 'png',
            pixelRatio: options.pixelRatio || 2,
            backgroundColor: options.backgroundColor || this.options.theme === 'dark' ? '#1a1a2e' : '#fff'
        });
    }

    /**
     * Get current options (for debugging)
     */
    getOptions() {
        return this.chart?.getOption();
    }

    // ========== EVENTS ==========

    /**
     * Subscribe to chart events
     */
    on(eventName, handler) {
        this.chart?.on(eventName, handler);
        return this;
    }

    /**
     * Unsubscribe from chart events
     */
    off(eventName, handler) {
        this.chart?.off(eventName, handler);
        return this;
    }

    // ========== LIFECYCLE ==========

    /**
     * Resize chart
     */
    resize() {
        this.chart?.resize();
        return this;
    }

    /**
     * Dispose chart and cleanup
     */
    dispose() {
        if (this._resizeHandler) {
            window.removeEventListener('resize', this._resizeHandler);
        }
        this.chart?.dispose();
        this.chart = null;
        this.kg = null;
    }
}

export default EChartsRenderer;
