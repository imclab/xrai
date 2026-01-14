# MCP Configuration Files

This directory contains configuration files for various MCP clients that can use the 3D WebGL visualization tools.

## Claude Desktop Configuration

`claude-desktop.json` configures the tool for use with Claude Desktop applications. It defines:

- Tool names and descriptions
- UI presentation preferences
- Default settings for visualization parameters

## Windsurf Browser Configuration

`windsurf.json` configures the tool for integration with the Windsurf browser extension, including:

- Client/server identity information
- Tool descriptions and icons
- Default parameters
- Permission requirements
- UI presentation settings

## Cursor Editor Configuration

`cursor.json` configures the tool for use with the Cursor code editor, including:

- Server description and launch command
- Tool details and categorization
- Context-specific actions for different scenarios
- Server configuration options

## Usage

To use these configurations:

1. For Claude Desktop:
   - Place the configuration file in the Claude Desktop MCP tools directory
   - Restart Claude Desktop to see the new tool

2. For Windsurf Browser:
   - Import the configuration in the Windsurf extension settings
   - Enable the tool in the Windsurf AI menu

3. For Cursor:
   - Open Cursor settings
   - Go to the Features tab
   - Navigate to MCP Servers section
   - Click "Add Server"
   - Choose "Import from file" and select the cursor.json file

Each configuration is optimized for the specific client environment while providing access to the same underlying 3D visualization capabilities.