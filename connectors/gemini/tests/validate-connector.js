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
