import { Tool } from '@modelcontextprotocol/sdk/types.js'
import { log } from '../utils/helpers.js'
import { ToolHandlers } from '../utils/types.js'
import z from 'zod'
import * as fs from 'fs/promises'
import * as path from 'path'
import { JSDOM } from 'jsdom'
import { Octokit } from '@octokit/rest'

// WebGL 3D visualization tool definition
const WEBGL_3D_VISUALIZATION_TOOL: Tool = {
  name: 'webgl-3d-visualization',
  description: 'Generate a 3D force-directed graph visualization for websites, GitHub repositories, or local file systems',
  inputSchema: {
    type: 'object',
    properties: {
      source: {
        type: 'string',
        description: 'URL of website, GitHub repository, or path to local directory',
        minLength: 1
      },
      sourceType: {
        type: 'string',
        description: 'Type of source to visualize',
        enum: ['website', 'github', 'local']
      },
      depth: {
        type: 'integer',
        description: 'How deep to scan (for websites/directories)',
        default: 2,
        minimum: 1,
        maximum: 5
      },
      layout: {
        type: 'string',
        description: 'Layout algorithm to use',
        enum: ['force', 'radial', 'hierarchical'],
        default: 'force'
      },
      outputFormat: {
        type: 'string',
        description: 'Format of visualization output',
        enum: ['html', 'json', 'url'],
        default: 'html'
      }
    },
    required: ['source', 'sourceType']
  }
}

// Export all tools
export const WEBGL_3D_TOOLS = [WEBGL_3D_VISUALIZATION_TOOL]

// Zod schema for input validation
const inputSchema = z.object({
  source: z.string().min(1, 'Source must not be empty'),
  sourceType: z.enum(['website', 'github', 'local']),
  depth: z.number().int().min(1).max(5).default(2),
  layout: z.enum(['force', 'radial', 'hierarchical']).default('force'),
  outputFormat: z.enum(['html', 'json', 'url']).default('html')
})

// Type for graph data structure
type GraphData = {
  nodes: Array<{
    id: string
    name: string
    val: number
    group?: string
    color?: string
    path?: string
  }>
  links: Array<{
    source: string
    target: string
    value?: number
  }>
}

// Process website structure into graph data
async function processWebsite(url: string, depth: number): Promise<GraphData> {
  log(`Processing website: ${url}, depth: ${depth}`)
  
  // Ensure URL has protocol
  if (!url.startsWith('http')) {
    url = `https://${url}`
  }
  
  try {
    // Fetch main page
    const response = await fetch(url)
    const html = await response.text()
    
    // Parse DOM
    const dom = new JSDOM(html)
    const document = dom.window.document
    
    // Extract structure
    const graphData: GraphData = {
      nodes: [],
      links: []
    }
    
    // Add root node
    const rootId = 'root'
    graphData.nodes.push({
      id: rootId,
      name: url,
      val: 5,
      group: 'root',
      color: '#FF6B6B'
    })
    
    // Process HTML elements
    const mainElements = ['div', 'section', 'nav', 'header', 'footer', 'main', 'article']
    
    // Process each major element
    mainElements.forEach(tag => {
      const elements = document.querySelectorAll(tag)
      elements.forEach((el: Element, index: number) => {
        const id = `${tag}-${index}`
        const className = el.className || ''
        const name = `${tag}${className ? '.' + className : ''}`
        
        graphData.nodes.push({
          id,
          name,
          val: 2 + (el.childNodes.length / 10),
          group: tag,
          color: getColorForTag(tag)
        })
        
        // Link to root
        graphData.links.push({
          source: rootId,
          target: id,
          value: 1
        })
        
        // Process children if depth allows
        if (depth > 1) {
          processChildren(el, id, graphData, 1, depth)
        }
      })
    })
    
    return graphData
  } catch (error) {
    log('Error processing website:', error)
    throw new Error(`Failed to process website: ${error instanceof Error ? error.message : String(error)}`)
  }
}

