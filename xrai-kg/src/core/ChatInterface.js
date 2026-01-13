/**
 * XRAI Knowledge Graph - Chat/Command Interface
 * Natural language processing for graph operations
 *
 * Supports: search, add, filter, navigate, export commands
 */

export class ChatInterface {
    constructor(knowledgeGraph, options = {}) {
        this.kg = knowledgeGraph;
        this.history = [];
        this.maxHistory = options.maxHistory || 50;
        this.suggestions = [];

        // Command patterns
        this.commands = [
            { pattern: /^(search|find|look for|show)\s+(.+)/i, handler: 'search' },
            { pattern: /^(add|create|new)\s+(entity|node)\s+(.+)/i, handler: 'addEntity' },
            { pattern: /^(add|create|new)\s+(relation|link|edge)\s+(.+)\s+(to|->|→)\s+(.+)/i, handler: 'addRelation' },
            { pattern: /^(filter|show only)\s+(by type\s+)?(.+)/i, handler: 'filter' },
            { pattern: /^(related|connections|neighbors)\s+(to|of|for)\s+(.+)/i, handler: 'related' },
            { pattern: /^(export|download)\s+(mermaid|json|md)/i, handler: 'export' },
            { pattern: /^(clear|reset)\s+(filter|search|all)/i, handler: 'clear' },
            { pattern: /^(stats|status|info)/i, handler: 'stats' },
            { pattern: /^(help|\?)/i, handler: 'help' },
            { pattern: /^(types|categories)/i, handler: 'listTypes' },
            { pattern: /^(recent|history)/i, handler: 'showHistory' },
            { pattern: /^@(\w+)\s*(.*)/i, handler: 'shortcut' } // @search, @add shortcuts
        ];

        // Shortcut mappings
        this.shortcuts = {
            s: 'search',
            f: 'filter',
            a: 'add entity',
            r: 'related to',
            e: 'export',
            t: 'types',
            h: 'help'
        };
    }

    /**
     * Process a natural language command
     * @param {string} input - User input
     * @returns {Object} Result with type, data, and suggestions
     */
    process(input) {
        const trimmed = input.trim();
        if (!trimmed) return this._result('empty', null, 'Type a command or search query');

        // Add to history
        this._addToHistory(trimmed);

        // Try to match a command pattern
        for (const cmd of this.commands) {
            const match = trimmed.match(cmd.pattern);
            if (match) {
                return this._handleCommand(cmd.handler, match);
            }
        }

        // Default: treat as search
        return this._handleSearch(trimmed);
    }

    _handleCommand(handler, match) {
        switch (handler) {
            case 'search':
                return this._handleSearch(match[2]);

            case 'addEntity':
                return this._handleAddEntity(match[3]);

            case 'addRelation':
                return this._handleAddRelation(match[3], match[5]);

            case 'filter':
                return this._handleFilter(match[3]);

            case 'related':
                return this._handleRelated(match[3]);

            case 'export':
                return this._handleExport(match[2]);

            case 'clear':
                return this._handleClear(match[2]);

            case 'stats':
                return this._handleStats();

            case 'help':
                return this._handleHelp();

            case 'listTypes':
                return this._handleListTypes();

            case 'showHistory':
                return this._handleShowHistory();

            case 'shortcut':
                const expanded = this.shortcuts[match[1]] || match[1];
                return this.process(`${expanded} ${match[2]}`);

            default:
                return this._result('error', null, `Unknown command: ${handler}`);
        }
    }

    _handleSearch(query) {
        const results = this.kg.search(query, { limit: 20, fuzzy: true });

        if (results.length === 0) {
            // Provide suggestions
            const suggestions = this.kg.getSuggestions(query, 3);
            return this._result('no_results', { query },
                suggestions.length > 0
                    ? `No results for "${query}". Did you mean: ${suggestions.map(s => s.text).join(', ')}?`
                    : `No results found for "${query}"`
            );
        }

        return this._result('search', {
            query,
            results: results.map(r => ({
                name: r.entity.name,
                type: r.entity.entityType,
                score: Math.round(r.score * 100),
                id: r.entity.id
            })),
            total: results.length
        }, `Found ${results.length} results for "${query}"`);
    }

