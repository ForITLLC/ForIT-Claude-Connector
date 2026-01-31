# ForIT AI Connectors - Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Restructure the Claude connector into a monorepo and add a Gemini connector with shared CI/CD and license validation.

**Architecture:** Monorepo with `connectors/claude/` and `connectors/gemini/` folders. Shared GitHub Actions workflow deploys either connector to any of 4 Power Platform environments. License validation at connection creation via existing ForIT SaaS endpoint.

**Tech Stack:** OpenAPI 2.0, C# script.csx, Power Platform CLI, GitHub Actions

---

## Task 1: Create Monorepo Structure

**Files:**
- Create: `connectors/claude/` (directory)
- Create: `connectors/gemini/` (directory)
- Create: `shared/` (directory for future shared utilities)

**Step 1: Create the directory structure**

```bash
mkdir -p connectors/claude connectors/gemini shared
```

**Step 2: Verify structure exists**

Run: `ls -la connectors/`
Expected: `claude/` and `gemini/` directories

**Step 3: Commit**

```bash
git add connectors shared
git commit -m "chore: create monorepo directory structure

Generated with [Claude Code](https://claude.ai/code)
via [Happy](https://happy.engineering)

Co-Authored-By: Claude <noreply@anthropic.com>
Co-Authored-By: Happy <yesreply@happy.engineering>"
```

---

## Task 2: Move Claude Connector Files

**Files:**
- Move: `claude-connector.json` → `connectors/claude/claude-connector.json`
- Move: `apiProperties.json` → `connectors/claude/apiProperties.json`
- Move: `script.csx` → `connectors/claude/script.csx`
- Move: `icon.png` → `connectors/claude/icon.png`

**Step 1: Move the connector files**

```bash
git mv claude-connector.json connectors/claude/
git mv apiProperties.json connectors/claude/
git mv script.csx connectors/claude/
git mv icon.png connectors/claude/
```

**Step 2: Verify files moved**

Run: `ls connectors/claude/`
Expected: `apiProperties.json  claude-connector.json  icon.png  script.csx`

**Step 3: Commit**

```bash
git add -A
git commit -m "refactor: move Claude connector to connectors/claude/

Generated with [Claude Code](https://claude.ai/code)
via [Happy](https://happy.engineering)

Co-Authored-By: Claude <noreply@anthropic.com>
Co-Authored-By: Happy <yesreply@happy.engineering>"
```

---

## Task 3: Move Tests to Claude Connector

**Files:**
- Move: `tests/validate-connector.js` → `connectors/claude/tests/validate-connector.js`
- Move: `package.json` → `connectors/claude/package.json`
- Move: `package-lock.json` → `connectors/claude/package-lock.json`

**Step 1: Create test directory and move files**

```bash
mkdir -p connectors/claude/tests
git mv tests/validate-connector.js connectors/claude/tests/
git mv package.json connectors/claude/
git mv package-lock.json connectors/claude/
rmdir tests
```

**Step 2: Verify files moved**

Run: `ls connectors/claude/`
Expected: includes `package.json`, `tests/`

**Step 3: Commit**

```bash
git add -A
git commit -m "refactor: move Claude tests to connectors/claude/

Generated with [Claude Code](https://claude.ai/code)
via [Happy](https://happy.engineering)

Co-Authored-By: Claude <noreply@anthropic.com>
Co-Authored-By: Happy <yesreply@happy.engineering>"
```

---

## Task 4: Update GitHub Actions for Monorepo

**Files:**
- Modify: `.github/workflows/deploy-connector.yml`
- Modify: `.github/workflows/validate.yml`

**Step 1: Update deploy workflow to support connector selection**

Replace `.github/workflows/deploy-connector.yml` with:

