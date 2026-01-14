# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build/Development Commands
- `npm run build` - Build TypeScript to JavaScript
- `npm run dev` - Run with tsx for development
- `npm run start` - Run the built application
- `npm run lint` - Run ESLint on TypeScript files
- `npm test` - Run all tests with vitest
- `npm test -- src/tools/example.test.ts` - Run a specific test file
- `npm test -- -t "should process valid input"` - Run tests matching description

## Code Style Guidelines
- **Imports**: Use ES Modules with .js extension in import paths, prefer named exports
- **Types**: Use TypeScript strict mode, explicit return types, Zod for validation
- **Naming**: camelCase for variables/functions, PascalCase for types/interfaces, kebab-case for files
- **Error Handling**: Use try/catch blocks, extract messages with instanceof Error check
- **Tool Structure**: Define schema, processing function, and handler function separately
- **Testing**: Use describe/it blocks with explicit expectations
- **Exports**: Group exports at the end of files when possible
- **Formatting**: 2-space indentation, single quotes for strings

## Architecture Pattern
Follow the MCP server pattern with tool definitions and handlers in separate files under src/tools/.
Use TestClient for testing tools in isolation.