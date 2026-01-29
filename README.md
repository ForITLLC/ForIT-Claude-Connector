# ForIT AI Connectors

Power Platform custom connectors for AI APIs.

## Available Connectors

| Connector | Provider | Status |
|-----------|----------|--------|
| [Claude](connectors/claude/) | Anthropic | Production |
| [Gemini](connectors/gemini/) | Google | In Development |

## Deployment

Deploy a specific connector to an environment:

```bash
gh workflow run "Deploy Connector to Power Platform" \
  -f connector=claude \
  -f environment=development \
  -f action=update
```

Deploy to all environments:

```bash
gh workflow run "Deploy Connector to Power Platform" \
  -f connector=claude \
  -f environment=all \
  -f action=update
```
