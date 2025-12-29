# ForIT Claude Connector

Power Platform custom connector for Anthropic's Claude API.

## Acceptance Criteria

### Ask Claude Action - SIMPLE User Experience
- **Model**: Dropdown selector (claude-sonnet-4-5, claude-opus-4-5)
- **Prompt**: Single text field (NOT an array) - user types their question
- **Response**: Single text output (NOT an array) - user gets the answer
- **Optional**: Enable Thinking toggle + Thinking Budget for extended reasoning
- **script.csx transforms**: prompt → messages array (request), content array → response text (response)

### Environments - ONE Connector Per Environment
| Environment | Requirement |
|-------------|-------------|
| development | ONE Claude connector, connection creation works |
| production | ONE Claude connector, connection creation works |
| forit-default | ONE Claude connector, connection creation works |
| forit-portal | ONE Claude connector, connection creation works |

**NO DUPLICATES. Delete old connectors before deploying new ones.**

### Critical Requirements
1. **apiProperties.json** MUST have both:
   - `"script": "script.csx"`
   - `"scriptOperations": ["AskClaude"]`
2. **BlobNotFound Error**: If connector was created WITHOUT script, it CANNOT be updated with script. Must DELETE and CREATE fresh.
3. **Connection Creation**: After deploying connector, users must be able to create a new connection with their API key.
4. **GitHub Variables**: Each environment needs `CONNECTOR_ID` variable updated after fresh CREATE.

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

- claude-sonnet-4-5-20250929 (balanced - default)
- claude-opus-4-5-20251101 (most capable)

## Troubleshooting

### "Entity 'connector' With Id = X Does Not Exist" on Connection Creation
**Cause**: Old connector ID cached in browser/API Hub after connector was deleted and recreated.
**Fix**:
1. Hard refresh browser (Cmd+Shift+R / Ctrl+Shift+R)
2. Clear browser cache for make.powerapps.com
3. Try in incognito/private window
4. Wait 5-10 minutes for API Hub cache to clear

### Duplicate Connectors in Environment
**Cause**: Old manually-created connector exists alongside new deployed connector.
**IMPORTANT**: Connectors exist in TWO places - must check BOTH:
1. **Dataverse**: `m365 pp dataverse table row list --tableName connector`
2. **API Hub**: `m365 pa connector list --environmentName "ENV_ID"`

**Fix** - Delete from appropriate location:
```bash
# If in Dataverse:
m365 pp dataverse table row remove --environmentName "ENV_NAME" --tableName connector --id "CONNECTOR_ID" --force

# If in API Hub only:
m365 request --url "https://api.powerapps.com/providers/Microsoft.PowerApps/apis/CONNECTOR_NAME?api-version=2016-11-01&\$filter=environment%20eq%20'ENV_ID'" --method delete
```

### Current Connector IDs (verified 2025-12-29)
| Environment | Connector ID | Status |
|-------------|--------------|--------|
| development | `14d76ada-e7e4-f011-8544-6045bd060382` | 1 connector |
| production | `9b96b96a-e8e4-f011-8543-6045bd054af0` | 1 connector |
| forit-portal | `843934e2-e7e4-f011-8406-7ced8d3b5021` | 1 connector |
| forit-default | `81e87038-ece4-f011-8544-6045bdebaeb6` | 1 connector |
