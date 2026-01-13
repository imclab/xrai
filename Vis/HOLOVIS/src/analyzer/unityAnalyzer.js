import { promises as fs } from 'fs';
import path from 'path';
import { glob } from 'glob';
import yaml from 'yaml';

export class UnityProjectAnalyzer {
  constructor(projectPath) {
    this.projectPath = projectPath;
    this.projectData = {
      name: path.basename(projectPath),
      path: projectPath,
      assets: [],
      scripts: [],
      packages: [],
      scenes: [],
      prefabs: [],
      materials: [],
      shaders: [],
      dependencies: [],
      structure: {}
    };
  }

  async analyze() {
    console.log(`Analyzing Unity project: ${this.projectPath}`);
    
    await this.detectProjectType();
    await this.analyzeAssets();
    await this.analyzeScripts();
    await this.analyzePackages();
    await this.analyzeDependencies();
    await this.buildStructure();
    
    return this.projectData;
  }

  async detectProjectType() {
    const projectSettingsPath = path.join(this.projectPath, 'ProjectSettings/ProjectSettings.asset');
    try {
      await fs.access(projectSettingsPath);
      this.projectData.isUnityProject = true;
      
      const versionPath = path.join(this.projectPath, 'ProjectSettings/ProjectVersion.txt');
      if (await this.fileExists(versionPath)) {
        const versionContent = await fs.readFile(versionPath, 'utf8');
        const match = versionContent.match(/m_EditorVersion: (.+)/);
        if (match) {
          this.projectData.unityVersion = match[1];
        }
      }
    } catch {
      this.projectData.isUnityProject = false;
    }
  }

  async analyzeAssets() {
    const assetsPath = path.join(this.projectPath, 'Assets');
    
    const assetPatterns = {
      scenes: '**/*.unity',
      prefabs: '**/*.prefab',
      materials: '**/*.mat',
      shaders: '**/*.shader',
      textures: '**/*.{png,jpg,jpeg,tga,psd,tif,tiff}',
      models: '**/*.{fbx,obj,dae,3ds,blend}',
      audio: '**/*.{wav,mp3,ogg,aiff,m4a}',
      animations: '**/*.{anim,controller}',
      vfx: '**/*.vfx',
      renderPipeline: '**/*.asset'
    };

    for (const [type, pattern] of Object.entries(assetPatterns)) {
      try {
        const files = await glob(pattern, { 
          cwd: assetsPath,
          nodir: true,
          absolute: false
        });
        
        if (type === 'renderPipeline') {
          this.projectData[type] = files
            .filter(file => file.includes('URP') || file.includes('HDRP') || file.includes('Pipeline'))
            .map(file => ({
              name: path.basename(file),
              path: path.join('Assets', file),
              type: type
            }));
        } else {
          this.projectData[type] = files.map(file => ({
            name: path.basename(file),
            path: path.join('Assets', file),
            type: type
          }));
        }
      } catch (error) {
        console.log(`Error scanning ${type}: ${error.message}`);
        this.projectData[type] = [];
      }
    }
  }

  async analyzeScripts() {
    const scriptsPattern = path.join(this.projectPath, 'Assets/**/*.cs');
    const scriptFiles = await glob(scriptsPattern);
    
    for (const scriptPath of scriptFiles) {
      const content = await fs.readFile(scriptPath, 'utf8');
      const relativePath = path.relative(this.projectPath, scriptPath);
      
      const scriptInfo = {
        name: path.basename(scriptPath),
        path: relativePath,
        namespace: this.extractNamespace(content),
        classes: this.extractClasses(content),
        dependencies: this.extractDependencies(content),
        isMonoBehaviour: content.includes('MonoBehaviour'),
        isScriptableObject: content.includes('ScriptableObject')
      };
      
      this.projectData.scripts.push(scriptInfo);
    }
  }

  async analyzePackages() {
    const manifestPath = path.join(this.projectPath, 'Packages/manifest.json');
    
    try {
      const manifestContent = await fs.readFile(manifestPath, 'utf8');
      const manifest = JSON.parse(manifestContent);
      
      this.projectData.packages = Object.entries(manifest.dependencies || {}).map(([name, version]) => ({
        name,
        version,
        type: name.startsWith('com.unity') ? 'unity' : 'third-party'
      }));
    } catch (error) {
      console.log('No package manifest found');
    }
  }

  async analyzeDependencies() {
    const asmdefFiles = await glob('**/*.asmdef', {
      cwd: this.projectPath,
      nodir: true
    });
    
    for (const asmdefPath of asmdefFiles) {
      try {
        const content = await fs.readFile(path.join(this.projectPath, asmdefPath), 'utf8');
        const asmdef = JSON.parse(content);
        
        this.projectData.dependencies.push({
          name: asmdef.name,
          path: asmdefPath,
          references: asmdef.references || [],
          includePlatforms: asmdef.includePlatforms || [],
          excludePlatforms: asmdef.excludePlatforms || []
        });
      } catch (error) {
        console.log(`Error parsing asmdef: ${asmdefPath}`);
      }
    }
  }

  async buildStructure() {
    const assetsPath = path.join(this.projectPath, 'Assets');
    this.projectData.structure = await this.buildDirectoryStructure(assetsPath, 'Assets');
  }

  async buildDirectoryStructure(dirPath, relativePath = '') {
    const structure = {
      name: path.basename(dirPath),
      path: relativePath,
      type: 'directory',
      children: []
    };

    try {
      const entries = await fs.readdir(dirPath, { withFileTypes: true });
      
      for (const entry of entries) {
        const fullPath = path.join(dirPath, entry.name);
        const childRelativePath = path.join(relativePath, entry.name);
        
        if (entry.isDirectory() && !entry.name.startsWith('.')) {
          structure.children.push(
            await this.buildDirectoryStructure(fullPath, childRelativePath)
          );
        } else if (entry.isFile()) {
          structure.children.push({
            name: entry.name,
            path: childRelativePath,
            type: 'file',
            extension: path.extname(entry.name)
          });
        }
      }
    } catch (error) {
      console.log(`Error reading directory: ${dirPath}`);
    }

    return structure;
  }

  extractNamespace(content) {
    const match = content.match(/namespace\s+([^\s{]+)/);
    return match ? match[1] : null;
  }

  extractClasses(content) {
    const classRegex = /(?:public|private|internal)?\s*(?:partial\s+)?(?:class|struct|interface|enum)\s+(\w+)/g;
    const classes = [];
    let match;
    
    while ((match = classRegex.exec(content)) !== null) {
      classes.push(match[1]);
    }
    
    return classes;
  }

  extractDependencies(content) {
    const usingRegex = /using\s+([^;]+);/g;
    const dependencies = [];
    let match;
    
    while ((match = usingRegex.exec(content)) !== null) {
      dependencies.push(match[1].trim());
    }
    
    return dependencies;
  }

  async fileExists(filePath) {
    try {
      await fs.access(filePath);
      return true;
    } catch {
      return false;
    }
  }
}