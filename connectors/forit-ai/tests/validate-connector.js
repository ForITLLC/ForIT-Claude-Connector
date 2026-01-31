const fs = require('fs');
const path = require('path');

const connectorPath = path.join(__dirname, '..', 'forit-ai-connector.json');
const propertiesPath = path.join(__dirname, '..', 'apiProperties.json');

console.log('Validating ForIT AI connector...\n');

// Validate connector JSON
let connector;
try {
    const content = fs.readFileSync(connectorPath, 'utf8');
    connector = JSON.parse(content);
    console.log('✓ forit-ai-connector.json is valid JSON');
} catch (e) {
    console.error('✗ forit-ai-connector.json is invalid:', e.message);
    process.exit(1);
}

// Validate apiProperties JSON
let properties;
try {
    const content = fs.readFileSync(propertiesPath, 'utf8');
    properties = JSON.parse(content);
    console.log('✓ apiProperties.json is valid JSON');
} catch (e) {
    console.error('✗ apiProperties.json is invalid:', e.message);
    process.exit(1);
}

// Validate OpenAPI 2.0 structure
if (connector.swagger !== '2.0') {
    console.error('✗ Must be OpenAPI 2.0 (swagger: "2.0")');
    process.exit(1);
}
console.log('✓ OpenAPI version is 2.0');

// Validate host
if (connector.host !== 'ai.forit.io') {
    console.error('✗ Host must be ai.forit.io');
    process.exit(1);
}
console.log('✓ Host is ai.forit.io');

// Validate required operations
const requiredOps = ['AskAI', 'AskClaude', 'AskGemini', 'GetLicenseStatus'];
const operations = [];
for (const [path, methods] of Object.entries(connector.paths)) {
    for (const [method, op] of Object.entries(methods)) {
        if (op.operationId) {
            operations.push(op.operationId);
        }
    }
}

for (const op of requiredOps) {
    if (!operations.includes(op)) {
        console.error(`✗ Missing required operation: ${op}`);
        process.exit(1);
    }
}
console.log(`✓ All required operations present: ${requiredOps.join(', ')}`);

// Validate security
if (!connector.securityDefinitions?.api_key) {
    console.error('✗ Missing api_key security definition');
    process.exit(1);
}
if (connector.securityDefinitions.api_key.name !== 'x-forit-license') {
    console.error('✗ Security header must be x-forit-license');
    process.exit(1);
}
console.log('✓ Security configured with x-forit-license header');

// Validate apiProperties
if (!properties.properties?.connectionParameters?.api_key) {
    console.error('✗ Missing api_key connection parameter');
    process.exit(1);
}
console.log('✓ Connection parameter configured for license key');

// Validate policy
const policies = properties.properties?.policyTemplateInstances || [];
const headerPolicy = policies.find(p => p.templateId === 'setheader');
if (!headerPolicy) {
    console.error('✗ Missing setheader policy');
    process.exit(1);
}
console.log('✓ Header policy configured');

console.log('\n✓ All validations passed!');
