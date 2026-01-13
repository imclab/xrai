/**
 * XRAI Knowledge Graph - Mermaid Exporter
 * Exports knowledge graphs to Mermaid diagram syntax
 *
 * Supports:
 * - flowchart (default)
 * - graph (alias for flowchart)
 * - mindmap
 * - classDiagram
 */

/**
 * Shape mappings for entity types
 */
const TYPE_SHAPES = {
    // Default shapes by common type names
    project: 'hexagon',      // {{name}}
    technology: 'stadium',   // ([name])
    framework: 'stadium',    // ([name])
    concept: 'circle',       // ((name))
    pattern: 'diamond',      // {name}
    file: 'rect',           // [name]
    class: 'rect',          // [name]
    method: 'subroutine',   // [[name]]
    default: 'rounded'       // (name)
};

/**
 * Relation arrow mappings
 */
const RELATION_ARROWS = {
    uses: '-->',
    extends: '--|>',
    implements: '..|>',
    depends_on: '-.->',
    contains: '--o',
    related_to: '---',
    calls: '-->',
    imports: '-.->',
    default: '-->'
};

/**
 * MermaidExporter - Converts KnowledgeGraph to Mermaid syntax
 */
export class MermaidExporter {
    constructor(options = {}) {
        this.options = {
            // Diagram type: 'flowchart', 'graph', 'mindmap', 'classDiagram'
            diagramType: options.diagramType || 'flowchart',

            // Direction: TB (top-bottom), LR (left-right), BT, RL
            direction: options.direction || 'TD',

            // Shape mappings (type -> shape)
            shapes: { ...TYPE_SHAPES, ...options.shapes },

            // Arrow mappings (relationType -> arrow)
            arrows: { ...RELATION_ARROWS, ...options.arrows },

            // Include relation labels
            showRelationLabels: options.showRelationLabels !== false,

            // Max nodes before warning
            maxNodes: options.maxNodes || 100,

            // Styling options
            useStyles: options.useStyles !== false,

            ...options
        };
    }

    /**
     * Export KnowledgeGraph to Mermaid code
     * @param {KnowledgeGraph} kg - Knowledge graph instance
     * @param {Object} options - Override options
     * @returns {string} Mermaid diagram code
     */
    export(kg, options = {}) {
        const opts = { ...this.options, ...options };
        const type = opts.diagramType.toLowerCase();

        switch (type) {
            case 'flowchart':
            case 'graph':
                return this._exportFlowchart(kg, opts);
            case 'mindmap':
                return this._exportMindmap(kg, opts);
            case 'classdiagram':
            case 'class':
                return this._exportClassDiagram(kg, opts);
            default:
                return this._exportFlowchart(kg, opts);
        }
    }

    /**
     * Export as flowchart/graph
     */
    _exportFlowchart(kg, opts) {
        const entities = kg.getAllEntities();
        const relations = kg.getAllRelations();

        let mermaid = `flowchart ${opts.direction}\n`;

        // Warn if too many nodes
        if (entities.length > opts.maxNodes) {
            mermaid = `%% Warning: ${entities.length} nodes (max recommended: ${opts.maxNodes})\n` + mermaid;
        }

        // Group entities by type for subgraphs
        const byType = new Map();
        entities.forEach(e => {
            const type = e.entityType || 'Unknown';
            if (!byType.has(type)) byType.set(type, []);
            byType.get(type).push(e);
        });

        // Add subgraphs for each type
        byType.forEach((typeEntities, type) => {
            mermaid += `\n    subgraph ${this._sanitizeId(type)}["${type}"]\n`;

            typeEntities.forEach(entity => {
                const id = this._sanitizeId(entity.name);
                const shape = this._getShape(entity.entityType, opts.shapes);
                mermaid += `        ${id}${shape.open}"${this._escapeLabel(entity.name)}"${shape.close}\n`;
            });

            mermaid += `    end\n`;
        });

        // Add relations
        mermaid += '\n';
        relations.forEach(r => {
            const fromId = this._sanitizeId(r.from);
            const toId = this._sanitizeId(r.to);
            const arrow = opts.arrows[r.relationType] || opts.arrows.default;

            if (opts.showRelationLabels && r.relationType !== 'related_to') {
                mermaid += `    ${fromId} ${arrow}|${r.relationType}| ${toId}\n`;
            } else {
                mermaid += `    ${fromId} ${arrow} ${toId}\n`;
            }
        });

        // Add styles if enabled
        if (opts.useStyles) {
            mermaid += this._generateStyles(byType, opts);
        }

        return mermaid;
    }

