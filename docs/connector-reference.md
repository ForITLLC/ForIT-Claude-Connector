# ForIT AI Connectors Reference

## Project Structure

**Repository:** ForITLLC/ForIT-Claude-Connector (GitHub)

```
forit-AI-Connector/
├── connectors/
│   ├── claude/
│   │   ├── claude-connector.json      # OpenAPI 2.0 definition
│   │   ├── apiProperties.json         # Power Platform config (unlicensed)
│   │   ├── apiProperties.licensed.json # With ForIT license validation
│   │   ├── script.csx                 # C# request/response transformation
│   │   └── tests/validate-connector.js
│   └── gemini/
│       ├── gemini-connector.json
│       ├── apiProperties.json
│       ├── apiProperties.licensed.json
│       ├── script.csx
│       └── tests/validate-connector.js
├── .github/workflows/
│   └── deploy-connector.yml           # CI/CD deployment
└── docs/
```

---

## Claude Connector

| Property | Value |
|----------|-------|
| Host | api.anthropic.com |
| BasePath | /v1 |
| Auth | API key via `x-api-key` header |
| Models | claude-sonnet-4-5, claude-opus-4-5 |

### Operations

- **AskClaude** (simple) - prompt, model, optional attachment, thinking mode
- **CreateMessage** (advanced) - full API access with tools, system prompts
- **CountTokens** - estimate token usage
- **CreateMessageBatch** - async batch processing

### Attachment Support

image/png, image/jpeg, image/gif, image/webp

### Connector IDs

| Environment | Connector ID |
|-------------|--------------|
| development | 14d76ada-e7e4-f011-8544-6045bd060382 |
| production | 9b96b96a-e8e4-f011-8543-6045bd054af0 |
| forit-portal | 843934e2-e7e4-f011-8406-7ced8d3b5021 |
| forit-default | 81e87038-ece4-f011-8544-6045bdebaeb6 |
| pivot | fc33393b-a4fd-f011-8407-0022486dd59e |

---

## Gemini Connector

| Property | Value |
|----------|-------|
| Host | generativelanguage.googleapis.com |
| BasePath | /v1beta |
| Auth | API key via `x-goog-api-key` header |
| Models | gemini-2.5-flash, gemini-2.0-flash |

### Operations

- **AskGemini** (simple) - prompt, model, optional attachment, output format
- **GenerateContent** (advanced) - full API with system instructions, safety settings

### Attachment Support

image/png, image/jpeg, image/gif, image/webp, application/pdf

### Connector IDs

| Environment | Connector ID |
|-------------|--------------|
| development | 928b0523-25fd-f011-8406-00224805fa58 |
| production | 8a03641e-25fd-f011-8406-7c1e528d1c98 |
| forit-default | cf0b1e18-25fd-f011-8407-002248321a0c |
| forit-portal | 2d8ccb1f-25fd-f011-8406-00224808b6d4 |
| pivot | a50a3441-a4fd-f011-8407-0022486dd59e |

---

## Deployment

### Workflow Inputs

| Input | Options |
|-------|---------|
| connector | claude, gemini |
| environment | development, production, forit-default, forit-portal, pivot, all |
| action | create, update |
| licensed | true, false (toggles ForIT license requirement) |

### Pivot Environment

| Property | Value |
|----------|-------|
| Tenant | flypivot.com (Air Georgian) |
| Tenant ID | 4343169d-f104-4976-832a-1dcd7bfc0df4 |
| Environment | Air Georgian (default) (Upgrade) |
| Environment URL | https://org388fdbfe.crm3.dynamics.com/ |

### Great North Publishing (App Registration)

| Property | Value |
|----------|-------|
| Name | Great North Publishing |
| App ID | 3f46777b-bc53-4c93-9951-9bd172d27a12 |
| Purpose | Deploy connectors to Pivot via GitHub Actions |
| Role | System Administrator (Application User) |

### GitHub Environment Secrets (pivot)

- POWER_PLATFORM_APP_ID
- POWER_PLATFORM_TENANT_ID
- POWER_PLATFORM_CLIENT_SECRET

### GitHub Environment Variables (pivot)

- POWER_PLATFORM_URL
- CONNECTOR_ID
- GEMINI_CONNECTOR_ID

---

## Notes

- Power Platform scripts cannot use `new HttpClient()` - external API calls not allowed
- Licensed builds add `forit_license_key` parameter to connection
- License validation endpoint (not implemented): https://ai.forit.io/license/validate
