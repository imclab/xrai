/**
 * XRAI Knowledge Graph - Data Layer
 * Pure data management with no visualization or search logic
 *
 * Principle: Single responsibility - only manages entities and relations
 */

export class Entity {
    constructor(data) {
        this.id = data.id || Entity.generateId(data.name);
        this.name = data.name;
        this.entityType = data.entityType || data.type || 'Unknown';
        this.observations = data.observations || [];
        this.metadata = data.metadata || {};
        this.createdAt = data.createdAt || Date.now();
        this.updatedAt = Date.now();
    }

    static generateId(name) {
        let hash = 0;
        const str = name || '';
        for (let i = 0; i < str.length; i++) {
            hash = ((hash << 5) - hash) + str.charCodeAt(i);
            hash = hash & hash;
        }
        return `e_${Math.abs(hash).toString(36)}`;
    }

    addObservation(obs) {
        this.observations.push(obs);
        this.updatedAt = Date.now();
    }

    toJSON() {
        return {
            id: this.id,
            name: this.name,
            entityType: this.entityType,
            observations: this.observations,
            metadata: this.metadata
        };
    }
}

export class Relation {
    constructor(data) {
        this.id = data.id || Relation.generateId(data.from, data.to);
        this.from = data.from;
        this.to = data.to;
        this.relationType = data.relationType || data.type || 'related_to';
        this.weight = data.weight || 1;
        this.metadata = data.metadata || {};
        this.createdAt = data.createdAt || Date.now();
    }

    static generateId(from, to) {
        return `r_${Entity.generateId(from)}_${Entity.generateId(to)}`;
    }

    toJSON() {
        return {
            id: this.id,
            from: this.from,
            to: this.to,
            relationType: this.relationType,
            weight: this.weight,
            metadata: this.metadata
        };
    }
}

export class KnowledgeGraph {
    constructor() {
        this._entities = new Map();  // id -> Entity
        this._relations = [];
        this._nameIndex = new Map(); // name -> id (for lookups)
        this._listeners = new Set();
    }

    // ========== ENTITY OPERATIONS ==========

    addEntity(data) {
        const entity = data instanceof Entity ? data : new Entity(data);

        if (this._entities.has(entity.id)) {
            // Merge observations if entity exists
            const existing = this._entities.get(entity.id);
            entity.observations.forEach(obs => {
                if (!existing.observations.includes(obs)) {
                    existing.addObservation(obs);
                }
            });
            this._emit('entityUpdated', existing);
            return existing;
        }

        this._entities.set(entity.id, entity);
        this._nameIndex.set(entity.name.toLowerCase(), entity.id);
        this._emit('entityAdded', entity);
        return entity;
    }

    getEntity(id) {
        return this._entities.get(id) || null;
    }

    getEntityByName(name) {
        const id = this._nameIndex.get(name.toLowerCase());
        return id ? this._entities.get(id) : null;
    }

    removeEntity(id) {
        const entity = this._entities.get(id);
        if (!entity) return false;

        this._entities.delete(id);
        this._nameIndex.delete(entity.name.toLowerCase());

        // Remove related relations
        this._relations = this._relations.filter(r =>
            r.from !== entity.name && r.to !== entity.name
        );

        this._emit('entityRemoved', entity);
        return true;
    }

    getAllEntities() {
        return Array.from(this._entities.values());
    }

    get entityCount() {
        return this._entities.size;
    }

    // ========== RELATION OPERATIONS ==========

    addRelation(data) {
        const relation = data instanceof Relation ? data : new Relation(data);

        // Check for duplicate
        const exists = this._relations.some(r =>
            r.from === relation.from &&
            r.to === relation.to &&
            r.relationType === relation.relationType
        );

        if (exists) return null;

        this._relations.push(relation);
        this._emit('relationAdded', relation);
        return relation;
    }

    getRelationsFor(entityName) {
        return this._relations.filter(r =>
            r.from === entityName || r.to === entityName
        );
    }

    getRelationsFrom(entityName) {
        return this._relations.filter(r => r.from === entityName);
    }

    getRelationsTo(entityName) {
        return this._relations.filter(r => r.to === entityName);
    }

    removeRelation(id) {
        const idx = this._relations.findIndex(r => r.id === id);
        if (idx === -1) return false;

        const relation = this._relations.splice(idx, 1)[0];
        this._emit('relationRemoved', relation);
        return true;
    }

    getAllRelations() {
        return [...this._relations];
    }

    get relationCount() {
        return this._relations.length;
    }

    // ========== BULK OPERATIONS ==========

    bulkAdd(data) {
        const results = { entities: 0, relations: 0, errors: [] };

        if (data.entities) {
            data.entities.forEach(e => {
                try {
                    this.addEntity(e);
                    results.entities++;
                } catch (err) {
                    results.errors.push({ type: 'entity', data: e, error: err.message });
                }
            });
        }

        if (data.relations) {
            data.relations.forEach(r => {
                try {
                    if (this.addRelation(r)) {
                        results.relations++;
                    }
                } catch (err) {
                    results.errors.push({ type: 'relation', data: r, error: err.message });
                }
            });
        }

        this._emit('bulkAdd', results);
        return results;
    }

    clear() {
        this._entities.clear();
        this._nameIndex.clear();
        this._relations = [];
        this._emit('cleared');
    }

    // ========== SERIALIZATION ==========

    toJSON() {
        return {
            entities: this.getAllEntities().map(e => e.toJSON()),
            relations: this.getAllRelations().map(r => r.toJSON())
        };
    }

    static fromJSON(json) {
        const data = typeof json === 'string' ? JSON.parse(json) : json;
        const kg = new KnowledgeGraph();
        kg.bulkAdd(data);
        return kg;
    }

    // ========== GRAPH TRAVERSAL ==========

    getNeighbors(entityName, depth = 1) {
        const visited = new Set([entityName]);
        let current = [entityName];
        const neighbors = [];

        for (let d = 0; d < depth; d++) {
            const next = [];
            current.forEach(name => {
                this._relations.forEach(r => {
                    let target = null;
                    let direction = null;

                    if (r.from === name && !visited.has(r.to)) {
                        target = r.to;
                        direction = 'outgoing';
                    } else if (r.to === name && !visited.has(r.from)) {
                        target = r.from;
                        direction = 'incoming';
                    }

                    if (target) {
                        visited.add(target);
                        next.push(target);
                        neighbors.push({
                            name: target,
                            entity: this.getEntityByName(target),
                            relation: r,
                            direction,
                            depth: d + 1
                        });
                    }
                });
            });
            current = next;
            if (next.length === 0) break;
        }

        return neighbors;
    }

    // ========== STATISTICS ==========

    getStats() {
        const typeCount = {};
        this._entities.forEach(e => {
            typeCount[e.entityType] = (typeCount[e.entityType] || 0) + 1;
        });

        const relationTypeCount = {};
        this._relations.forEach(r => {
            relationTypeCount[r.relationType] = (relationTypeCount[r.relationType] || 0) + 1;
        });

        return {
            entityCount: this._entities.size,
            relationCount: this._relations.length,
            typeCount,
            relationTypeCount,
            types: Object.keys(typeCount),
            relationTypes: Object.keys(relationTypeCount)
        };
    }

    // ========== EVENT SYSTEM ==========

    on(callback) {
        this._listeners.add(callback);
        return () => this._listeners.delete(callback);
    }

    _emit(event, data) {
        const payload = { type: event, data, timestamp: Date.now() };
        this._listeners.forEach(cb => {
            try { cb(payload); } catch (e) { console.error('Event handler error:', e); }
        });
    }
}

export default KnowledgeGraph;
