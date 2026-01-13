// Streaming Data Manager for non-blocking, dynamic search results
import { DataManager } from './DataManager.js';

export class StreamingDataManager extends DataManager {
    constructor() {
        super();
        this.activeSearches = new Map(); // Track active search operations
        this.searchCallbacks = new Map(); // Callbacks for progressive results
        this.batchSize = 100; // Results to process per batch
        this.debounceTime = 150; // Debounce for typing
        this.searchQueue = [];
        this.isProcessing = false;
        this.maxConcurrentSearches = 5;
    }
    
    // Non-blocking search with progressive results
    async searchStreaming(query, sources = ['all'], options = {}) {
        const searchId = this.generateSearchId();
        const abortController = new AbortController();
        
        // Cancel previous searches for the same query pattern
        this.cancelSimilarSearches(query);
        
        // Store search metadata
        this.activeSearches.set(searchId, {
            query,
            sources,
            abortController,
            startTime: Date.now(),
            resultsCount: 0,
            completed: false
        });
        
        // Return search handle for cancellation and progress tracking
        const searchHandle = {
            id: searchId,
            cancel: () => this.cancelSearch(searchId),
            onProgress: (callback) => this.addProgressCallback(searchId, callback),
            onComplete: (callback) => this.addCompleteCallback(searchId, callback),
            promise: this.performStreamingSearch(searchId, query, sources, options)
        };
        
        return searchHandle;
    }
    
    async performStreamingSearch(searchId, query, sources, options) {
        const searchMeta = this.activeSearches.get(searchId);
        if (!searchMeta) return { results: [], cancelled: true };
        
        const allResults = [];
        const activeSources = sources.includes('all') 
            ? Object.keys(this.searchProviders)
            : sources;
        
        // Create promise for each source with timeout
        const searchPromises = activeSources.map((source, index) => 
            this.searchSourceWithTimeout(
                source, 
                query, 
                searchMeta.abortController.signal,
                options.timeout || 5000
            ).then(results => ({
                status: 'fulfilled',
                results,
                source,
                index
            })).catch(error => ({
                status: 'rejected',
                error,
                source,
                index
            }))
        );
        
        const pending = new Set(searchPromises);
        
        // Process results as each source completes
        while (pending.size > 0 && !searchMeta.abortController.signal.aborted) {
            const settled = await Promise.race(pending);
            pending.delete(searchPromises[settled.index]);
            
            if (settled.status === 'fulfilled') {
                const sourceResults = settled.results;
                if (sourceResults && sourceResults.length > 0) {
                    await this.processResultsBatch(searchId, sourceResults, allResults);
                }
            } else if (settled.status === 'rejected') {
                console.warn(`Search error in source ${settled.source}:`, settled.error.message || settled.error);
            }
        }
        
        // Mark search as complete
        searchMeta.completed = true;
        this.triggerCompleteCallbacks(searchId, allResults);
        
        // Clean up
        setTimeout(() => this.activeSearches.delete(searchId), 5000);
        
        return {
            results: allResults,
            searchTime: Date.now() - searchMeta.startTime,
            cancelled: searchMeta.abortController.signal.aborted
        };
    }
    
    async searchSourceWithTimeout(source, query, signal, timeout) {
        return Promise.race([
            this.searchProviders[source]?.search(query).then(results => {
                if (signal.aborted) return [];
                return results;
            }),
            new Promise((_, reject) => 
                setTimeout(() => reject(new Error(`${source} search timeout`)), timeout)
            )
        ]);
    }
    
    async processResultsBatch(searchId, results, allResults) {
        const searchMeta = this.activeSearches.get(searchId);
        if (!searchMeta || searchMeta.abortController.signal.aborted) return;
        
        // Process results in chunks to avoid blocking
        for (let i = 0; i < results.length; i += this.batchSize) {
            if (searchMeta.abortController.signal.aborted) break;
            
            const batch = results.slice(i, i + this.batchSize);
            allResults.push(...batch);
            searchMeta.resultsCount += batch.length;
            
            // Trigger progress callbacks
            this.triggerProgressCallbacks(searchId, batch, allResults);
            
            // Yield to browser for smooth UI
            await this.yieldToBrowser();
        }
    }
    