    _handleAddEntity(description) {
        // Parse "name as type" or just "name"
        const asMatch = description.match(/^(.+?)\s+as\s+(\w+)$/i);
        const name = asMatch ? asMatch[1].trim() : description;
        const type = asMatch ? asMatch[2] : 'Concept';

        const entity = this.kg.addEntity({ name, entityType: type });

        return this._result('add_entity', { entity },
            `Added "${name}" as ${type}`
        );
    }

    _handleAddRelation(from, to) {
        // Check if entities exist
        const fromResults = this.kg.search(from, { limit: 1, fuzzy: true });
        const toResults = this.kg.search(to, { limit: 1, fuzzy: true });

        if (fromResults.length === 0) {
            return this._result('error', null, `Entity not found: "${from}". Add it first.`);
        }
        if (toResults.length === 0) {
            return this._result('error', null, `Entity not found: "${to}". Add it first.`);
        }

        const fromEntity = fromResults[0].entity;
        const toEntity = toResults[0].entity;

        const relation = this.kg.addRelation({
            from: fromEntity.name,
            to: toEntity.name,
            relationType: 'related_to'
        });

        return this._result('add_relation', { relation, from: fromEntity, to: toEntity },
            `Linked "${fromEntity.name}" → "${toEntity.name}"`
        );
    }

    _handleFilter(typeQuery) {
        const types = typeQuery.split(/[,\s]+/).map(t => t.trim().toLowerCase());
        const results = this.kg.filterByTypes(types);

        return this._result('filter', {
            types,
            results: results.map(e => ({ name: e.name, type: e.entityType })),
            total: results.length
        }, `Filtered to ${results.length} entities of type: ${types.join(', ')}`);
    }

    _handleRelated(entityQuery) {
        const searchResults = this.kg.search(entityQuery, { limit: 1, fuzzy: true });

        if (searchResults.length === 0) {
            return this._result('error', null, `Entity not found: "${entityQuery}"`);
        }

        const entity = searchResults[0].entity;
        const related = this.kg.getRelatedEntities(entity.id, 2);

        return this._result('related', {
            source: entity,
            related: related.map(r => ({
                name: r.entity?.name,
                type: r.entity?.entityType,
                relation: r.relation.relationType,
                depth: r.depth
            })),
            total: related.length
        }, `Found ${related.length} connections to "${entity.name}"`);
    }

    _handleExport(format) {
        const formatLower = format.toLowerCase();
        let content, filename;

        switch (formatLower) {
            case 'mermaid':
            case 'md':
                content = this.kg.toMermaid();
                filename = 'knowledge-graph.mmd';
                break;
            case 'json':
                content = JSON.stringify(this.kg.toJSON(), null, 2);
                filename = 'knowledge-graph.json';
                break;
            default:
                return this._result('error', null, `Unknown format: ${format}`);
        }

        return this._result('export', { content, filename, format: formatLower },
            `Ready to export as ${formatLower}`
        );
    }

    _handleClear(what) {
        const whatLower = what.toLowerCase();
        switch (whatLower) {
            case 'filter':
                return this._result('clear_filter', {}, 'Filters cleared');
            case 'search':
                return this._result('clear_search', {}, 'Search cleared');
            case 'all':
                return this._result('clear_all', {}, 'All cleared');
            default:
                return this._result('error', null, `Unknown clear target: ${what}`);
        }
    }

    _handleStats() {
        const stats = this.kg.stats;
        const types = {};
        this.kg.getAllEntities().forEach(e => {
            const t = e.entityType || 'Unknown';
            types[t] = (types[t] || 0) + 1;
        });

        return this._result('stats', {
            entityCount: stats.entityCount,
            relationCount: stats.relationCount,
            typeBreakdown: types,
            searchCount: stats.searchCount,
            lastModified: stats.lastModified
        }, `${stats.entityCount} entities, ${stats.relationCount} relations`);
    }