    /**
     * Export as mindmap (hierarchical)
     */
    _exportMindmap(kg, opts) {
        const entities = kg.getAllEntities();
        const relations = kg.getAllRelations();

        let mermaid = `mindmap\n`;

        // Find root nodes (no incoming relations)
        const hasIncoming = new Set(relations.map(r => r.to));
        const roots = entities.filter(e => !hasIncoming.has(e.name));

        if (roots.length === 0) {
            // No clear root, use first entity or create artificial root
            mermaid += `    root((Knowledge Graph))\n`;
            entities.slice(0, 10).forEach(e => {
                mermaid += `        ${this._escapeLabel(e.name)}\n`;
            });
        } else {
            // Build tree from each root
            const visited = new Set();
            roots.forEach(root => {
                this._buildMindmapBranch(mermaid, root, relations, entities, visited, 1);
            });

            // Rebuild mermaid with proper structure
            mermaid = `mindmap\n`;
            roots.slice(0, 5).forEach(root => {
                mermaid += `    ${this._getMindmapShape(root.entityType)}${this._escapeLabel(root.name)}${this._getMindmapShapeClose(root.entityType)}\n`;
                mermaid = this._addMindmapChildren(mermaid, root.name, relations, entities, new Set([root.name]), 2);
            });
        }

        return mermaid;
    }

    _addMindmapChildren(mermaid, parentName, relations, entities, visited, depth) {
        if (depth > 5) return mermaid; // Max depth

        const children = relations
            .filter(r => r.from === parentName && !visited.has(r.to))
            .map(r => entities.find(e => e.name === r.to))
            .filter(Boolean);

        const indent = '    '.repeat(depth);
        children.slice(0, 5).forEach(child => {
            visited.add(child.name);
            mermaid += `${indent}${this._escapeLabel(child.name)}\n`;
            mermaid = this._addMindmapChildren(mermaid, child.name, relations, entities, visited, depth + 1);
        });

        return mermaid;
    }

    _getMindmapShape(type) {
        const shapes = {
            project: '{{',
            concept: '((',
            default: '('
        };
        return shapes[type?.toLowerCase()] || shapes.default;
    }

    _getMindmapShapeClose(type) {
        const shapes = {
            project: '}}',
            concept: '))',
            default: ')'
        };
        return shapes[type?.toLowerCase()] || shapes.default;
    }

    /**
     * Export as class diagram
     */
    _exportClassDiagram(kg, opts) {
        const entities = kg.getAllEntities();
        const relations = kg.getAllRelations();

        let mermaid = `classDiagram\n`;

        // Add classes (entities)
        entities.forEach(entity => {
            const className = this._sanitizeId(entity.name);
            mermaid += `    class ${className} {\n`;
            mermaid += `        <<${entity.entityType}>>\n`;

            // Add observations as attributes
            if (entity.observations?.length) {
                entity.observations.slice(0, 5).forEach(obs => {
                    const shortObs = obs.length > 30 ? obs.substring(0, 30) + '...' : obs;
                    mermaid += `        +${this._sanitizeAttribute(shortObs)}\n`;
                });
            }

            mermaid += `    }\n`;
        });

        // Add relationships
        relations.forEach(r => {
            const from = this._sanitizeId(r.from);
            const to = this._sanitizeId(r.to);
            const arrow = this._getClassDiagramArrow(r.relationType);

            mermaid += `    ${from} ${arrow} ${to} : ${r.relationType}\n`;
        });

        return mermaid;
    }