// Helper function to process children elements
function processChildren(element: Element, parentId: string, graphData: GraphData, currentDepth: number, maxDepth: number) {
  if (currentDepth >= maxDepth) return
  
  Array.from(element.children).forEach((child, index) => {
    const tag = child.tagName.toLowerCase()
    const id = `${parentId}-${tag}-${index}`
    const className = child.className || ''
    const name = `${tag}${className ? '.' + className : ''}`
    
    graphData.nodes.push({
      id,
      name,
      val: 1 + (child.childNodes.length / 20),
      group: tag,
      color: getColorForTag(tag)
    })
    
    // Link to parent
    graphData.links.push({
      source: parentId,
      target: id,
      value: 1
    })
    
    // Process children
    processChildren(child, id, graphData, currentDepth + 1, maxDepth)
  })
}

// Process github repository into graph data
async function processGithubRepo(repoUrl: string): Promise<GraphData> {
  log(`Processing GitHub repo: ${repoUrl}`)
  
  // Extract owner and repo from URL
  const urlPattern = /github\.com\/([^\/]+)\/([^\/]+)/
  const match = repoUrl.match(urlPattern)
  
  if (!match) {
    throw new Error('Invalid GitHub repository URL')
  }
  
  const [, owner, repo] = match
  
  try {
    const octokit = new Octokit()
    
    // Fetch repository files
    const { data: filesData } = await octokit.repos.getContent({
      owner,
      repo,
      path: ''
    })
    
    // Fetch repository info
    const { data: repoData } = await octokit.repos.get({
      owner,
      repo
    })
    
    const graphData: GraphData = {
      nodes: [],
      links: []
    }
    
    // Add root node
    const rootId = 'root'
    graphData.nodes.push({
      id: rootId,
      name: repoData.name,
      val: 7,
      group: 'repo',
      color: '#61DAFB'
    })
    
    if (Array.isArray(filesData)) {
      // Process files and directories
      for (const item of filesData) {
        const id = item.path
        const isDir = item.type === 'dir'
        
        graphData.nodes.push({
          id,
          name: item.name,
          val: isDir ? 3 : 1,
          group: isDir ? 'directory' : getFileGroup(item.name),
          color: isDir ? '#4CAF50' : getColorForFile(item.name),
          path: item.path
        })
        
        // Link to root
        graphData.links.push({
          source: rootId,
          target: id,
          value: 1
        })
        
        // Recursively process directories
        if (isDir) {
          await processGithubDirectory(owner, repo, item.path, id, graphData)
        }
      }
    }
    
    return graphData
  } catch (error) {
    log('Error processing GitHub repo:', error)
    throw new Error(`Failed to process GitHub repository: ${error instanceof Error ? error.message : String(error)}`)
  }
}

// Helper function for processing github directories recursively
async function processGithubDirectory(owner: string, repo: string, path: string, parentId: string, graphData: GraphData) {
  try {
    const octokit = new Octokit()
    const { data: contents } = await octokit.repos.getContent({
      owner,
      repo,
      path
    })
    
    if (Array.isArray(contents)) {
      for (const item of contents) {
        const id = item.path
        const isDir = item.type === 'dir'
        
        graphData.nodes.push({
          id,
          name: item.name,
          val: isDir ? 3 : 1,
          group: isDir ? 'directory' : getFileGroup(item.name),
          color: isDir ? '#4CAF50' : getColorForFile(item.name),
          path: item.path
        })
        
        // Link to parent
        graphData.links.push({
          source: parentId,
          target: id,
          value: 1
        })
        
        // Only process one level of subdirectories to avoid API rate limits
        if (isDir && path.split('/').length < 3) {
          await processGithubDirectory(owner, repo, item.path, id, graphData)
        }
      }
    }
  } catch (error) {
    log(`Error processing GitHub directory ${path}:`, error)
    // Don't throw, just log error and continue
  }
}

