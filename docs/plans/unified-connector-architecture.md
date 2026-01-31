# ForIT Unified AI Connector Architecture

## Vision

Single Power Platform connector that provides access to multiple AI providers (Claude, Gemini, OpenAI) through the ForIT SaaS proxy at `ai.forit.io`.

## Benefits

1. **One Connection** - Users connect once with ForIT license key, access all providers
2. **Centralized API Keys** - ForIT manages provider API keys, not end users
3. **License Control** - Validate and meter usage at the proxy layer
4. **Consistent Interface** - Unified request/response format across providers
5. **Easy Updates** - Add new providers without updating connector

## Architecture

```
┌─────────────────────┐     ┌─────────────────────┐     ┌─────────────────────┐
│   Power Automate    │     │    ForIT Proxy      │     │   AI Providers      │
│                     │     │   ai.forit.io       │     │                     │
│  ┌───────────────┐  │     │  ┌───────────────┐  │     │  ┌───────────────┐  │
│  │ ForIT AI      │──┼────▶│  │ License       │  │     │  │ Anthropic     │  │
│  │ Connector     │  │     │  │ Validation    │  │     │  │ (Claude)      │  │
│  └───────────────┘  │     │  └───────┬───────┘  │     │  └───────────────┘  │
│                     │     │          │          │     │                     │
│  Connection:        │     │  ┌───────▼───────┐  │     │  ┌───────────────┐  │
│  - ForIT License    │     │  │ Route to      │──┼────▶│  │ Google        │  │
│                     │     │  │ Provider      │  │     │  │ (Gemini)      │  │
│                     │     │  └───────────────┘  │     │  └───────────────┘  │
│                     │     │                     │     │                     │
│                     │     │  API keys stored    │     │  ┌───────────────┐  │
│                     │     │  server-side        │     │  │ OpenAI        │  │
│                     │     │                     │     │  │ (GPT-4)       │  │
└─────────────────────┘     └─────────────────────┘     └───────────────────┘
```

## Connector Operations

### Simple Operations (Transformed)

| Operation | Description |
|-----------|-------------|
| **Ask** | Universal prompt → any provider, auto-selects best model |
| **AskClaude** | Prompt → Claude (specific provider) |
| **AskGemini** | Prompt → Gemini (specific provider) |
| **AskOpenAI** | Prompt → OpenAI (specific provider) |

### Advanced Operations (Pass-through)

| Operation | Description |
|-----------|-------------|
| **CreateMessage** | Full Claude API access |
| **GenerateContent** | Full Gemini API access |
| **ChatCompletion** | Full OpenAI API access |

## ForIT Proxy Endpoints

Base URL: `https://ai.forit.io/api`

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/ask` | POST | Universal ask (provider auto-selected) |
| `/claude/messages` | POST | Proxy to Claude API |
| `/gemini/generate` | POST | Proxy to Gemini API |
| `/openai/chat` | POST | Proxy to OpenAI API |
| `/license/validate` | POST | Validate license key |
| `/usage` | GET | Get usage statistics |

## Request Format (Universal)

```json
{
  "prompt": "What is the capital of France?",
  "provider": "claude",          // optional: claude, gemini, openai, auto
  "model": "claude-sonnet-4-5",  // optional: specific model
  "max_tokens": 4096,            // optional
  "attachment": "base64...",     // optional
  "attachment_type": "image/png" // optional
}
```

## Response Format (Normalized)

```json
{
  "response": "The capital of France is Paris.",
  "provider": "claude",
  "model": "claude-sonnet-4-5-20250929",
  "usage": {
    "prompt_tokens": 15,
    "completion_tokens": 8,
    "total_tokens": 23
  }
}
```

## Authentication

### Connection Parameters

| Parameter | Description |
|-----------|-------------|
| `forit_license_key` | ForIT license key (required) |
| `organization_id` | Optional org ID for multi-tenant |

### License Tiers

| Tier | Features |
|------|----------|
| **Free** | 100 requests/month, Claude only |
| **Pro** | 10,000 requests/month, all providers |
| **Enterprise** | Unlimited, custom models, SLA |

## Implementation Phases

### Phase 1: Proxy Infrastructure
- [ ] Create ai.forit.io Azure Function App
- [ ] Implement license validation
- [ ] Implement Claude proxy endpoint
- [ ] Implement Gemini proxy endpoint

### Phase 2: Unified Connector
- [ ] Create forit-ai-connector.json (OpenAPI 2.0)
- [ ] Create apiProperties.json with license auth
- [ ] Create script.csx for request normalization
- [ ] Deploy to all environments

### Phase 3: Commercialization
- [ ] License key management portal
- [ ] Usage tracking and billing
- [ ] Self-service signup

## Directory Structure

```
forit-AI-Connector/
├── connectors/
│   ├── claude/              # Direct Claude connector (free)
│   ├── gemini/              # Direct Gemini connector (free)
│   └── forit-ai/            # Unified connector (licensed)
│       ├── forit-ai-connector.json
│       ├── apiProperties.json
│       ├── script.csx
│       └── icon.png
├── proxy/                   # ForIT SaaS proxy
│   ├── src/
│   │   ├── functions/
│   │   │   ├── ask.ts
│   │   │   ├── claude.ts
│   │   │   ├── gemini.ts
│   │   │   └── license.ts
│   │   └── services/
│   │       ├── license.ts
│   │       └── providers.ts
│   └── host.json
└── docs/
    └── plans/
```

## Migration Path

1. Keep existing direct connectors (claude, gemini) for users who want to use their own API keys
2. Unified connector for commercial customers who want managed experience
3. Both can coexist - different use cases

## Open Questions

1. **API Key Storage**: Azure Key Vault or database with encryption?
2. **Rate Limiting**: Per-license or per-request?
3. **Caching**: Cache common responses to reduce costs?
4. **Fallback**: If Claude is down, auto-switch to Gemini?