    _getClassDiagramArrow(relationType) {
        const arrows = {
            extends: '<|--',
            implements: '<|..',
            uses: '-->',
            depends_on: '..>',
            contains: '*--',
            aggregates: 'o--',
            related_to: '--'
        };
        return arrows[relationType] || '-->';
    }

    // ========== HELPERS ==========

    /**
     * Sanitize string for use as Mermaid ID
     */
    _sanitizeId(str) {
        if (!str) return 'unknown';
        return str
            .replace(/[^a-zA-Z0-9_]/g, '_')
            .replace(/^[0-9]/, '_$&')
            .substring(0, 50);
    }

    /**
     * Escape label for Mermaid
     */
    _escapeLabel(str) {
        if (!str) return '';
        return str
            .replace(/"/g, "'")
            .replace(/[\[\]{}()<>]/g, ' ')
            .trim();
    }

    /**
     * Sanitize attribute name
     */
    _sanitizeAttribute(str) {
        return str
            .replace(/[^a-zA-Z0-9_\s]/g, '')
            .replace(/\s+/g, '_')
            .substring(0, 40);
    }

    /**
     * Get shape brackets for entity type
     */
    _getShape(type, shapes) {
        const shapeName = shapes[type?.toLowerCase()] || shapes.default;

        const shapeMap = {
            rect: { open: '[', close: ']' },
            rounded: { open: '(', close: ')' },
            stadium: { open: '([', close: '])' },
            circle: { open: '((', close: '))' },
            diamond: { open: '{', close: '}' },
            hexagon: { open: '{{', close: '}}' },
            subroutine: { open: '[[', close: ']]' },
            cylinder: { open: '[(', close: ')]' },
            parallelogram: { open: '[/', close: '/]' }
        };

        return shapeMap[shapeName] || shapeMap.rounded;
    }

    /**
     * Generate CSS-like styles for subgraphs
     */
    _generateStyles(byType, opts) {
        let styles = '\n';

        const colors = [
            '#e1f5fe', '#fff3e0', '#f3e5f5', '#e8f5e9',
            '#fce4ec', '#e0f2f1', '#fff8e1', '#f1f8e9'
        ];

        let i = 0;
        byType.forEach((_, type) => {
            const id = this._sanitizeId(type);
            const color = colors[i % colors.length];
            styles += `    style ${id} fill:${color},stroke:#333\n`;
            i++;
        });

        return styles;
    }

    // ========== STATIC UTILITIES ==========

    /**
     * Quick export without creating instance
     */
    static toMermaid(kg, options = {}) {
        const exporter = new MermaidExporter(options);
        return exporter.export(kg, options);
    }

    /**
     * Validate Mermaid syntax (basic check)
     */
    static validate(mermaidCode) {
        const errors = [];

        // Check for basic structure
        if (!mermaidCode.match(/^(flowchart|graph|mindmap|classDiagram)/m)) {
            errors.push('Missing diagram type declaration');
        }

        // Check for unmatched brackets
        const brackets = { '[': ']', '(': ')', '{': '}', '<': '>' };
        const stack = [];

        for (const char of mermaidCode) {
            if (Object.keys(brackets).includes(char)) {
                stack.push(char);
            } else if (Object.values(brackets).includes(char)) {
                const expected = brackets[stack.pop()];
                if (char !== expected) {
                    errors.push(`Unmatched bracket: expected ${expected}, got ${char}`);
                }
            }
        }

        if (stack.length > 0) {
            errors.push(`Unclosed brackets: ${stack.join(', ')}`);
        }

        return {
            valid: errors.length === 0,
            errors
        };
    }
}

export default MermaidExporter;
