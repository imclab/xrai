import { promises as fs } from 'fs';
import path from 'path';

async function generateSparseProjectData() {
    try {
        // Read the full analysis results
        const fullData = JSON.parse(
            await fs.readFile('analysis-results.json', 'utf8')
        );
        
        // Create sparse representations for each project
        const sparseProjects = fullData.map(project => {
            // Calculate total assets
            const totalAssets = Object.values(project.stats).reduce((sum, val) => sum + val, 0);
            
            // Determine project type based on dominant assets
            let projectType = 'general';
            if (project.stats.vfx > project.stats.scripts) {
                projectType = 'vfx-focused';
            } else if (project.stats.scripts > 30) {
                projectType = 'code-heavy';
            } else if (project.stats.prefabs > 20) {
                projectType = 'prefab-based';
            } else if (project.stats.materials > 30) {
                projectType = 'visual-heavy';
            }
            
            // Create sparse representation
            return {
                id: project.name.toLowerCase().replace(/[^a-z0-9]/g, '-'),
                name: project.name,
                unity: project.unityVersion,
                type: projectType,
                size: totalAssets,
                highlights: {
                    scripts: project.stats.scripts,
                    vfx: project.stats.vfx,
                    scenes: project.stats.scenes
                },
                packages: project.topPackages.length,
                complexity: calculateComplexity(project.stats)
            };
        });
        
        // Create visualization-friendly structure
        const visualizationData = {
            projects: sparseProjects,
            categories: {
                byType: groupByType(sparseProjects),
                bySize: groupBySize(sparseProjects),
                byUnityVersion: groupByUnityVersion(sparseProjects)
            },
            stats: {
                totalProjects: sparseProjects.length,
                averageSize: Math.round(sparseProjects.reduce((sum, p) => sum + p.size, 0) / sparseProjects.length),
                types: [...new Set(sparseProjects.map(p => p.type))],
                unityVersions: [...new Set(sparseProjects.map(p => p.unity))]
            }
        };
        
        // Save sparse data
        await fs.writeFile(
            'sparse-projects.json',
            JSON.stringify(visualizationData, null, 2)
        );
        
        // Create individual project files
        for (const project of sparseProjects) {
            const projectData = {
                ...project,
                connections: findConnections(project, sparseProjects),
                visualHints: {
                    primaryColor: getProjectColor(project),
                    radius: Math.min(50 + project.size * 0.5, 200),
                    orbitSpeed: 0.001 + (project.complexity * 0.0001),
                    glowIntensity: project.type === 'vfx-focused' ? 0.6 : 0.3
                }
            };
            
            await fs.writeFile(
                `sparse-data/${project.id}.json`,
                JSON.stringify(projectData, null, 2)
            );
        }
        
        console.log('Sparse JSON representations generated:');
        console.log(`- Main file: sparse-projects.json`);
        console.log(`- Individual files: sparse-data/*.json`);
        console.log(`\nProject Summary:`);
        sparseProjects.forEach(p => {
            console.log(`  ${p.name}: ${p.size} assets (${p.type})`);
        });
        
    } catch (error) {
        console.error('Error generating sparse data:', error);
    }
}

function calculateComplexity(stats) {
    // Simple complexity score based on asset diversity and count
    const factors = {
        scripts: stats.scripts * 2,
        vfx: stats.vfx * 3,
        shaders: stats.shaders * 2.5,
        materials: stats.materials * 1,
        prefabs: stats.prefabs * 1.5
    };
    
    const totalScore = Object.values(factors).reduce((sum, val) => sum + val, 0);
    return Math.min(Math.round(totalScore / 10), 100);
}

function groupByType(projects) {
    const groups = {};
    projects.forEach(p => {
        if (!groups[p.type]) groups[p.type] = [];
        groups[p.type].push(p.id);
    });
    return groups;
}

function groupBySize(projects) {
    const groups = {
        small: [],
        medium: [],
        large: []
    };
    
    projects.forEach(p => {
        if (p.size < 50) groups.small.push(p.id);
        else if (p.size < 200) groups.medium.push(p.id);
        else groups.large.push(p.id);
    });
    
    return groups;
}

function groupByUnityVersion(projects) {
    const groups = {};
    projects.forEach(p => {
        const majorVersion = p.unity.split('.')[0];
        if (!groups[majorVersion]) groups[majorVersion] = [];
        groups[majorVersion].push(p.id);
    });
    return groups;
}

function findConnections(project, allProjects) {
    // Find projects with similar characteristics
    const connections = [];
    
    allProjects.forEach(other => {
        if (other.id === project.id) return;
        
        let similarity = 0;
        
        // Same type
        if (other.type === project.type) similarity += 30;
        
        // Similar size
        const sizeDiff = Math.abs(other.size - project.size);
        if (sizeDiff < 50) similarity += 20;
        
        // Similar Unity version
        if (other.unity.split('.')[0] === project.unity.split('.')[0]) similarity += 10;
        
        // Similar complexity
        const complexityDiff = Math.abs(other.complexity - project.complexity);
        if (complexityDiff < 20) similarity += 15;
        
        if (similarity > 40) {
            connections.push({
                id: other.id,
                strength: similarity / 100
            });
        }
    });
    
    return connections;
}

function getProjectColor(project) {
    const colors = {
        'vfx-focused': '#00BCD4',
        'code-heavy': '#4CAF50',
        'prefab-based': '#FF9800',
        'visual-heavy': '#9C27B0',
        'general': '#2196F3'
    };
    
    return colors[project.type] || '#2196F3';
}

// Create sparse-data directory
await fs.mkdir('sparse-data', { recursive: true });

// Run the generator
generateSparseProjectData();