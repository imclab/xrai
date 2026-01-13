import { UnityProjectAnalyzer } from './analyzer/unityAnalyzer.js';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

async function analyzeExampleProjects() {
    const exampleProjects = [
        '/Users/jamestunick/Downloads/SplatVFX-main/URP',
        '/Users/jamestunick/Downloads/SplatVFX-main/VFX',
        '/Users/jamestunick/Downloads/AI_Knowledge_Base_Setup/SdfVfxSamples-master'
    ];

    for (const projectPath of exampleProjects) {
        console.log(`\n${'='.repeat(50)}`);
        console.log(`Analyzing: ${projectPath}`);
        console.log(`${'='.repeat(50)}\n`);

        try {
            const analyzer = new UnityProjectAnalyzer(projectPath);
            const data = await analyzer.analyze();
            
            console.log(`Project: ${data.name}`);
            console.log(`Unity Version: ${data.unityVersion || 'Unknown'}`);
            console.log(`Is Unity Project: ${data.isUnityProject}`);
            console.log(`\nAsset Summary:`);
            console.log(`- Scripts: ${data.scripts.length}`);
            console.log(`- Scenes: ${data.scenes.length}`);
            console.log(`- Prefabs: ${data.prefabs.length}`);
            console.log(`- Materials: ${data.materials.length}`);
            console.log(`- Shaders: ${data.shaders.length}`);
            console.log(`- Textures: ${data.textures?.length || 0}`);
            console.log(`- Models: ${data.models?.length || 0}`);
            
            console.log(`\nPackages: ${data.packages.length}`);
            data.packages.slice(0, 5).forEach(pkg => {
                console.log(`  - ${pkg.name} (${pkg.version})`);
            });
            
            console.log(`\nKey Scripts:`);
            data.scripts.filter(s => s.isMonoBehaviour).slice(0, 5).forEach(script => {
                console.log(`  - ${script.name} ${script.namespace ? `(${script.namespace})` : ''}`);
            });
            
        } catch (error) {
            console.error(`Error analyzing ${projectPath}: ${error.message}`);
        }
    }
}

if (import.meta.url === `file://${process.argv[1]}`) {
    analyzeExampleProjects();
}

export { UnityProjectAnalyzer };