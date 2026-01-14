# MCP Tool Shortcuts

This directory contains shortcut configurations for MCP tools that enable quick access through command-line style invocations.

## WebGL 3D Visualization Shortcut

The `/create3d` shortcut provides quick access to the 3D visualization tool:

```
/create3d [source] [sourceType] [layout] [depth]
```

### Parameters

- `source`: URL of website, GitHub repository, or path to local directory
- `sourceType`: Type of source to visualize (`website`, `github`, or `local`)
- `layout`: Layout algorithm to use (`force`, `radial`, or `hierarchical`)
- `depth`: How deep to scan (1-5)

### Examples

```
# Visualize a website
/create3d https://example.com website

# Visualize a GitHub repository
/create3d https://github.com/username/repo github

# Visualize a local directory with hierarchical layout
/create3d /path/to/directory local hierarchical 3
```

## Installation

To install shortcuts in MCP-enabled applications:

1. Copy the shortcut JSON files to the application's MCP shortcuts directory
2. Restart the application or refresh the MCP configuration
3. Use the shortcut command in the application's command interface

## Creating New Shortcuts

To create a new shortcut:

1. Create a JSON file with the format shown in the existing shortcuts
2. Define the shortcut name, tool mapping, and parameter mappings
3. Add examples and help text for user guidance
4. Place the file in this directory and register it with your MCP application