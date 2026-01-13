export class Visualizer {
  constructor(container, data) {
    this.container = container;
    this.data = data;
    this.mode = 'treegraph';
    this.nodes = [];
    this.links = [];
  }

  generateTreeGraph() {
    const root = {
      id: 'root',
      name: this.data.name,
      type: 'project',
      size: 100,
      children: [],
      position: { x: 0, y: 0, z: 0 }
    };

    const categories = [
      { key: 'scripts', color: '#4CAF50', type: 'script' },
      { key: 'scenes', color: '#2196F3', type: 'scene' },
      { key: 'prefabs', color: '#FF9800', type: 'prefab' },
      { key: 'materials', color: '#9C27B0', type: 'material' },
      { key: 'shaders', color: '#F44336', type: 'shader' },
      { key: 'textures', color: '#795548', type: 'texture' },
      { key: 'models', color: '#607D8B', type: 'model' },
      { key: 'vfx', color: '#00BCD4', type: 'vfx' },
      { key: 'animations', color: '#FFC107', type: 'animation' },
      { key: 'audio', color: '#8BC34A', type: 'audio' },
      { key: 'renderPipeline', color: '#3F51B5', type: 'pipeline' }
    ];

    categories.forEach((category, index) => {
      if (this.data[category.key] && this.data[category.key].length > 0) {
        const categoryNode = {
          id: category.key,
          name: category.key.charAt(0).toUpperCase() + category.key.slice(1),
          type: 'category',
          color: category.color,
          size: 80,
          children: [],
          position: this.calculatePosition(index, categories.length, 200)
        };

        this.data[category.key].forEach((item, itemIndex) => {
          const itemNode = {
            id: `${category.key}-${itemIndex}`,
            name: item.name,
            type: category.type,
            path: item.path,
            color: category.color,
            size: 30,
            position: this.calculatePosition(itemIndex, this.data[category.key].length, 100)
          };
          categoryNode.children.push(itemNode);
        });

        root.children.push(categoryNode);
      }
    });

    return root;
  }

  generateNodeGraph() {
    const nodes = [];
    const links = [];
    
    nodes.push({
      id: 'root',
      name: this.data.name,
      type: 'project',
      size: 120,
      x: 0,
      y: 0,
      z: 0
    });

    this.data.packages.forEach((pkg, index) => {
      const angle = (index / this.data.packages.length) * Math.PI * 2;
      const radius = 300;
      
      nodes.push({
        id: `package-${index}`,
        name: pkg.name,
        type: pkg.type === 'unity' ? 'unity-package' : 'third-party-package',
        version: pkg.version,
        size: 60,
        x: Math.cos(angle) * radius,
        y: 0,
        z: Math.sin(angle) * radius
      });
      
      links.push({
        source: 'root',
        target: `package-${index}`,
        type: 'dependency'
      });
    });

    this.data.scripts.forEach((script, index) => {
      if (script.isMonoBehaviour || script.isScriptableObject) {
        const angle = (index / this.data.scripts.length) * Math.PI * 2;
        const radius = 500;
        
        nodes.push({
          id: `script-${index}`,
          name: script.name,
          type: script.isMonoBehaviour ? 'monobehaviour' : 'scriptableobject',
          namespace: script.namespace,
          size: 40,
          x: Math.cos(angle) * radius,
          y: (script.isMonoBehaviour ? 50 : -50),
          z: Math.sin(angle) * radius
        });
      }
    });

    return { nodes, links };
  }

  generateFlowChart() {
    const nodes = [];
    const links = [];
    
    const sceneNodes = this.data.scenes.map((scene, index) => ({
      id: `scene-${index}`,
      name: scene.name,
      type: 'scene',
      level: 0,
      x: index * 200,
      y: 0,
      z: 0
    }));
    
    nodes.push(...sceneNodes);

    const scriptsByScene = this.groupScriptsByLocation();
    
    Object.entries(scriptsByScene).forEach(([sceneName, scripts], sceneIndex) => {
      scripts.forEach((script, scriptIndex) => {
        const node = {
          id: `script-${sceneIndex}-${scriptIndex}`,
          name: script.name,
          type: 'script',
          level: 1,
          x: sceneIndex * 200,
          y: 100 + scriptIndex * 50,
          z: 0
        };
        nodes.push(node);
        
        const sceneNode = sceneNodes.find(n => n.name === sceneName);
        if (sceneNode) {
          links.push({
            source: sceneNode.id,
            target: node.id,
            type: 'contains'
          });
        }
      });
    });

    return { nodes, links };
  }

  calculatePosition(index, total, radius) {
    const angle = (index / total) * Math.PI * 2;
    return {
      x: Math.cos(angle) * radius,
      y: 0,
      z: Math.sin(angle) * radius
    };
  }

  groupScriptsByLocation() {
    const grouped = {};
    
    this.data.scripts.forEach(script => {
      const pathParts = script.path.split('/');
      const location = pathParts.length > 2 ? pathParts[1] : 'Root';
      
      if (!grouped[location]) {
        grouped[location] = [];
      }
      grouped[location].push(script);
    });
    
    return grouped;
  }

  setMode(mode) {
    this.mode = mode;
    this.update();
  }

  update() {
    switch (this.mode) {
      case 'treegraph':
        return this.generateTreeGraph();
      case 'nodegraph':
        return this.generateNodeGraph();
      case 'flowchart':
        return this.generateFlowChart();
      default:
        return this.generateTreeGraph();
    }
  }
}