```yaml
name: Deploy Connector to Power Platform

on:
  workflow_dispatch:
    inputs:
      connector:
        description: 'Connector to deploy'
        required: true
        default: 'claude'
        type: choice
        options:
          - claude
          - gemini
      environment:
        description: 'Target environment'
        required: true
        default: 'development'
        type: choice
        options:
          - development
          - production
          - forit-default
          - forit-portal
          - all
      action:
        description: 'Deployment action'
        required: true
        default: 'update'
        type: choice
        options:
          - create
          - update

  push:
    branches: [main, master]
    paths:
      - 'connectors/**'

jobs:
  detect-changes:
    name: Detect Changed Connectors
    runs-on: ubuntu-latest
    outputs:
      claude_changed: ${{ steps.changes.outputs.claude }}
      gemini_changed: ${{ steps.changes.outputs.gemini }}
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 2

      - name: Check for changes
        id: changes
        run: |
          if git diff --name-only HEAD~1 HEAD | grep -q '^connectors/claude/'; then
            echo "claude=true" >> $GITHUB_OUTPUT
          else
            echo "claude=false" >> $GITHUB_OUTPUT
          fi
          if git diff --name-only HEAD~1 HEAD | grep -q '^connectors/gemini/'; then
            echo "gemini=true" >> $GITHUB_OUTPUT
          else
            echo "gemini=false" >> $GITHUB_OUTPUT
          fi

  validate:
    name: Validate ${{ matrix.connector }} Connector
    runs-on: ubuntu-latest
    needs: detect-changes
    strategy:
      matrix:
        connector: [claude, gemini]
        exclude:
          - connector: ${{ github.event_name == 'push' && needs.detect-changes.outputs.claude_changed != 'true' && 'claude' || 'none' }}
          - connector: ${{ github.event_name == 'push' && needs.detect-changes.outputs.gemini_changed != 'true' && 'gemini' || 'none' }}
    steps:
      - uses: actions/checkout@v4

      - name: Check connector exists
        id: check
        run: |
          if [ -d "connectors/${{ matrix.connector }}" ]; then
            echo "exists=true" >> $GITHUB_OUTPUT
          else
            echo "exists=false" >> $GITHUB_OUTPUT
          fi

      - name: Setup Node.js
        if: steps.check.outputs.exists == 'true'
        uses: actions/setup-node@v4
        with:
          node-version: '20'

      - name: Install and validate
        if: steps.check.outputs.exists == 'true'
        working-directory: connectors/${{ matrix.connector }}
        run: |
          if [ -f "package.json" ]; then
            npm ci
            npm test
          fi

      - name: Validate JSON syntax
        if: steps.check.outputs.exists == 'true'
        working-directory: connectors/${{ matrix.connector }}
        run: |
          for f in *-connector.json; do
            if [ -f "$f" ]; then
              node -e "JSON.parse(require('fs').readFileSync('$f'))"
              echo "$f: Valid JSON"
            fi
          done

  deploy:
    name: Deploy ${{ github.event.inputs.connector || 'claude' }} to ${{ matrix.environment }}
    runs-on: ubuntu-latest
    needs: validate
    if: github.event_name == 'workflow_dispatch'
    strategy:
      matrix:
        environment: ${{ github.event.inputs.environment == 'all' && fromJson('["development", "production", "forit-default", "forit-portal"]') || fromJson(format('["{0}"]', github.event.inputs.environment)) }}
    environment:
      name: ${{ matrix.environment }}
      url: ${{ vars.POWER_PLATFORM_URL }}

    steps:
      - uses: actions/checkout@v4

      - name: Set connector paths
        id: paths
        run: |
          CONNECTOR="${{ github.event.inputs.connector }}"
          echo "dir=connectors/${CONNECTOR}" >> $GITHUB_OUTPUT
          echo "definition=connectors/${CONNECTOR}/${CONNECTOR}-connector.json" >> $GITHUB_OUTPUT
          echo "properties=connectors/${CONNECTOR}/apiProperties.json" >> $GITHUB_OUTPUT
          echo "script=connectors/${CONNECTOR}/script.csx" >> $GITHUB_OUTPUT

      - name: Install Power Platform CLI
        uses: microsoft/powerplatform-actions/actions-install@v1

      - name: Authenticate
        run: |
          $POWERPLATFORMTOOLS_PACPATH auth create \
            --name deploy \
            --applicationId ${{ secrets.POWER_PLATFORM_APP_ID }} \
            --clientSecret "${{ secrets.POWER_PLATFORM_CLIENT_SECRET }}" \
            --tenant ${{ secrets.POWER_PLATFORM_TENANT_ID }} \
            --accept-cleartext-caching

      - name: Get Connector ID
        id: connector-id
        run: |
          CONNECTOR="${{ github.event.inputs.connector }}"
          CONNECTOR_VAR="${CONNECTOR^^}_CONNECTOR_ID"
          echo "id=${{ vars[env.CONNECTOR_VAR] || vars.CONNECTOR_ID }}" >> $GITHUB_OUTPUT
        env:
          CONNECTOR_VAR: ${{ github.event.inputs.connector }}_CONNECTOR_ID

      - name: Deploy Connector
        run: |
          if [ "${{ github.event.inputs.action }}" == "create" ]; then
            $POWERPLATFORMTOOLS_PACPATH connector create \
              --api-definition-file ${{ steps.paths.outputs.definition }} \
              --api-properties-file ${{ steps.paths.outputs.properties }} \
              --script-file ${{ steps.paths.outputs.script }} \
              --environment ${{ vars.POWER_PLATFORM_URL }}
          else
            $POWERPLATFORMTOOLS_PACPATH connector update \
              --connector-id ${{ steps.connector-id.outputs.id }} \
              --api-definition-file ${{ steps.paths.outputs.definition }} \
              --api-properties-file ${{ steps.paths.outputs.properties }} \
              --script-file ${{ steps.paths.outputs.script }} \
              --environment ${{ vars.POWER_PLATFORM_URL }}
          fi

      - name: Summary
        run: |
          echo "## ${{ matrix.environment }} Deployment Complete" >> $GITHUB_STEP_SUMMARY
          echo "| Detail | Value |" >> $GITHUB_STEP_SUMMARY
          echo "|--------|-------|" >> $GITHUB_STEP_SUMMARY
          echo "| Connector | ${{ github.event.inputs.connector }} |" >> $GITHUB_STEP_SUMMARY
          echo "| Version | $(jq -r '.info.version' ${{ steps.paths.outputs.definition }}) |" >> $GITHUB_STEP_SUMMARY
```

