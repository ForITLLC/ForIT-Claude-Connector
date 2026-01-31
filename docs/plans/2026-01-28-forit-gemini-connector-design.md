# ForIT Gemini Connector - Design

## Summary

A Power Platform custom connector for Google's Gemini API, following the same architecture as the existing ForIT Claude Connector. Part of the "ForIT AI Connectors" family -- separate connectors per provider, shared patterns and branding.

## Architecture

```
Connection Creation:
    User provides ForIT License Key + Gemini API Key
            |
            v
    ai.forit.io/license/validate  -->  Valid? Create connection
                                       Invalid? Reject

Runtime (all requests):
    Power Automate Flow
            |
            v
    ForIT Gemini Connector
    host: generativelanguage.googleapis.com
            |
            v
    Google Gemini API (direct, no proxy)
```

License is validated once at connection creation. All subsequent API calls go directly to Google -- no per-request proxy overhead.

## Connector Structure

```
ForIT-Gemini-Connector/           (new repo)
├── gemini-connector.json         # OpenAPI 2.0 definition
├── apiProperties.json            # Power Platform properties + auth
├── script.csx                    # Transform prompt <-> contents/parts
├── package.json                  # Validation tests
├── tests/
│   └── validate-connector.js
├── .github/workflows/
│   ├── deploy-connector.yml      # CI/CD to 4 environments
│   └── validate.yml              # PR validation
└── README.md
```

## Authentication

Two parameters at connection creation:

1. **ForIT License Key** -- validated against `ai.forit.io/license/validate` at connection time
2. **Gemini API Key** -- passed as `?key=` query parameter on all requests to Google

The ForIT License Key is checked once during connection creation via a `testConnection` policy or custom validation endpoint. The Gemini API Key is used on every request.

## API Mapping

### Gemini API Details

| Property | Value |
|----------|-------|
| Host | `generativelanguage.googleapis.com` |
| Base Path | `/v1beta` |
| Auth | API key as query parameter (`?key=`) |
| Content Type | `application/json` |

### Key Differences from Claude

| | Claude | Gemini |
|---|---|---|
| Auth | `x-api-key` header | `?key=` query param |
| Endpoint | `POST /v1/messages` | `POST /v1beta/models/{model}:generateContent` |
| Model location | In request body | In URL path |
| Messages format | `messages: [{role, content}]` | `contents: [{role, parts: [{text}]}]` |
| System prompt | `system` field in body | `systemInstruction` object |
| Response text | `content[].text` | `candidates[0].content.parts[0].text` |
| Token usage | `usage.input_tokens` | `usageMetadata.promptTokenCount` |

## Actions

### Action 1: Ask Gemini (Simple)

**Operation ID:** `AskGemini`
**Visibility:** Important (shown by default)
**Endpoint:** `POST /v1beta/models/{model}:generateContent`

**Inputs:**
- **Model** (dropdown, required): `gemini-2.5-flash`, `gemini-2.0-flash`
- **Prompt** (string, required): User's question
- **Output Format** (advanced, optional): Text (default) or JSON

**Outputs:**
- **Response** (string): The text response
- **Model** (string, advanced): Model used
- **Usage** (object, advanced): `promptTokenCount`, `candidatesTokenCount`

**script.csx transforms:**
- Request: `prompt` string --> `contents: [{role: "user", parts: [{text: prompt}]}]`
- Response: `candidates[0].content.parts[0].text` --> flat `response` string

### Action 2: Generate Content (Advanced)

**Operation ID:** `GenerateContent`
**Visibility:** Advanced
**Endpoint:** `POST /v1beta/models/{model}:generateContent`

**Inputs:**
- **Model** (dropdown, required): `gemini-2.5-flash`, `gemini-2.0-flash`
- **Contents** (array, required): `[{role, parts: [{text}]}]`
- **System Instruction** (string, optional): System prompt text
- **Temperature** (number, optional): 0-2
- **Max Output Tokens** (integer, optional)
- **Top P** (number, optional): 0-1
- **Top K** (integer, optional)
- **Safety Settings** (array, optional): Content filtering thresholds
- **Response MIME Type** (string, optional): `text/plain` or `application/json`

**Outputs:**
- Full Gemini API response (candidates, usageMetadata, modelVersion)

No script.csx transformation -- passthrough to API.

## Models

| Model | Description | Default |
|-------|-------------|---------|
| `gemini-2.5-flash` | Latest, fast, thinking capable | Yes |
| `gemini-2.0-flash` | Previous generation, proven | No |

## apiProperties.json Key Points

- `connectionParameters`: Two secure strings (ForIT License Key, Gemini API Key)
- `script`: `script.csx`
- `scriptOperations`: `["AskGemini"]`
- Policy to set API key as query parameter (not header)
- `testConnection` endpoint pointing to ForIT SaaS license validation

## Deployment

Same as Claude connector -- GitHub Actions deploys to 4 environments:
- development
- production
- forit-default
- forit-portal

## Licensing Flow

1. User gets a ForIT License Key from forit.io
2. User gets a Gemini API Key from aistudio.google.com
3. In Power Automate, user adds "ForIT Gemini Connector"
4. Connection creation prompts for both keys
5. Connector validates ForIT License Key against `ai.forit.io/license/validate`
6. If valid, connection is created and all future requests go direct to Google
7. If license expires, user must recreate connection

## Decisions Made

- **Licensing model**: One-time purchase for "ForIT AI Connectors" bundle. Single license key works for all ForIT AI connectors (Claude, Gemini, future providers). No subscription, no expiry, no revocation complexity.
- **License validation**: Simple lookup at connection creation -- "is this key valid?" No expiry dates or renewal logic.
- **Repo structure**: Monorepo. Rename/restructure to `ForIT-AI-Connectors` with `connectors/claude/` and `connectors/gemini/` folders. Shared CI/CD and test infrastructure.
- **Branding**: Match provider brand colors. Claude stays tan (`#D4A27F`), Gemini uses blue (`#4285F4`). Different icons per provider for easy visual distinction.

## ForIT SaaS Endpoint

```
POST ai.forit.io/license/validate
Content-Type: application/json

{ "key": "FORIT-XXXXX" }

Response (valid):
{ "valid": true }

Response (invalid):
{ "valid": false, "error": "Invalid license key" }
```

## Next Steps

1. Create `ai.forit.io/license/validate` endpoint on ForIT SaaS
2. Restructure repo to monorepo (`ForIT-AI-Connectors`)
3. Move existing Claude connector to `connectors/claude/`
4. Build Gemini connector in `connectors/gemini/`
5. Update CI/CD to handle multi-connector deploys
6. Add license key parameter to both connectors' connection flow
7. Test end-to-end with license validation
