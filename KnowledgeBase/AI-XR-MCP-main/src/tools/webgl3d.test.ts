import { describe, it, expect, beforeAll } from 'vitest';
import { TestClient } from '../utils/TestClient.js';

describe('webgl-3d-visualization', () => {
    let client: TestClient;

    beforeAll(async () => {
        client = new TestClient();
    });

    it('should be available in tools list', async () => {
        const tools = await client.listTools();
        expect(tools).toContainEqual(
            expect.objectContaining({
                name: 'webgl-3d-visualization',
                description: 'Generate a 3D force-directed graph visualization for websites, GitHub repositories, or local file systems',
            })
        );
    });

    it('should validate input parameters', async () => {
        // Test empty source
        await expect(
            client.callTool('webgl-3d-visualization', { 
                source: '', 
                sourceType: 'website' 
            })
        ).rejects.toThrow('Source must not be empty');

        // Test invalid source type
        await expect(
            client.callTool('webgl-3d-visualization', { 
                source: 'https://example.com', 
                sourceType: 'invalid' 
            })
        ).rejects.toThrow('Invalid enum value');

        // Test invalid depth
        await expect(
            client.callTool('webgl-3d-visualization', { 
                source: 'https://example.com', 
                sourceType: 'website',
                depth: 10 
            })
        ).rejects.toThrow('Number must be less than or equal to 5');
    });

    it('should process website source', async () => {
        // This test uses a local test server for consistent results
        // For actual integration testing, use a real website
        const result = await client.callTool(
            'webgl-3d-visualization',
            { 
                source: 'example.com', 
                sourceType: 'website',
                depth: 1,
                layout: 'force',
                outputFormat: 'json'
            }
        );

        expect(result.toolResult.content[0].type).toBe('text');
        
        // Parse the JSON response
        const jsonData = JSON.parse(result.toolResult.content[0].text);
        
        // Verify structure
        expect(jsonData).toHaveProperty('nodes');
        expect(jsonData).toHaveProperty('links');
        expect(Array.isArray(jsonData.nodes)).toBe(true);
        expect(Array.isArray(jsonData.links)).toBe(true);
        
        // Should have at least a root node
        expect(jsonData.nodes.length).toBeGreaterThan(0);
        expect(jsonData.nodes.find((n: { id: string }) => n.id === 'root')).toBeDefined();
    });

    it('should process local directory source', async () => {
        const result = await client.callTool(
            'webgl-3d-visualization',
            { 
                source: process.cwd(), 
                sourceType: 'local',
                depth: 1,
                outputFormat: 'json'
            }
        );

        expect(result.toolResult.content[0].type).toBe('text');
        
        // Parse the JSON response
        const jsonData = JSON.parse(result.toolResult.content[0].text);
        
        // Verify structure
        expect(jsonData).toHaveProperty('nodes');
        expect(jsonData).toHaveProperty('links');
        expect(Array.isArray(jsonData.nodes)).toBe(true);
        expect(Array.isArray(jsonData.links)).toBe(true);
        
        // Should have at least a root node representing current directory
        expect(jsonData.nodes.length).toBeGreaterThan(0);
        expect(jsonData.nodes.find((n: { id: string }) => n.id === 'root')).toBeDefined();
    });

    it('should generate HTML visualization', async () => {
        const result = await client.callTool(
            'webgl-3d-visualization',
            { 
                source: process.cwd(), 
                sourceType: 'local',
                depth: 1,
                outputFormat: 'html'
            }
        );

        expect(result.toolResult.content[0].type).toBe('text');
        const htmlContent = result.toolResult.content[0].text;
        
        // Verify HTML structure
        expect(htmlContent).toContain('<!DOCTYPE html>');
        expect(htmlContent).toContain('<script src="https://unpkg.com/three"></script>');
        expect(htmlContent).toContain('<script src="https://unpkg.com/3d-force-graph"></script>');
        expect(htmlContent).toContain('const graphData =');
    });
});