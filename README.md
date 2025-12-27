# ForIT Claude Connector

Power Platform custom connector for Anthropic's Claude API. Enables Power Automate flows to interact with Claude for AI-powered automation.

## Features

- **Create Message** - Send messages to Claude and receive AI-generated responses
- **Count Tokens** - Estimate token usage before making requests
- **Message Batches** - Process up to 10,000 requests asynchronously with 50% cost savings
- **Multiple Models** - Support for Claude Opus 4, Sonnet 4, and Claude 3.5 models

## Prerequisites

1. Anthropic API key from [console.anthropic.com](https://console.anthropic.com)
2. Power Platform environment with custom connector permissions
3. GitHub repository secrets configured (for CI/CD)

## Setup

### 1. Get Anthropic API Key

1. Go to [console.anthropic.com](https://console.anthropic.com)
2. Create or select a project
3. Generate an API key
4. Save securely - you'll need this when creating a connection

### 2. Deploy the Connector

#### Option A: Manual Deployment

1. Go to [Power Automate](https://make.powerautomate.com)
2. Navigate to Data > Custom connectors
3. Click "New custom connector" > "Import an OpenAPI file"
4. Upload `claude-connector.json`
5. Configure the connector settings
6. Create and test a connection

#### Option B: Automated Deployment (CI/CD)

Configure GitHub repository secrets and variables:

**Secrets (per environment):**
- `POWER_PLATFORM_APP_ID` - Azure AD app registration ID
- `POWER_PLATFORM_CLIENT_SECRET` - Azure AD app secret
- `POWER_PLATFORM_TENANT_ID` - Azure AD tenant ID

**Variables (per environment):**
- `POWER_PLATFORM_URL` - Environment URL (e.g., `https://org.crm.dynamics.com`)
- `CONNECTOR_ID` - Connector GUID (after first deployment)

Then trigger the workflow:
```bash
gh workflow run "Deploy Connector to Power Platform" -f environment=development -f action=create
```

## Usage in Power Automate

### Basic Message

```
Action: Create Message
Model: claude-sonnet-4-20250514
Max Tokens: 1024
Messages: [{"role": "user", "content": "Hello, Claude!"}]
```

### With System Prompt

```
Action: Create Message
Model: claude-sonnet-4-20250514
Max Tokens: 2048
System Prompt: "You are a helpful assistant that responds in JSON format."
Messages: [{"role": "user", "content": "List 3 colors"}]
```

### Multi-turn Conversation

```
Messages: [
  {"role": "user", "content": "My name is Alice"},
  {"role": "assistant", "content": "Hello Alice! Nice to meet you."},
  {"role": "user", "content": "What's my name?"}
]
```

## Models Available

| Model | Best For |
|-------|----------|
| claude-opus-4-20250514 | Complex reasoning, analysis |
| claude-sonnet-4-20250514 | Balanced performance (recommended) |
| claude-3-5-sonnet-20241022 | Previous generation balanced |
| claude-3-5-haiku-20241022 | Fast, cost-effective |

## Rate Limits

Anthropic applies rate limits based on your usage tier:

| Tier | Requests/min | Tokens/min |
|------|-------------|------------|
| 1 | 50 | 40,000 |
| 2 | 1,000 | 80,000 |
| 3 | 2,000 | 160,000 |
| 4 | 4,000 | 400,000 |

## Error Handling

The connector returns structured error responses:

- `400` - Bad request (invalid parameters)
- `401` - Invalid API key
- `403` - Permission denied
- `429` - Rate limited (implement retry with backoff)
- `500` - Server error
- `529` - API overloaded (retry later)

## Development

### Validate Locally

```bash
npm install
npm test
```

### Project Structure

```
ForIT-Claude-Connector/
├── claude-connector.json    # OpenAPI 2.0 definition
├── apiProperties.json       # Power Platform properties
├── package.json
├── tests/
│   └── validate-connector.js
├── .github/
│   └── workflows/
│       ├── deploy-connector.yml
│       └── validate.yml
└── README.md
```

## Learnings Applied

This connector incorporates lessons from ForIT-Xero-Connector:

1. **No scripts/icons in PAC CLI** - These cause BlobNotFound errors; add manually if needed
2. **OpenAPI 2.0 required** - Power Platform doesn't support OpenAPI 3.0
3. **Error responses matter** - Include 400, 401, 429, 500 responses
4. **Multiple environments** - CI/CD supports dev, prod, and custom environments
5. **API key auth is simpler** - No OAuth redirect URLs to manage

## Next Steps

### 1. Create GitHub Repository

```bash
cd /Users/benjaminwesleythomas/GitProjects/ForIT-Claude-Connector
gh repo create ForITLLC/ForIT-Claude-Connector --public --source . --push
```

### 2. Configure GitHub Environments

Create these environments in GitHub repo settings: `development`, `production`, `forit-default`, `forit-portal`

**Secrets (per environment):**
| Secret | Description |
|--------|-------------|
| `POWER_PLATFORM_APP_ID` | Azure AD app registration ID |
| `POWER_PLATFORM_CLIENT_SECRET` | Azure AD app secret |
| `POWER_PLATFORM_TENANT_ID` | Azure AD tenant ID |

**Variables (per environment):**
| Variable | Example |
|----------|---------|
| `POWER_PLATFORM_URL` | `https://org.crm.dynamics.com` |
| `CONNECTOR_ID` | Set after first deployment |

### 3. Deploy to All Environments

```bash
gh workflow run "Deploy Connector to Power Platform" -f environment=all -f action=create
```

### 4. Get Connector IDs

After deployment, get the connector IDs from the workflow logs and update the `CONNECTOR_ID` variable in each environment.

### 5. Test Connection

1. Go to Power Automate > Data > Custom connectors
2. Find "ForIT Claude Connector"
3. Create a connection with your Anthropic API key
4. Test the "Create Message" action

## License

MIT

## Support

- [GitHub Issues](https://github.com/ForITLLC/ForIT-Claude-Connector/issues)
- [Anthropic Documentation](https://docs.anthropic.com)
