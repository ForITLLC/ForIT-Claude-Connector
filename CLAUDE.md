# ForIT Claude Connector

Power Platform custom connector for Anthropic's Claude API.

## Project Structure

```
ForIT-Claude-Connector/
├── claude-connector.json    # OpenAPI 2.0 definition (required for Power Platform)
├── apiProperties.json       # Power Platform properties & API key auth config
├── package.json            # Node.js for validation tests
├── tests/
│   └── validate-connector.js
├── .github/workflows/
│   ├── deploy-connector.yml  # CI/CD to 4 environments
│   └── validate.yml          # PR validation
└── README.md
```

## Key Learnings (from ForIT-Xero-Connector)

1. **No --icon-file in PAC CLI** - Including --icon-file causes BlobNotFound errors
2. **Icons must be set manually** - Create the connector, then set icon via Power Platform portal. The base64 icon in apiProperties.json is NOT automatically uploaded to blob storage by PAC CLI.
3. **OpenAPI 2.0 required** - Power Platform doesn't support OpenAPI 3.0
4. **Include error responses** - Always add 400, 401, 429, 500 responses to operations
5. **API key auth is simpler** - No OAuth redirect URLs to manage (unlike Xero)

## Deployment

```bash
gh workflow run "Deploy Connector to Power Platform" -f environment=all -f action=create
```

## Authentication

Uses API key authentication (simpler than OAuth):
- User provides Anthropic API key from console.anthropic.com
- Key is passed in `x-api-key` header to all requests

## Models

- claude-opus-4-5-20251101 (most capable - default)
- claude-sonnet-4-20250514 (balanced)
- claude-opus-4-20250514 (complex reasoning)