// Process local directory into graph data
async function processLocalDirectory(dirPath: string, depth: number): Promise<GraphData> {
  log(`Processing local directory: ${dirPath}, depth: ${depth}`)
  
  try {
    const graphData: GraphData = {
      nodes: [],
      links: []
    }
    
    // Add root node
    const rootId = 'root'
    const rootName = path.basename(dirPath)
    graphData.nodes.push({
      id: rootId,
      name: rootName,
      val: 5,
      group: 'directory',
      color: '#4CAF50',
      path: dirPath
    })
    
    // Process directory
    await processDirectory(dirPath, rootId, graphData, 1, depth)
    
    return graphData
  } catch (error) {
    log('Error processing local directory:', error)
    throw new Error(`Failed to process directory: ${error instanceof Error ? error.message : String(error)}`)
  }
}

// Helper function to process directory recursively
async function processDirectory(dirPath: string, parentId: string, graphData: GraphData, currentDepth: number, maxDepth: number) {
  if (currentDepth > maxDepth) return
  
  try {
    const files = await fs.readdir(dirPath)
    
    for (const file of files) {
      const filePath = path.join(dirPath, file)
      const stats = await fs.stat(filePath)
      const isDir = stats.isDirectory()
      
      // Skip hidden files/directories
      if (file.startsWith('.')) continue
      
      const id = filePath
      
      graphData.nodes.push({
        id,
        name: file,
        val: isDir ? 3 : 1,
        group: isDir ? 'directory' : getFileGroup(file),
        color: isDir ? '#4CAF50' : getColorForFile(file),
        path: filePath
      })
      
      // Link to parent
      graphData.links.push({
        source: parentId,
        target: id,
        value: 1
      })
      
      // Process subdirectory
      if (isDir) {
        await processDirectory(filePath, id, graphData, currentDepth + 1, maxDepth)
      }
    }
  } catch (error) {
    log(`Error reading directory ${dirPath}:`, error)
    // Don't throw, just log error and continue
  }
}

// Helper function to get file group
function getFileGroup(filename: string): string {
  const ext = path.extname(filename).toLowerCase()
  
  if (['.js', '.ts', '.jsx', '.tsx'].includes(ext)) return 'script'
  if (['.html', '.htm'].includes(ext)) return 'html'
  if (['.css', '.scss', '.sass', '.less'].includes(ext)) return 'style'
  if (['.jpg', '.jpeg', '.png', '.gif', '.svg', '.webp'].includes(ext)) return 'image'
  if (['.json', '.yml', '.yaml', '.toml'].includes(ext)) return 'data'
  if (['.md', '.txt', '.pdf', '.doc', '.docx'].includes(ext)) return 'document'
  
  return 'file'
}

// Helper function to get colors for elements
function getColorForTag(tag: string): string {
  const colors: Record<string, string> = {
    div: '#4287f5',
    section: '#42c5f5',
    nav: '#f542a7',
    header: '#f5a742',
    footer: '#42f5b3',
    main: '#a142f5',
    article: '#f54242'
  }
  
  return colors[tag] || '#777777'
}

// Helper function to get colors for files
function getColorForFile(filename: string): string {
  const fileGroup = getFileGroup(filename)
  
  const colors: Record<string, string> = {
    script: '#F9E79F',
    html: '#FF9900',
    style: '#85C1E9',
    image: '#7DCEA0',
    data: '#F1948A',
    document: '#D2B4DE',
    file: '#AAB7B8'
  }
  
  return colors[fileGroup] || '#AAB7B8'
}

// Generate HTML with embedded 3D visualization
function generateVisualizationHtml(graphData: GraphData, layout: string): string {
  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>3D Graph Visualization</title>
  <style>
    body {
      margin: 0;
      padding: 0;
      font-family: Arial, sans-serif;
      overflow: hidden;
    }
    #graph-container {
      position: absolute;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: #111;
    }
    .info-panel {
      position: absolute;
      top: 10px;
      right: 10px;
      background: rgba(0, 0, 0, 0.7);
      color: white;
      padding: 15px;
      border-radius: 4px;
      font-size: 14px;
      max-width: 300px;
    }
    .controls {
      position: absolute;
      bottom: 10px;
      left: 10px;
      color: white;
      background: rgba(0, 0, 0, 0.7);
      padding: 10px;
      border-radius: 4px;
    }
  </style>
  <script src="https://unpkg.com/three"></script>
  <script src="https://unpkg.com/three-spritetext"></script>
  <script src="https://unpkg.com/3d-force-graph"></script>