    // Debounced search for typing
    searchDebounced(query, sources, options) {
        clearTimeout(this.debounceTimer);
        
        return new Promise((resolve) => {
            this.debounceTimer = setTimeout(() => {
                const searchHandle = this.searchStreaming(query, sources, options);
                resolve(searchHandle);
            }, this.debounceTime);
        });
    }
    
    // Cancel search by ID
    cancelSearch(searchId) {
        const searchMeta = this.activeSearches.get(searchId);
        if (searchMeta) {
            searchMeta.abortController.abort();
            this.activeSearches.delete(searchId);
            this.searchCallbacks.delete(searchId);
            console.log(`Cancelled search: ${searchId}`);
        }
    }
    
    // Cancel searches with similar query
    cancelSimilarSearches(query) {
        const queryStart = query.substring(0, 3).toLowerCase();
        
        this.activeSearches.forEach((meta, id) => {
            if (meta.query.toLowerCase().startsWith(queryStart) && !meta.completed) {
                this.cancelSearch(id);
            }
        });
    }
    
    // Cancel all active searches
    cancelAllSearches() {
        this.activeSearches.forEach((_, id) => this.cancelSearch(id));
    }
    
    // Progress tracking
    addProgressCallback(searchId, callback) {
        if (!this.searchCallbacks.has(searchId)) {
            this.searchCallbacks.set(searchId, { progress: [], complete: [] });
        }
        this.searchCallbacks.get(searchId).progress.push(callback);
    }
    
    addCompleteCallback(searchId, callback) {
        if (!this.searchCallbacks.has(searchId)) {
            this.searchCallbacks.set(searchId, { progress: [], complete: [] });
        }
        this.searchCallbacks.get(searchId).complete.push(callback);
    }
    
    triggerProgressCallbacks(searchId, newResults, allResults) {
        const callbacks = this.searchCallbacks.get(searchId);
        if (callbacks && callbacks.progress) {
            callbacks.progress.forEach(cb => {
                try {
                    cb({
                        searchId,
                        newResults,
                        totalResults: allResults,
                        count: allResults.length
                    });
                } catch (error) {
                    console.error('Progress callback error:', error);
                }
            });
        }
    }
    
    triggerCompleteCallbacks(searchId, results) {
        const callbacks = this.searchCallbacks.get(searchId);
        if (callbacks && callbacks.complete) {
            callbacks.complete.forEach(cb => {
                try {
                    cb({
                        searchId,
                        results,
                        count: results.length
                    });
                } catch (error) {
                    console.error('Complete callback error:', error);
                }
            });
        }
        this.searchCallbacks.delete(searchId);
    }
    
    // Helper methods
    generateSearchId() {
        return `search_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    }
    
    async yieldToBrowser() {
        return new Promise(resolve => {
            if (typeof requestIdleCallback !== 'undefined') {
                requestIdleCallback(resolve);
            } else {
                setTimeout(resolve, 0);
            }
        });
    }
    
    // Get search statistics
    getSearchStats() {
        const stats = {
            active: 0,
            completed: 0,
            cancelled: 0,
            totalResults: 0
        };
        
        this.activeSearches.forEach(meta => {
            if (meta.completed) stats.completed++;
            else stats.active++;
            stats.totalResults += meta.resultsCount;
        });
        
        return stats;
    }
    
    // Parallel search with result merging
    async searchParallel(queries, sources = ['all']) {
        const searchHandles = queries.map(query => 
            this.searchStreaming(query, sources)
        );
        
        const allResults = [];
        const resultMap = new Map(); // Deduplicate by ID
        
        // Collect results as they stream in
        searchHandles.forEach(handle => {
            handle.onProgress(({ newResults }) => {
                newResults.forEach(result => {
                    if (!resultMap.has(result.id)) {
                        resultMap.set(result.id, result);
                        allResults.push(result);
                    }
                });
            });
        });
        
        // Wait for all to complete
        await Promise.all(searchHandles.map(h => h.promise));
        
        return allResults;
    }
}
