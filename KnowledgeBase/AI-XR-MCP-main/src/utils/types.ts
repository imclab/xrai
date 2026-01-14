import z from 'zod'
import { CallToolRequestSchema } from '@modelcontextprotocol/sdk/types.js'

export type ToolHandlers = Record<string, (request: z.infer<typeof CallToolRequestSchema>) => Promise<{
  toolResult: {
    content: Array<{
      type: string;
      text: string;
    }>;
    isError?: boolean;
  };
}>>