**Step 2: Update validate workflow**

Replace `.github/workflows/validate.yml` with:

```yaml
name: Validate PR

on:
  pull_request:
    paths:
      - 'connectors/**'

jobs:
  validate:
    name: Validate Connectors
    runs-on: ubuntu-latest
    strategy:
      matrix:
        connector: [claude, gemini]
    steps:
      - uses: actions/checkout@v4

      - name: Check connector exists
        id: check
        run: |
          if [ -d "connectors/${{ matrix.connector }}" ]; then
            echo "exists=true" >> $GITHUB_OUTPUT
          else
            echo "exists=false" >> $GITHUB_OUTPUT
          fi

      - name: Setup Node.js
        if: steps.check.outputs.exists == 'true'
        uses: actions/setup-node@v4
        with:
          node-version: '20'

      - name: Install and test
        if: steps.check.outputs.exists == 'true'
        working-directory: connectors/${{ matrix.connector }}
        run: |
          if [ -f "package.json" ]; then
            npm ci
            npm test
          fi
```

**Step 3: Commit**

```bash
git add .github/workflows/
git commit -m "chore: update CI/CD for monorepo structure

- Add connector selection to deploy workflow
- Support deploying claude or gemini connector
- Matrix deploy to multiple environments
- Per-connector validation

Generated with [Claude Code](https://claude.ai/code)
via [Happy](https://happy.engineering)

Co-Authored-By: Claude <noreply@anthropic.com>
Co-Authored-By: Happy <yesreply@happy.engineering>"
```

---

## Task 5: Update CLAUDE.md and README

**Files:**
- Modify: `CLAUDE.md`
- Modify: `README.md`

**Step 1: Update CLAUDE.md for monorepo**

Update the Project Structure section in `CLAUDE.md`:

```markdown
## Project Structure

```
ForIT-AI-Connectors/
├── connectors/
│   ├── claude/
│   │   ├── claude-connector.json    # OpenAPI 2.0 definition
│   │   ├── apiProperties.json       # Power Platform properties
│   │   ├── script.csx               # Request/response transforms
│   │   ├── icon.png
│   │   ├── package.json
│   │   └── tests/
│   │       └── validate-connector.js
│   └── gemini/
│       ├── gemini-connector.json    # OpenAPI 2.0 definition
│       ├── apiProperties.json       # Power Platform properties
│       ├── script.csx               # Request/response transforms
│       ├── icon.png
│       ├── package.json
│       └── tests/
│           └── validate-connector.js
├── shared/                          # Future shared utilities
├── docs/
│   └── plans/
├── .github/workflows/
│   ├── deploy-connector.yml         # CI/CD for all connectors
│   └── validate.yml                 # PR validation
└── README.md
```
```

