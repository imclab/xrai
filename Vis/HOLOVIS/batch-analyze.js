import { UnityProjectAnalyzer } from './src/analyzer/unityAnalyzer.js';
import { promises as fs } from 'fs';
import path from 'path';

async function findUnityProjects(basePath, maxDepth = 3) {
    const projects = [];
    
    async function searchDir(dirPath, depth) {
        if (depth > maxDepth) return;
        
        try {
            const entries = await fs.readdir(dirPath, { withFileTypes: true });
            
            for (const entry of entries) {
                if (entry.isDirectory() && !entry.name.startsWith('.')) {
                    const fullPath = path.join(dirPath, entry.name);
                    
                    // Check if this is a Unity project
                    const projectSettingsPath = path.join(fullPath, 'ProjectSettings');
                    try {
                        await fs.access(projectSettingsPath);
                        projects.push(fullPath);
                    } catch {
                        // Not a Unity project, search subdirectories
                        await searchDir(fullPath, depth + 1);
                    }
                }
            }
        } catch (error) {
            // Skip directories we can't read
        }
    }
    
    await searchDir(basePath, 0);
    return projects;
}

async function batchAnalyze() {
    console.log('Searching for Unity projects...\n');
    
    const searchPaths = [
        '/Users/jamestunick/Downloads/SplatVFX-main',
        '/Users/jamestunick/Downloads/AI_Knowledge_Base_Setup'
    ];
    
    let allProjects = [];
    
    for (const searchPath of searchPaths) {
        const projects = await findUnityProjects(searchPath);
        allProjects = allProjects.concat(projects);
    }
    
    console.log(`Found ${allProjects.length} Unity projects\n`);
    
    const results = [];
    
    for (const projectPath of allProjects.slice(0, 10)) { // Analyze first 10 projects
        console.log(`${'='.repeat(60)}`);
        console.log(`Analyzing: ${projectPath}`);
        console.log(`${'='.repeat(60)}`);
        
        try {
            const analyzer = new UnityProjectAnalyzer(projectPath);
            const data = await analyzer.analyze();
            
            const summary = {
                name: data.name,
                path: projectPath,
                unityVersion: data.unityVersion || 'Unknown',
                stats: {
                    scripts: data.scripts?.length || 0,
                    scenes: data.scenes?.length || 0,
                    prefabs: data.prefabs?.length || 0,
                    materials: data.materials?.length || 0,
                    vfx: data.vfx?.length || 0,
                    shaders: data.shaders?.length || 0,
                    packages: data.packages?.length || 0,
                    models: data.models?.length || 0,
                    textures: data.textures?.length || 0
                },
                topPackages: data.packages?.slice(0, 3).map(p => p.name) || []
            };
            
            results.push(summary);
            
            console.log(`\nProject: ${summary.name}`);
            console.log(`Unity Version: ${summary.unityVersion}`);
            console.log('\nAsset Summary:');
            Object.entries(summary.stats).forEach(([key, value]) => {
                if (value > 0) {
                    console.log(`  ${key}: ${value}`);
                }
            });
            
            if (summary.topPackages.length > 0) {
                console.log('\nTop Packages:');
                summary.topPackages.forEach(pkg => {
                    console.log(`  - ${pkg}`);
                });
            }
            
            console.log('\n');
        } catch (error) {
            console.error(`Error analyzing ${projectPath}: ${error.message}\n`);
        }
    }
    
    // Save results
    await fs.writeFile(
        'analysis-results.json',
        JSON.stringify(results, null, 2)
    );
    
    console.log(`\nAnalysis complete! Results saved to analysis-results.json`);
    console.log(`\nProjects with most assets:`);
    
    results
        .sort((a, b) => {
            const totalA = Object.values(a.stats).reduce((sum, val) => sum + val, 0);
            const totalB = Object.values(b.stats).reduce((sum, val) => sum + val, 0);
            return totalB - totalA;
        })
        .slice(0, 5)
        .forEach(project => {
            const total = Object.values(project.stats).reduce((sum, val) => sum + val, 0);
            console.log(`  ${project.name}: ${total} assets`);
        });
}

batchAnalyze();