    _handleHelp() {
        const help = {
            commands: [
                { cmd: 'search <query>', desc: 'Fuzzy search entities' },
                { cmd: 'add entity <name> as <type>', desc: 'Add new entity' },
                { cmd: 'add relation <from> to <to>', desc: 'Link two entities' },
                { cmd: 'filter <types>', desc: 'Show only certain types' },
                { cmd: 'related to <entity>', desc: 'Find connections' },
                { cmd: 'export mermaid|json', desc: 'Export graph' },
                { cmd: 'types', desc: 'List all entity types' },
                { cmd: 'stats', desc: 'Show statistics' },
                { cmd: 'clear filter|search|all', desc: 'Reset view' }
            ],
            shortcuts: [
                { key: '@s', desc: 'Quick search' },
                { key: '@f', desc: 'Quick filter' },
                { key: '@a', desc: 'Quick add' },
                { key: '@r', desc: 'Quick relations' }
            ],
            tips: [
                'Type anything to search',
                'Fuzzy matching handles typos',
                'Use quotes for exact phrases'
            ]
        };

        return this._result('help', help, 'Available commands');
    }

    _handleListTypes() {
        const types = {};
        this.kg.getAllEntities().forEach(e => {
            const t = e.entityType || 'Unknown';
            types[t] = (types[t] || 0) + 1;
        });

        const sorted = Object.entries(types).sort((a, b) => b[1] - a[1]);

        return this._result('types', {
            types: sorted.map(([name, count]) => ({ name, count }))
        }, `${sorted.length} entity types`);
    }

    _handleShowHistory() {
        return this._result('history', {
            commands: this.history.slice(-10).reverse()
        }, `Last ${Math.min(this.history.length, 10)} commands`);
    }

    // ========== SUGGESTIONS ==========

    getSuggestions(partial) {
        if (!partial || partial.length < 2) {
            return this._getCommandSuggestions();
        }

        const suggestions = [];

        // Command suggestions
        if (partial.startsWith('@')) {
            Object.entries(this.shortcuts).forEach(([key, cmd]) => {
                if (key.startsWith(partial.slice(1))) {
                    suggestions.push({ type: 'shortcut', text: `@${key}`, desc: cmd });
                }
            });
        }

        // Entity suggestions from fuzzy search
        const entitySuggestions = this.kg.getSuggestions(partial, 5);
        entitySuggestions.forEach(s => {
            suggestions.push({ type: 'entity', text: s.text, desc: s.type });
        });

        // Command completions
        const cmdWords = ['search', 'find', 'add', 'filter', 'related', 'export', 'clear', 'stats', 'help'];
        cmdWords.forEach(cmd => {
            if (cmd.startsWith(partial.toLowerCase())) {
                suggestions.push({ type: 'command', text: cmd, desc: 'command' });
            }
        });

        return suggestions.slice(0, 8);
    }

    _getCommandSuggestions() {
        return [
            { type: 'command', text: 'search', desc: 'Find entities' },
            { type: 'command', text: 'filter', desc: 'Filter by type' },
            { type: 'command', text: 'stats', desc: 'View statistics' },
            { type: 'command', text: 'help', desc: 'Show help' }
        ];
    }

    // ========== UTILITIES ==========

    _result(type, data, message) {
        return {
            type,
            data,
            message,
            timestamp: Date.now(),
            suggestions: this.getSuggestions('')
        };
    }

    _addToHistory(command) {
        this.history.push({ command, timestamp: Date.now() });
        if (this.history.length > this.maxHistory) {
            this.history.shift();
        }
    }

    getHistory() {
        return [...this.history];
    }

    clearHistory() {
        this.history = [];
    }
}

export default ChatInterface;