**Step 2: Update README.md title and description**

Update `README.md` to reflect the monorepo:

```markdown
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
```

**Step 3: Commit**

```bash
git add CLAUDE.md README.md
git commit -m "docs: update documentation for monorepo structure

Generated with [Claude Code](https://claude.ai/code)
via [Happy](https://happy.engineering)

Co-Authored-By: Claude <noreply@anthropic.com>
Co-Authored-By: Happy <yesreply@happy.engineering>"
```

---

## Task 6: Create Gemini Connector - OpenAPI Definition

**Files:**
- Create: `connectors/gemini/gemini-connector.json`

**Step 1: Create the OpenAPI 2.0 definition**

Create `connectors/gemini/gemini-connector.json`:

```json
{
  "swagger": "2.0",
  "info": {
    "title": "ForIT Gemini Connector",
    "description": "Power Automate connector for Google's Gemini API.",
    "version": "1.0.0",
    "contact": {
      "name": "ForIT Support",
      "url": "https://github.com/ForITLLC/ForIT-AI-Connectors"
    },
    "x-ms-api-annotation": {
      "status": "Preview"
    }
  },
  "host": "generativelanguage.googleapis.com",
  "basePath": "/v1beta",
  "schemes": ["https"],
  "consumes": ["application/json"],
  "produces": ["application/json"],
  "paths": {
    "/models/{model}:generateContent": {
      "post": {
        "operationId": "AskGemini",
        "summary": "Ask Gemini",
        "description": "Simple prompt to Gemini. For advanced options, use Generate Content (Advanced).",
        "x-ms-visibility": "important",
        "parameters": [
          {
            "name": "model",
            "in": "path",
            "required": true,
            "type": "string",
            "description": "Gemini model",
            "enum": ["gemini-2.5-flash", "gemini-2.0-flash"],
            "default": "gemini-2.5-flash",
            "x-ms-summary": "Model",
            "x-ms-url-encoding": "single"
          },
          {
            "name": "key",
            "in": "query",
            "required": true,
            "type": "string",
            "x-ms-visibility": "internal",
            "x-ms-dynamic-values": {
              "value": "{gemini_api_key}"
            }
          },
          {
            "name": "body",
            "in": "body",
            "required": true,
            "schema": {
              "type": "object",
              "required": ["prompt"],
              "properties": {
                "prompt": {
                  "type": "string",
                  "description": "Your question for Gemini",
                  "x-ms-summary": "Prompt"
                },
                "output_format": {
                  "type": "string",
                  "description": "Response format",
                  "x-ms-summary": "Output Format",
                  "enum": ["text", "json"],
                  "default": "text",
                  "x-ms-visibility": "advanced"
                }
              }
            }
          }
        ],
        "responses": {
          "200": {
            "description": "Success",
            "schema": {
              "type": "object",
              "properties": {
                "response": { "type": "string", "x-ms-summary": "Response" },
                "model": { "type": "string", "x-ms-summary": "Model", "x-ms-visibility": "advanced" },
                "usage": {
                  "type": "object",
                  "x-ms-summary": "Usage",
                  "x-ms-visibility": "advanced",
                  "properties": {
                    "prompt_tokens": { "type": "integer", "x-ms-summary": "Prompt Tokens" },
                    "completion_tokens": { "type": "integer", "x-ms-summary": "Completion Tokens" }
                  }
                }
              }
            }
          },
          "400": { "description": "Bad request", "schema": { "$ref": "#/definitions/ErrorResponse" } },
          "401": { "description": "Invalid API key", "schema": { "$ref": "#/definitions/ErrorResponse" } },
          "429": { "description": "Rate limited", "schema": { "$ref": "#/definitions/ErrorResponse" } },
          "500": { "description": "Server error", "schema": { "$ref": "#/definitions/ErrorResponse" } }
        }
      }
    },
    "/models/{model}:generateContent/": {
      "post": {
        "operationId": "GenerateContent",
        "summary": "Generate Content (Advanced)",
        "description": "Full control over Gemini API with system instructions, temperature, safety settings, and all parameters.",
        "x-ms-visibility": "advanced",
        "parameters": [
          {
            "name": "model",
            "in": "path",
            "required": true,
            "type": "string",
            "description": "Gemini model",
            "enum": ["gemini-2.5-flash", "gemini-2.0-flash"],
            "default": "gemini-2.5-flash",
            "x-ms-summary": "Model",
            "x-ms-url-encoding": "single"
          },
          {
            "name": "key",
            "in": "query",
            "required": true,
            "type": "string",
            "x-ms-visibility": "internal"
          },
          {
            "name": "body",
            "in": "body",
            "required": true,
            "schema": {
              "type": "object",
              "required": ["contents"],
              "properties": {
                "contents": {
                  "type": "array",
                  "description": "Conversation contents",
                  "x-ms-summary": "Contents",
                  "items": {
                    "type": "object",
                    "properties": {
                      "role": { "type": "string", "enum": ["user", "model"], "x-ms-summary": "Role" },
                      "parts": {
                        "type": "array",
                        "x-ms-summary": "Parts",
                        "items": {
                          "type": "object",
                          "properties": {
                            "text": { "type": "string", "x-ms-summary": "Text" }
                          }
                        }
                      }
                    }
                  }
                },
                "systemInstruction": {
                  "type": "object",
                  "description": "System instruction",
                  "x-ms-summary": "System Instruction",
                  "properties": {
                    "parts": {
                      "type": "array",
                      "items": {
                        "type": "object",
                        "properties": {
                          "text": { "type": "string" }
                        }
                      }
                    }
                  }
                },
                "generationConfig": {
                  "type": "object",
                  "description": "Generation configuration",
                  "x-ms-summary": "Generation Config",
                  "properties": {
                    "temperature": { "type": "number", "x-ms-summary": "Temperature" },
                    "topP": { "type": "number", "x-ms-summary": "Top P" },
                    "topK": { "type": "integer", "x-ms-summary": "Top K" },
                    "maxOutputTokens": { "type": "integer", "x-ms-summary": "Max Output Tokens" },
                    "responseMimeType": { "type": "string", "x-ms-summary": "Response MIME Type" }
                  }
                },
                "safetySettings": {
                  "type": "array",
                  "description": "Safety settings",
                  "x-ms-summary": "Safety Settings",
                  "items": {
                    "type": "object",
                    "properties": {
                      "category": { "type": "string", "x-ms-summary": "Category" },
                      "threshold": { "type": "string", "x-ms-summary": "Threshold" }
                    }
                  }
                }
              }
            }
          }
        ],
        "responses": {
          "200": {
            "description": "Success",
            "schema": {
              "type": "object",
              "properties": {
                "candidates": {
                  "type": "array",
                  "x-ms-summary": "Candidates",
                  "items": {
                    "type": "object",
                    "properties": {
                      "content": {
                        "type": "object",
                        "properties": {
                          "parts": {
                            "type": "array",
                            "items": {
                              "type": "object",
                              "properties": {
                                "text": { "type": "string", "x-ms-summary": "Text" }
                              }
                            }
                          },
                          "role": { "type": "string", "x-ms-summary": "Role" }
                        }
                      },
                      "finishReason": { "type": "string", "x-ms-summary": "Finish Reason" },
                      "safetyRatings": { "type": "array", "x-ms-summary": "Safety Ratings" }
                    }
                  }
                },
                "usageMetadata": {
                  "type": "object",
                  "x-ms-summary": "Usage",
                  "properties": {
                    "promptTokenCount": { "type": "integer", "x-ms-summary": "Prompt Tokens" },
                    "candidatesTokenCount": { "type": "integer", "x-ms-summary": "Completion Tokens" },
                    "totalTokenCount": { "type": "integer", "x-ms-summary": "Total Tokens" }
                  }
                },
                "modelVersion": { "type": "string", "x-ms-summary": "Model Version" }
              }
            }
          },
          "400": { "description": "Bad request", "schema": { "$ref": "#/definitions/ErrorResponse" } },
          "401": { "description": "Invalid API key", "schema": { "$ref": "#/definitions/ErrorResponse" } },
          "429": { "description": "Rate limited", "schema": { "$ref": "#/definitions/ErrorResponse" } },
          "500": { "description": "Server error", "schema": { "$ref": "#/definitions/ErrorResponse" } }
        }
      }
    }
  },
  "definitions": {
    "ErrorResponse": {
      "type": "object",
      "properties": {
        "error": {
          "type": "object",
          "properties": {
            "code": { "type": "integer", "x-ms-summary": "Error Code" },
            "message": { "type": "string", "x-ms-summary": "Error Message" },
            "status": { "type": "string", "x-ms-summary": "Error Status" }
          }
        }
      }
    }
  },
  "x-ms-connector-metadata": [
    { "propertyName": "Website", "propertyValue": "https://ai.google.dev" },
    { "propertyName": "Privacy policy", "propertyValue": "https://policies.google.com/privacy" },
    { "propertyName": "Categories", "propertyValue": "AI;Productivity" }
  ]
}
```

