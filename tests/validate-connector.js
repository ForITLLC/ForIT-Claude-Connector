const fs = require('fs');
const path = require('path');

console.log('Validating Claude connector...\n');

let hasErrors = false;
let hasWarnings = false;

// Load connector definition
const connectorPath = path.join(__dirname, '..', 'claude-connector.json');
const propertiesPath = path.join(__dirname, '..', 'apiProperties.json');

let connector, properties;

try {
  connector = JSON.parse(fs.readFileSync(connectorPath, 'utf8'));
  console.log('Connector JSON: Valid');
} catch (e) {
  console.error('ERROR: Invalid connector JSON:', e.message);
  process.exit(1);
}

try {
  properties = JSON.parse(fs.readFileSync(propertiesPath, 'utf8'));
  console.log('API Properties JSON: Valid');
} catch (e) {
  console.error('ERROR: Invalid apiProperties JSON:', e.message);
  process.exit(1);
}

// Validate OpenAPI version
if (connector.swagger !== '2.0') {
  console.error('ERROR: Must use swagger 2.0 for Power Platform');
  hasErrors = true;
}

// Validate required fields
const requiredFields = ['info', 'host', 'basePath', 'paths', 'securityDefinitions'];
for (const field of requiredFields) {
  if (!connector[field]) {
    console.error(`ERROR: Missing required field: ${field}`);
    hasErrors = true;
  }
}

// Validate info section
if (connector.info) {
  if (!connector.info.title) {
    console.error('ERROR: Missing info.title');
    hasErrors = true;
  }
  if (!connector.info.version) {
    console.error('ERROR: Missing info.version');
    hasErrors = true;
  }
  if (connector.info.title && connector.info.title.length > 50) {
    console.warn('WARNING: Title exceeds 50 characters');
    hasWarnings = true;
  }
}

// Validate security (API Key for Claude)
if (connector.securityDefinitions) {
  const secDefs = Object.keys(connector.securityDefinitions);
  if (secDefs.length === 0) {
    console.error('ERROR: No security definitions found');
    hasErrors = true;
  } else {
    console.log(`Security: ${secDefs.join(', ')}`);

    // Validate API key configuration
    const apiKey = connector.securityDefinitions.api_key;
    if (apiKey) {
      if (apiKey.type !== 'apiKey') {
        console.error('ERROR: api_key must have type "apiKey"');
        hasErrors = true;
      }
      if (apiKey.in !== 'header') {
        console.error('ERROR: api_key must be in header');
        hasErrors = true;
      }
      if (apiKey.name !== 'x-api-key') {
        console.error('ERROR: api_key header name should be "x-api-key" for Anthropic');
        hasErrors = true;
      }
    }
  }
}

// Validate operations
const paths = connector.paths || {};
let operationCount = 0;
const operationIds = new Set();

for (const [pathName, pathDef] of Object.entries(paths)) {
  for (const [method, operation] of Object.entries(pathDef)) {
    if (['get', 'post', 'put', 'patch', 'delete'].includes(method)) {
      operationCount++;

      if (!operation.operationId) {
        console.error(`ERROR: Missing operationId for ${method.toUpperCase()} ${pathName}`);
        hasErrors = true;
      } else {
        if (operationIds.has(operation.operationId)) {
          console.error(`ERROR: Duplicate operationId: ${operation.operationId}`);
          hasErrors = true;
        }
        operationIds.add(operation.operationId);
      }

      if (!operation.summary) {
        console.warn(`WARNING: Missing summary for ${operation.operationId || pathName}`);
        hasWarnings = true;
      }

      // Check for responses
      if (!operation.responses) {
        console.error(`ERROR: Missing responses for ${operation.operationId}`);
        hasErrors = true;
      } else {
        // Check for error responses
        const errorCodes = ['400', '401', '403', '404', '429', '500'];
        const hasErrorResponses = errorCodes.some(code => operation.responses[code]);
        if (!hasErrorResponses) {
          console.warn(`WARNING: No error responses defined for ${operation.operationId}`);
          hasWarnings = true;
        }
      }
    }
  }
}

console.log(`\nOperations found: ${operationCount}`);
console.log(`Operation IDs: ${Array.from(operationIds).join(', ')}`);

// Validate apiProperties
if (properties.properties) {
  if (!properties.properties.connectionParameters) {
    console.error('ERROR: Missing connectionParameters in apiProperties');
    hasErrors = true;
  } else {
    const params = Object.keys(properties.properties.connectionParameters);
    console.log(`\nConnection parameters: ${params.join(', ')}`);
  }

  if (!properties.properties.publisher) {
    console.warn('WARNING: Missing publisher in apiProperties');
    hasWarnings = true;
  }
}

// Summary
console.log('\n' + '='.repeat(50));
if (hasErrors) {
  console.error('VALIDATION FAILED: Please fix errors above');
  process.exit(1);
} else if (hasWarnings) {
  console.log('VALIDATION PASSED with warnings');
  process.exit(0);
} else {
  console.log('VALIDATION PASSED');
  process.exit(0);
}
