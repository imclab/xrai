/**
 * XRAI Knowledge Graph - AI Layer
 * Natural language command parser for chat-based interactions
 *
 * Principle: Simple pattern matching first, LLM hooks for complex queries
 *
 * Commands supported:
 * - search <query>           → Fuzzy search entities
 * - find <query>             → Alias for search
 * - add <name> as <type>     → Add new entity
 * - relate <A> to <B> [as <type>]  → Add relation
 * - filter [by] <type>       → Filter entities by type
 * - neighbors [of] <name>    → Get connected entities
 * - stats                    → Show statistics
 * - export [mermaid|json]    → Export data
 * - clear                    → Clear graph
 * - help                     → Show commands
 */

/**
 * Command types returned by parser
 */
export const CommandType = {
    SEARCH: 'search',
    ADD_ENTITY: 'addEntity',
    ADD_RELATION: 'addRelation',
    FILTER: 'filter',
    NEIGHBORS: 'neighbors',
    STATS: 'stats',
    EXPORT: 'export',
    CLEAR: 'clear',
    HELP: 'help',
    UNKNOWN: 'unknown'
};

/**
 * ChatParser - Converts natural language to structured commands
 */
export class ChatParser {
    constructor(options = {}) {
        this.options = {
            caseSensitive: false,
            suggestionThreshold: 0.6,
            maxSuggestions: 3,
            ...options
        };

        // Command patterns (order matters - more specific first)
        this.patterns = [
            // Add entity: "add ARFoundation as Technology"
            {
                type: CommandType.ADD_ENTITY,
                patterns: [
                    /^add\s+(?:entity\s+)?["']?(.+?)["']?\s+as\s+["']?(.+?)["']?$/i,
                    /^create\s+(?:entity\s+)?["']?(.+?)["']?\s+(?:type|as)\s+["']?(.+?)["']?$/i,
                    /^new\s+(?:entity\s+)?["']?(.+?)["']?\s+(?:type|as)\s+["']?(.+?)["']?$/i
                ],
                extract: (match) => ({
                    name: match[1].trim(),
                    entityType: match[2].trim()
                })
            },

            // Add relation: "relate ARFoundation to Unity" or "connect A to B as uses"
            {
                type: CommandType.ADD_RELATION,
                patterns: [
                    /^(?:relate|connect|link)\s+["']?(.+?)["']?\s+(?:to|with|->)\s+["']?(.+?)["']?(?:\s+(?:as|type)\s+["']?(.+?)["']?)?$/i,
                    /^["']?(.+?)["']?\s+(?:->|-->|relates to|connects to)\s+["']?(.+?)["']?(?:\s+(?:as|type)\s+["']?(.+?)["']?)?$/i
                ],
                extract: (match) => ({
                    from: match[1].trim(),
                    to: match[2].trim(),
                    relationType: match[3]?.trim() || 'related_to'
                })
            },

            // Filter: "filter by Technology" or "show only Projects"
            {
                type: CommandType.FILTER,
                patterns: [
                    /^filter\s+(?:by\s+)?["']?(.+?)["']?$/i,
                    /^show\s+(?:only\s+)?["']?(.+?)["']?$/i,
                    /^type\s*[:=]?\s*["']?(.+?)["']?$/i
                ],
                extract: (match) => ({
                    types: match[1].split(/[,\s]+/).map(t => t.trim()).filter(Boolean)
                })
            },

            // Neighbors: "neighbors of ARFoundation" or "related to Unity"
            {
                type: CommandType.NEIGHBORS,
                patterns: [
                    /^neighbors?\s+(?:of\s+)?["']?(.+?)["']?(?:\s+depth\s+(\d+))?$/i,
                    /^related\s+(?:to\s+)?["']?(.+?)["']?(?:\s+depth\s+(\d+))?$/i,
                    /^connections?\s+(?:of|for|to)\s+["']?(.+?)["']?(?:\s+depth\s+(\d+))?$/i
                ],
                extract: (match) => ({
                    entityName: match[1].trim(),
                    depth: parseInt(match[2]) || 1
                })
            },

            // Export: "export mermaid" or "export json"
            {
                type: CommandType.EXPORT,
                patterns: [
                    /^export\s+(?:as\s+)?["']?(mermaid|json|csv)["']?$/i,
                    /^save\s+(?:as\s+)?["']?(mermaid|json|csv)["']?$/i,
                    /^download\s+["']?(mermaid|json|csv)["']?$/i
                ],
                extract: (match) => ({
                    format: match[1].toLowerCase()
                })
            },

            // Stats: "stats" or "statistics" or "info"
            {
                type: CommandType.STATS,
                patterns: [
                    /^(?:stats?|statistics?|info|summary)$/i
                ],
                extract: () => ({})
            },

            // Clear: "clear" or "reset"
            {
                type: CommandType.CLEAR,
                patterns: [
                    /^(?:clear|reset|empty)(?:\s+(?:graph|all|data))?$/i
                ],
                extract: () => ({})
            },

            // Help: "help" or "?"
            {
                type: CommandType.HELP,
                patterns: [
                    /^(?:help|\?|commands?)$/i
                ],
                extract: () => ({})
            },

            // Search (catch-all for queries): "search unity" or just "unity"
            {
                type: CommandType.SEARCH,
                patterns: [
                    /^(?:search|find|query|lookup|s|f)\s+(.+)$/i,
                    /^["'](.+)["']$/,  // Quoted text
                    /^(.{2,})$/        // Anything 2+ chars (lowest priority)
                ],
                extract: (match) => ({
                    query: match[1].trim()
                })
            }
        ];

        // Command aliases for suggestions
        this.aliases = {
            's': 'search',
            'f': 'find',
            'q': 'query',
            'n': 'neighbors',
            'r': 'relate',
            'a': 'add',
            '?': 'help'
        };
    }

    /**
     * Parse a chat message into a command
     * @param {string} input - User input
     * @returns {Object} Parsed command with type and params
     */
    parse(input) {
        if (!input || typeof input !== 'string') {
            return { type: CommandType.UNKNOWN, raw: input, params: {} };
        }

        const trimmed = input.trim();

        // Empty input
        if (!trimmed) {
            return { type: CommandType.UNKNOWN, raw: input, params: {} };
        }

        // Try each pattern
        for (const handler of this.patterns) {
            for (const pattern of handler.patterns) {
                const match = trimmed.match(pattern);
                if (match) {
                    return {
                        type: handler.type,
                        raw: input,
                        params: handler.extract(match)
                    };
                }
            }
        }

        // No match
        return { type: CommandType.UNKNOWN, raw: input, params: {} };
    }

    /**
     * Parse and execute command against XRAI instance
     * @param {string} input - User input
     * @param {XRAI} xrai - XRAI instance
     * @returns {Object} Result with success, data, and message
     */
    execute(input, xrai) {
        const command = this.parse(input);

        try {
            switch (command.type) {
                case CommandType.SEARCH:
                    return this._executeSearch(command.params, xrai);

                case CommandType.ADD_ENTITY:
                    return this._executeAddEntity(command.params, xrai);

                case CommandType.ADD_RELATION:
                    return this._executeAddRelation(command.params, xrai);

                case CommandType.FILTER:
                    return this._executeFilter(command.params, xrai);

                case CommandType.NEIGHBORS:
                    return this._executeNeighbors(command.params, xrai);

                case CommandType.STATS:
                    return this._executeStats(xrai);

                case CommandType.EXPORT:
                    return this._executeExport(command.params, xrai);

                case CommandType.CLEAR:
                    return this._executeClear(xrai);

                case CommandType.HELP:
                    return this._executeHelp();

                default:
                    return {
                        success: false,
                        command,
                        message: `Unknown command. Type "help" for available commands.`,
                        suggestions: this._getSuggestions(input)
                    };
            }
        } catch (error) {
            return {
                success: false,
                command,
                message: `Error: ${error.message}`,
                error
            };
        }
    }

    // ========== COMMAND EXECUTORS ==========

    _executeSearch({ query }, xrai) {
        const results = xrai.find(query, { limit: 20 });
        return {
            success: true,
            type: CommandType.SEARCH,
            data: results,
            message: results.length > 0
                ? `Found ${results.length} result(s) for "${query}"`
                : `No results found for "${query}"`
        };
    }

    _executeAddEntity({ name, entityType }, xrai) {
        const entity = xrai.addEntity({ name, entityType });
        return {
            success: true,
            type: CommandType.ADD_ENTITY,
            data: entity,
            message: `Added entity "${name}" (${entityType})`
        };
    }

    _executeAddRelation({ from, to, relationType }, xrai) {
        const relation = xrai.addRelation(from, to, relationType);
        return {
            success: true,
            type: CommandType.ADD_RELATION,
            data: relation,
            message: `Added relation: ${from} --[${relationType}]--> ${to}`
        };
    }

    _executeFilter({ types }, xrai) {
        const results = xrai.filterByTypes(types);
        return {
            success: true,
            type: CommandType.FILTER,
            data: results,
            message: `Found ${results.length} entities of type(s): ${types.join(', ')}`
        };
    }

    _executeNeighbors({ entityName, depth }, xrai) {
        const neighbors = xrai.getNeighbors(entityName, depth);
        return {
            success: true,
            type: CommandType.NEIGHBORS,
            data: neighbors,
            message: `Found ${neighbors.length} neighbor(s) of "${entityName}" (depth: ${depth})`
        };
    }

    _executeStats(xrai) {
        const stats = xrai.getStats();
        return {
            success: true,
            type: CommandType.STATS,
            data: stats,
            message: `Graph: ${stats.entityCount} entities, ${stats.relationCount} relations`
        };
    }

    _executeExport({ format }, xrai) {
        let data;
        switch (format) {
            case 'json':
                data = xrai.toJSON();
                break;
            case 'mermaid':
                data = this._generateMermaid(xrai);
                break;
            case 'csv':
                data = this._generateCSV(xrai);
                break;
            default:
                data = xrai.toJSON();
        }
        return {
            success: true,
            type: CommandType.EXPORT,
            data,
            format,
            message: `Exported graph as ${format.toUpperCase()}`
        };
    }

    _executeClear(xrai) {
        const stats = xrai.getStats();
        xrai.kg.clear();
        return {
            success: true,
            type: CommandType.CLEAR,
            data: stats,
            message: `Cleared ${stats.entityCount} entities and ${stats.relationCount} relations`
        };
    }

    _executeHelp() {
        const commands = [
            { cmd: 'search <query>', desc: 'Fuzzy search entities' },
            { cmd: 'add <name> as <type>', desc: 'Add new entity' },
            { cmd: 'relate <A> to <B> [as type]', desc: 'Add relation' },
            { cmd: 'filter [by] <type>', desc: 'Filter by entity type' },
            { cmd: 'neighbors [of] <name>', desc: 'Get connected entities' },
            { cmd: 'stats', desc: 'Show graph statistics' },
            { cmd: 'export [json|mermaid|csv]', desc: 'Export graph data' },
            { cmd: 'clear', desc: 'Clear all data' },
            { cmd: 'help', desc: 'Show this help' }
        ];

        return {
            success: true,
            type: CommandType.HELP,
            data: commands,
            message: 'Available commands:\n' +
                commands.map(c => `  ${c.cmd.padEnd(28)} - ${c.desc}`).join('\n')
        };
    }

    // ========== EXPORT HELPERS ==========

    _generateMermaid(xrai) {
        const data = xrai.toJSON();
        let mermaid = 'graph TD\n';

        // Sanitize ID
        const sanitize = (name) => name.replace(/[^a-zA-Z0-9]/g, '_');

        // Add entities
        const entityIds = new Map();
        data.entities.forEach((e, i) => {
            const id = sanitize(e.name) || `node_${i}`;
            entityIds.set(e.name, id);
            mermaid += `    ${id}["${e.name}"]\n`;
        });

        // Add relations
        data.relations.forEach(r => {
            const fromId = entityIds.get(r.from);
            const toId = entityIds.get(r.to);
            if (fromId && toId) {
                mermaid += `    ${fromId} -->|${r.relationType}| ${toId}\n`;
            }
        });

        return mermaid;
    }

    _generateCSV(xrai) {
        const data = xrai.toJSON();

        // Entities CSV
        let csv = 'type,section,data\n';

        data.entities.forEach(e => {
            csv += `entity,${e.entityType},"${e.name}"\n`;
        });

        data.relations.forEach(r => {
            csv += `relation,${r.relationType},"${r.from} -> ${r.to}"\n`;
        });

        return csv;
    }

    // ========== SUGGESTIONS ==========

    _getSuggestions(input) {
        const suggestions = [];
        const lower = input.toLowerCase().trim();

        // Command suggestions
        if (lower.length >= 1) {
            const commands = ['search', 'add', 'relate', 'filter', 'neighbors', 'stats', 'export', 'help'];
            commands.forEach(cmd => {
                if (cmd.startsWith(lower) || this._similarity(cmd, lower) > this.options.suggestionThreshold) {
                    suggestions.push(`Did you mean: ${cmd}?`);
                }
            });
        }

        return suggestions.slice(0, this.options.maxSuggestions);
    }

    _similarity(a, b) {
        // Simple Levenshtein-based similarity
        if (a === b) return 1;
        if (!a || !b) return 0;

        const longer = a.length > b.length ? a : b;
        const shorter = a.length > b.length ? b : a;

        if (longer.length === 0) return 1;

        return (longer.length - this._editDistance(longer, shorter)) / longer.length;
    }

    _editDistance(a, b) {
        const matrix = [];
        for (let i = 0; i <= b.length; i++) {
            matrix[i] = [i];
        }
        for (let j = 0; j <= a.length; j++) {
            matrix[0][j] = j;
        }
        for (let i = 1; i <= b.length; i++) {
            for (let j = 1; j <= a.length; j++) {
                if (b.charAt(i - 1) === a.charAt(j - 1)) {
                    matrix[i][j] = matrix[i - 1][j - 1];
                } else {
                    matrix[i][j] = Math.min(
                        matrix[i - 1][j - 1] + 1,
                        matrix[i][j - 1] + 1,
                        matrix[i - 1][j] + 1
                    );
                }
            }
        }
        return matrix[b.length][a.length];
    }

    // ========== LLM HOOK (OPTIONAL) ==========

    /**
     * Set LLM handler for complex queries
     * @param {Function} handler - async (input) => { type, params }
     */
    setLLMHandler(handler) {
        this.llmHandler = handler;
    }

    /**
     * Parse with LLM fallback for complex queries
     */
    async parseWithLLM(input) {
        const result = this.parse(input);

        // If unknown and LLM handler exists, try LLM
        if (result.type === CommandType.UNKNOWN && this.llmHandler) {
            try {
                const llmResult = await this.llmHandler(input);
                if (llmResult && llmResult.type) {
                    return { ...llmResult, raw: input, llmParsed: true };
                }
            } catch (e) {
                console.warn('LLM parsing failed:', e);
            }
        }

        return result;
    }
}

export default ChatParser;