**Step 2: Validate JSON**

Run: `node -e "JSON.parse(require('fs').readFileSync('connectors/gemini/gemini-connector.json'))"`
Expected: No error output

**Step 3: Commit**

```bash
git add connectors/gemini/gemini-connector.json
git commit -m "feat(gemini): add OpenAPI definition for Gemini connector

- AskGemini simple action with prompt in, response out
- GenerateContent advanced action with full API access
- Support for gemini-2.5-flash and gemini-2.0-flash models

Generated with [Claude Code](https://claude.ai/code)
via [Happy](https://happy.engineering)

Co-Authored-By: Claude <noreply@anthropic.com>
Co-Authored-By: Happy <yesreply@happy.engineering>"
```

---

## Task 7: Create Gemini Connector - apiProperties.json

**Files:**
- Create: `connectors/gemini/apiProperties.json`

**Step 1: Create apiProperties.json with API key auth**

Create `connectors/gemini/apiProperties.json`:

```json
{
  "properties": {
    "connectionParameters": {
      "gemini_api_key": {
        "type": "securestring",
        "uiDefinition": {
          "displayName": "Gemini API Key",
          "description": "Your Google Gemini API Key from aistudio.google.com",
          "tooltip": "Enter your Gemini API key",
          "constraints": {
            "required": "true",
            "tabIndex": 1
          }
        }
      }
    },
    "iconBrandColor": "#4285F4",
    "capabilities": [],
    "publisher": "ForIT",
    "script": "script.csx",
    "scriptOperations": ["AskGemini"],
    "policyTemplateInstances": [
      {
        "templateId": "routerequesttoendpoint",
        "title": "Add API Key to Query",
        "parameters": {
          "x-ms-apimTemplateParameter.newPath": "/@connectionParameters('gemini_api_key')",
          "x-ms-apimTemplateParameter.httpMethod": "@Request.OriginalHTTPMethod",
          "x-ms-apimTemplate-operationName": ["AskGemini", "GenerateContent"]
        }
      }
    ]
  }
}
```