</head>
<body>
  <div id="graph-container"></div>
  <div class="info-panel">
    <h3>Visualization Controls</h3>
    <p>
      <b>Rotate:</b> Left-click + drag<br>
      <b>Pan:</b> Right-click + drag<br>
      <b>Zoom:</b> Scroll / pinch<br>
      <b>Node info:</b> Click on node
    </p>
    <div id="node-info"></div>
  </div>
  <div class="controls">
    <button id="toggle-layout">Toggle Layout</button>
    <button id="toggle-labels">Toggle Labels</button>
    <button id="center-graph">Center View</button>
  </div>

  <script>
    // Graph data
    const graphData = ${JSON.stringify(graphData, null, 2)};
    
    // Setup visualization
    let showLabels = true;
    let currentLayout = '${layout}';
    
    // Initialize 3D force graph
    const Graph = ForceGraph3D()
      .backgroundColor('#111')
      .graphData(graphData)
      .nodeId('id')
      .nodeVal('val')
      .nodeLabel('name')
      .nodeColor('color')
      .nodeResolution(8)
      .linkWidth(0.5)
      .linkOpacity(0.6)
      .linkDirectionalParticles(2)
      .linkDirectionalParticleWidth(1.5)
      .linkDirectionalParticleSpeed(0.003)
      .onNodeClick(node => {
        // Display node info
        const infoPanel = document.getElementById('node-info');
        infoPanel.innerHTML = \`
          <h4>\${node.name}</h4>
          <p>
            Type: \${node.group || 'Unknown'}<br>
            \${node.path ? \`Path: \${node.path}\` : ''}
          </p>
        \`;
        
        // Highlight connected links
        graphData.links.forEach(link => {
          if (link.source.id === node.id || link.target.id === node.id) {
            link.__highlighted = true;
          } else {
            link.__highlighted = false;
          }
        });
        
        Graph.linkWidth(link => link.__highlighted ? 2 : 0.5)
             .linkColor(link => link.__highlighted ? '#FFFFFF' : '#666666')
             .nodeRelSize(8);
      });
    
    // Add to DOM
    Graph(document.getElementById('graph-container'));
    
    // Apply initial layout
    applyLayout(currentLayout);
    
    // Event Listeners
    document.getElementById('toggle-layout').addEventListener('click', () => {
      currentLayout = currentLayout === 'force' 
        ? 'radial' 
        : currentLayout === 'radial' 
          ? 'hierarchical' 
          : 'force';
      
      applyLayout(currentLayout);
    });
    
    document.getElementById('toggle-labels').addEventListener('click', () => {
      showLabels = !showLabels;
      
      if (showLabels) {
        Graph.nodeThreeObject(null)
             .nodeLabel('name');
      } else {
        Graph.nodeLabel(null)
             .nodeThreeObject(node => {
               // Return basic sphere
               return undefined;
             });
      }
    });
    
    document.getElementById('center-graph').addEventListener('click', () => {
      Graph.zoomToFit(1000, 50);
    });
    
    // Helper function to apply different layouts
    function applyLayout(layout) {
      Graph.cooldownTime(10000);
      
      switch(layout) {
        case 'force':
          Graph.d3Force('link')
               .distance(link => 30);
          Graph.d3Force('charge')
               .strength(-120);
          break;
        
        case 'radial':
          Graph.d3Force('radial', d3.forceRadial()
            .radius(node => {
              // Group nodes by type
              if (node.group === 'root') return 0;
              if (node.group === 'directory') return 100;
              return 200;
            })
            .strength(1)
          );
          break;
        
        case 'hierarchical':
          // Position nodes in a tree layout
          const rootNode = graphData.nodes.find(node => node.id === 'root');
          if (rootNode) {
            positionNodesHierarchically(rootNode, graphData.links);
          }
          break;
      }
      
      // Update info
      document.querySelector('.info-panel h3').textContent = 
        'Layout: ' + layout.charAt(0).toUpperCase() + layout.slice(1);
    }
    
    // Helper for hierarchical layout
    function positionNodesHierarchically(rootNode, links) {
      // Reset positions
      graphData.nodes.forEach(node => {
        node.fx = null;
        node.fy = null;
        node.fz = null;
      });
      
      // Set root position
      rootNode.fx = 0;
      rootNode.fy = 0;
      rootNode.fz = 0;
      
      // Simple BFS to position nodes
      const levels = {};
      const queue = [rootNode.id];
      let currentLevel = 0;
      levels[rootNode.id] = 0;
      
      while (queue.length > 0) {
        const levelSize = queue.length;
        const nodesInCurrentLevel = [];
        
        for (let i = 0; i < levelSize; i++) {
          const nodeId = queue.shift();
          const node = graphData.nodes.find(n => n.id === nodeId);
          if (!node) continue;
          
          nodesInCurrentLevel.push(node);
          
          // Find children
          links.forEach(link => {
            const sourceId = typeof link.source === 'object' ? link.source.id : link.source;
            const targetId = typeof link.target === 'object' ? link.target.id : link.target;
            
            if (sourceId === nodeId && !levels.hasOwnProperty(targetId)) {
              queue.push(targetId);
              levels[targetId] = currentLevel + 1;
            }
          });
        }
        
        // Position nodes in current level in a circle
        const radius = 100 * (currentLevel + 1);
        const angleStep = (2 * Math.PI) / nodesInCurrentLevel.length;
        
        nodesInCurrentLevel.forEach((node, idx) => {
          const angle = idx * angleStep;
          node.fx = radius * Math.cos(angle);
          node.fy = radius * Math.sin(angle);
          node.fz = -currentLevel * 100; // Negative to extend forward from camera
        });
        
        currentLevel++;
      }
      
      Graph.numDimensions(3) // Set to 3D
           .forceEngine('d3');
    }
    
    // Set initial zoom
    setTimeout(() => {
      Graph.zoomToFit(1000, 50);
    }, 1000);
  </script>
</body>
</html>`;
}

// Main handler function implementation
async function handleWebgl3dVisualization(
  source: string,
  sourceType: string,
  depth: number,
  layout: string,
  outputFormat: string
): Promise<string> {
  log('Generating 3D visualization for:', source)
  
  let graphData: GraphData
  
  // Process different source types
  switch (sourceType) {
    case 'website':
      graphData = await processWebsite(source, depth)
      break
    
    case 'github':
      graphData = await processGithubRepo(source)
      break
    
    case 'local':
      graphData = await processLocalDirectory(source, depth)
      break
    
    default:
      throw new Error(`Unsupported source type: ${sourceType}`)
  }
  
  // Generate output based on format
  switch (outputFormat) {
    case 'html':
      return generateVisualizationHtml(graphData, layout)
    
    case 'json':
      return JSON.stringify(graphData, null, 2)
    
    case 'url':
      // For simplicity, we'd typically save file and generate URL
      // But for this example, we just create a data URL
      const htmlContent = generateVisualizationHtml(graphData, layout)
      const base64Content = Buffer.from(htmlContent).toString('base64')
      return `data:text/html;base64,${base64Content}`
    
    default:
      throw new Error(`Unsupported output format: ${outputFormat}`)
  }
}

// Export handlers
export const WEBGL_3D_HANDLERS: ToolHandlers = {
  'webgl-3d-visualization': async (request) => {
    try {
      const args = request.params.arguments
      const validatedInput = inputSchema.parse(args)
      
      const result = await handleWebgl3dVisualization(
        validatedInput.source,
        validatedInput.sourceType,
        validatedInput.depth,
        validatedInput.layout,
        validatedInput.outputFormat
      )
      
      return {
        toolResult: {
          content: [{ type: 'text', text: result }],
        },
      }
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error)
      throw new Error(`Failed to generate 3D visualization: ${errorMessage}`)
    }
  }
}