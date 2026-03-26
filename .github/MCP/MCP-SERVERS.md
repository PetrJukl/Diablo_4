# MCP Servers for Migration

Source of truth on this PC: `C:\Users\JuklP\.copilot\mcp-config.json`

## 1) User-configured MCP servers (copy exactly)

```json
{
  "mcpServers": {
    "microsoft-learn": {
      "type": "http",
      "url": "https://learn.microsoft.com/api/mcp"
    },
    "context7": {
      "type": "http",
      "url": "https://mcp.context7.com/mcp",
      "headers": {
        "CONTEXT7_API_KEY": "${CONTEXT7_API_KEY}"
      },
      "tools": [
        "query-docs",
        "resolve-library-id"
      ]
    },
    "markitdown": {
      "type": "stdio",
      "command": "C:\\Users\\JuklP\\AppData\\Local\\Programs\\Python\\Python312\\Scripts\\markitdown-mcp.exe",
      "args": []
    }
  }
}
```

## 2) Built-in GitHub MCP server

`github-mcp-server` is built into GitHub Copilot CLI by default and is not listed in `mcp-config.json`.

## 3) Context7 runtime note (Node.js vs HTTP)

- Current setup on this PC uses `context7` as `type: "http"` to `https://mcp.context7.com/mcp`.
- In this HTTP mode, local Node.js is not required for Context7 itself.
- If you switch Context7 to a local `stdio`/npm-based server, Node.js is required.

## 4) Migration steps to another PC

1. Install GitHub Copilot CLI and sign in.
2. Copy the `mcpServers` block above into `C:\Users\<USER>\.copilot\mcp-config.json` on the target PC.
3. Set environment variable `CONTEXT7_API_KEY` on the target PC.
4. Install MarkItDown MCP provider so `markitdown-mcp.exe` exists (this PC has `markitdown-mcp` version `0.0.1a4`).
5. If Python path differs, update `markitdown.command` to the correct executable path.

## 5) Quick validation checklist

- `microsoft-learn` connects (HTTP endpoint reachable).
- `context7` connects and can run `resolve-library-id`.
- `markitdown` starts via configured `command`.
- GitHub tools are available in Copilot CLI (built-in GitHub MCP).