**Note:** The API key injection via policy may need adjustment. Power Platform's `routerequesttoendpoint` policy may not work for query params. An alternative is using `setheader` to a custom header and then having the script add it to the query. Testing will reveal the correct approach.

**Step 2: Commit**

```bash
git add connectors/gemini/apiProperties.json
git commit -m "feat(gemini): add apiProperties with API key auth

- Gemini API key as connection parameter
- Blue brand color (#4285F4) matching Gemini branding
- script.csx for AskGemini transformation

Generated with [Claude Code](https://claude.ai/code)
via [Happy](https://happy.engineering)

Co-Authored-By: Claude <noreply@anthropic.com>
Co-Authored-By: Happy <yesreply@happy.engineering>"
```

---

## Task 8: Create Gemini Connector - script.csx

**Files:**
- Create: `connectors/gemini/script.csx`

**Step 1: Create script.csx for prompt transformation**

Create `connectors/gemini/script.csx`:

```csharp
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Script : ScriptBase
{
    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        // Handle AskGemini - transform simple prompt to contents array
        if (this.Context.OperationId == "AskGemini")
        {
            return await this.TransformPromptToContents().ConfigureAwait(false);
        }

        // Pass through all other operations unchanged
        return await this.Context.SendAsync(this.Context.Request, this.CancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> TransformPromptToContents()
    {
        // Read the incoming request body
        var contentAsString = await this.Context.Request.Content.ReadAsStringAsync()
            .ConfigureAwait(false);

        var body = JObject.Parse(contentAsString);

        // Check if "prompt" field exists (simple input)
        if (body["prompt"] != null)
        {
            var prompt = body["prompt"].ToString();
            var outputFormat = body["output_format"]?.ToString() ?? "text";

            // Create contents array from simple prompt
            var contents = new JArray
            {
                new JObject
                {
                    ["role"] = "user",
                    ["parts"] = new JArray
                    {
                        new JObject { ["text"] = prompt }
                    }
                }
            };

            // Remove our custom fields
            body.Remove("prompt");
            body.Remove("output_format");

            // Set contents
            body["contents"] = contents;

            // Set response MIME type if JSON requested
            if (outputFormat == "json")
            {
                body["generationConfig"] = new JObject
                {
                    ["responseMimeType"] = "application/json"
                };
            }
        }

        // Update request content with transformed body
        this.Context.Request.Content = CreateJsonContent(body.ToString());

        // Forward to Gemini API
        var response = await this.Context.SendAsync(this.Context.Request, this.CancellationToken)
            .ConfigureAwait(false);

        // Transform response to extract text
        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            var result = JObject.Parse(responseString);

            // Extract text response from candidates array
            if (result["candidates"] is JArray candidates && candidates.Count > 0)
            {
                var firstCandidate = candidates[0] as JObject;
                var content = firstCandidate?["content"] as JObject;
                var parts = content?["parts"] as JArray;

                if (parts != null && parts.Count > 0)
                {
                    var text = parts[0]?["text"]?.ToString() ?? "";
                    result["response"] = text;
                }
            }

            // Normalize usage metadata
            if (result["usageMetadata"] is JObject usage)
            {
                result["usage"] = new JObject
                {
                    ["prompt_tokens"] = usage["promptTokenCount"],
                    ["completion_tokens"] = usage["candidatesTokenCount"]
                };
            }

            // Add model info
            if (result["modelVersion"] != null)
            {
                result["model"] = result["modelVersion"];
            }

            response.Content = CreateJsonContent(result.ToString());
        }

        return response;
    }
}
```

