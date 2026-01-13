import { UnityProjectAnalyzer } from './src/analyzer/unityAnalyzer.js';

async function testAnalyzer() {
    const projectPath = '/Users/jamestunick/Downloads/SplatVFX-main/URP';
    console.log(`Testing analyzer on: ${projectPath}\n`);
    
    const analyzer = new UnityProjectAnalyzer(projectPath);
    const data = await analyzer.analyze();
    
    console.log('Detected assets:');
    Object.entries(data).forEach(([key, value]) => {
        if (Array.isArray(value) && value.length > 0) {
            console.log(`\n${key}: ${value.length} items`);
            value.slice(0, 3).forEach(item => {
                console.log(`  - ${item.name || item}`);
            });
        }
    });
}

testAnalyzer();