**Step 2: Commit**

```bash
git add connectors/gemini/script.csx
git commit -m "feat(gemini): add script.csx for prompt transformation

- Transform simple prompt to contents/parts format
- Extract text from candidates[0].content.parts[0].text
- Normalize usage metadata to match Claude format
- Support JSON output format via responseMimeType

Generated with [Claude Code](https://claude.ai/code)
via [Happy](https://happy.engineering)

Co-Authored-By: Claude <noreply@anthropic.com>
Co-Authored-By: Happy <yesreply@happy.engineering>"
```

---

## Task 9: Create Gemini Connector - Tests and Package

**Files:**
- Create: `connectors/gemini/package.json`
- Create: `connectors/gemini/tests/validate-connector.js`

**Step 1: Create package.json**

Create `connectors/gemini/package.json`:

```json
{
  "name": "forit-gemini-connector",
  "version": "1.0.0",
  "description": "ForIT Gemini Connector validation tests",
  "scripts": {
    "test": "node tests/validate-connector.js"
  },
  "devDependencies": {
    "ajv": "^8.12.0"
  }
}
```

**Step 2: Create validation test**

Create `connectors/gemini/tests/validate-connector.js`:

```javascript
const fs = require('fs');
const path = require('path');

const connectorPath = path.join(__dirname, '..', 'gemini-connector.json');
const propertiesPath = path.join(__dirname, '..', 'apiProperties.json');
const scriptPath = path.join(__dirname, '..', 'script.csx');

let errors = [];

// Test 1: Connector JSON is valid
try {
  const connector = JSON.parse(fs.readFileSync(connectorPath, 'utf8'));
  console.log('✓ gemini-connector.json is valid JSON');

  // Test required fields
  if (!connector.swagger) errors.push('Missing swagger field');
  if (!connector.info?.title) errors.push('Missing info.title');
  if (!connector.host) errors.push('Missing host');
  if (!connector.paths) errors.push('Missing paths');

  // Test host is Gemini
  if (connector.host !== 'generativelanguage.googleapis.com') {
    errors.push(`Expected host generativelanguage.googleapis.com, got ${connector.host}`);
  }

  // Test AskGemini operation exists
  const askGeminiPath = Object.keys(connector.paths).find(p =>
    connector.paths[p].post?.operationId === 'AskGemini'
  );
  if (!askGeminiPath) {
    errors.push('Missing AskGemini operation');
  } else {
    console.log('✓ AskGemini operation exists');
  }

  // Test GenerateContent operation exists
  const generatePath = Object.keys(connector.paths).find(p =>
    connector.paths[p].post?.operationId === 'GenerateContent'
  );
  if (!generatePath) {
    errors.push('Missing GenerateContent operation');
  } else {
    console.log('✓ GenerateContent operation exists');
  }

} catch (e) {
  errors.push(`Failed to parse gemini-connector.json: ${e.message}`);
}

// Test 2: apiProperties.json is valid
try {
  const props = JSON.parse(fs.readFileSync(propertiesPath, 'utf8'));
  console.log('✓ apiProperties.json is valid JSON');

  // Test script is configured
  if (props.properties?.script !== 'script.csx') {
    errors.push('script not set to script.csx in apiProperties.json');
  }

  // Test scriptOperations includes AskGemini
  if (!props.properties?.scriptOperations?.includes('AskGemini')) {
    errors.push('scriptOperations must include AskGemini');
  }

  // Test connection parameter exists
  if (!props.properties?.connectionParameters?.gemini_api_key) {
    errors.push('Missing gemini_api_key connection parameter');
  } else {
    console.log('✓ gemini_api_key connection parameter configured');
  }

} catch (e) {
  errors.push(`Failed to parse apiProperties.json: ${e.message}`);
}

// Test 3: script.csx exists
if (fs.existsSync(scriptPath)) {
  console.log('✓ script.csx exists');

  const script = fs.readFileSync(scriptPath, 'utf8');

  // Check for AskGemini handling
  if (!script.includes('AskGemini')) {
    errors.push('script.csx does not handle AskGemini operation');
  }

  // Check for contents transformation
  if (!script.includes('contents')) {
    errors.push('script.csx does not transform to contents format');
  }

} else {
  errors.push('script.csx does not exist');
}

// Summary
console.log('\n--- Validation Summary ---');
if (errors.length === 0) {
  console.log('All validations passed!');
  process.exit(0);
} else {
  console.log(`${errors.length} error(s) found:`);
  errors.forEach(e => console.log(`  ✗ ${e}`));
  process.exit(1);
}
```

**Step 3: Commit**

```bash
git add connectors/gemini/package.json connectors/gemini/tests/
git commit -m "feat(gemini): add validation tests

- Validate JSON structure
- Check required operations exist
- Verify script.csx handles AskGemini
- Verify apiProperties configuration

Generated with [Claude Code](https://claude.ai/code)
via [Happy](https://happy.engineering)

Co-Authored-By: Claude <noreply@anthropic.com>
Co-Authored-By: Happy <yesreply@happy.engineering>"
```

---

## Task 10: Run Tests and Verify

**Step 1: Install dependencies and run Claude tests**

```bash
cd connectors/claude && npm ci && npm test && cd ../..
```

Expected: All validations passed

**Step 2: Install dependencies and run Gemini tests**

```bash
cd connectors/gemini && npm ci && npm test && cd ../..
```

Expected: All validations passed

**Step 3: Final commit with any fixes**

If tests reveal issues, fix them and commit:

```bash
git add -A
git commit -m "fix: address test failures

Generated with [Claude Code](https://claude.ai/code)
via [Happy](https://happy.engineering)

Co-Authored-By: Claude <noreply@anthropic.com>
Co-Authored-By: Happy <yesreply@happy.engineering>"
```

---

## Task 11: Update GitHub Repo Settings (Manual)

This task requires manual intervention in GitHub:

1. Rename repository from `ForIT-Claude-Connector` to `ForIT-AI-Connectors`
2. Add new environment variables for Gemini:
   - `GEMINI_CONNECTOR_ID` in each environment (after first deploy with `action=create`)

---

## Summary

After completing all tasks:

1. **Monorepo structure** with `connectors/claude/` and `connectors/gemini/`
2. **Shared CI/CD** that deploys either connector to any environment
3. **Gemini connector** with:
   - `AskGemini` simple action (prompt → response)
   - `GenerateContent` advanced action (full API)
   - script.csx transformations
   - Validation tests
4. **Documentation** updated for monorepo

**First deployment steps:**
1. Push to GitHub
2. Run workflow: `connector=gemini, environment=development, action=create`
3. Note the new connector ID
4. Add `GEMINI_CONNECTOR_ID` to GitHub environment variables
5. Subsequent deploys use `action=update`
