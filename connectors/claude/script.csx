using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Script : ScriptBase
{
    private const string LICENSE_VALIDATION_URL = "https://ai.forit.io/license/validate";

    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        // Check for ForIT license if header is present (licensed build)
        if (this.Context.Request.Headers.Contains("x-forit-license"))
        {
            var licenseCheck = await this.ValidateLicense().ConfigureAwait(false);
            if (licenseCheck != null)
            {
                return licenseCheck; // Return error response if license invalid
            }
        }

        // Handle AskClaude - transform simple prompt to messages array
        if (this.Context.OperationId == "AskClaude")
        {
            return await this.TransformPromptToMessages().ConfigureAwait(false);
        }

        // Pass through all other operations unchanged
        return await this.Context.SendAsync(this.Context.Request, this.CancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> ValidateLicense()
    {
        try
        {
            var licenseKey = this.Context.Request.Headers.GetValues("x-forit-license").FirstOrDefault();
            if (string.IsNullOrEmpty(licenseKey))
            {
                return CreateErrorResponse(HttpStatusCode.Unauthorized, "ForIT license key is required");
            }

            // Remove the license header before forwarding to Claude API
            this.Context.Request.Headers.Remove("x-forit-license");

            // Call ForIT license validation endpoint
            using (var client = new HttpClient())
            {
                var validateRequest = new HttpRequestMessage(HttpMethod.Post, LICENSE_VALIDATION_URL);
                validateRequest.Content = CreateJsonContent(JsonConvert.SerializeObject(new
                {
                    license_key = licenseKey,
                    product = "forit-claude-connector"
                }));

                var validateResponse = await client.SendAsync(validateRequest).ConfigureAwait(false);

                if (!validateResponse.IsSuccessStatusCode)
                {
                    var errorBody = await validateResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return CreateErrorResponse(HttpStatusCode.Unauthorized, $"License validation failed: {errorBody}");
                }

                var responseBody = await validateResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var result = JObject.Parse(responseBody);

                if (result["valid"]?.Value<bool>() != true)
                {
                    return CreateErrorResponse(HttpStatusCode.Unauthorized, result["message"]?.ToString() ?? "Invalid license");
                }
            }

            return null; // License is valid, continue processing
        }
        catch (Exception ex)
        {
            // If license server is unreachable, allow the request (fail open for availability)
            // You can change this to fail closed if security is more important than availability
            return null;
        }
    }

    private HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string message)
    {
        var response = new HttpResponseMessage(statusCode);
        response.Content = CreateJsonContent(JsonConvert.SerializeObject(new
        {
            error = new { message = message, type = "license_error" }
        }));
        return response;
    }

    private async Task<HttpResponseMessage> TransformPromptToMessages()
    {
        // Read the incoming request body
        var contentAsString = await this.Context.Request.Content.ReadAsStringAsync()
            .ConfigureAwait(false);

        var body = JObject.Parse(contentAsString);

        // Check if "prompt" field exists (simple input)
        if (body["prompt"] != null)
        {
            var prompt = body["prompt"].ToString();

            // Create messages array from simple prompt
            var messages = new JArray
            {
                new JObject
                {
                    ["role"] = "user",
                    ["content"] = prompt
                }
            };

            // Replace prompt with messages array
            body.Remove("prompt");
            body["messages"] = messages;
        }

        // Handle enable_thinking and thinking_budget
        bool enableThinking = body["enable_thinking"]?.Value<bool>() ?? false;
        int thinkingBudget = body["thinking_budget"]?.Value<int>() ?? 10000;

        // Remove our custom fields (not part of Claude API)
        body.Remove("enable_thinking");
        body.Remove("thinking_budget");

        if (enableThinking)
        {
            // Build thinking object for Claude API
            body["thinking"] = new JObject
            {
                ["type"] = "enabled",
                ["budget_tokens"] = thinkingBudget
            };

            // Ensure max_tokens is sufficient for thinking
            int maxTokens = body["max_tokens"]?.Value<int>() ?? 4096;
            if (maxTokens < thinkingBudget + 4096)
            {
                body["max_tokens"] = thinkingBudget + 4096;
            }
        }

        // Update request content with transformed body
        this.Context.Request.Content = CreateJsonContent(body.ToString());

        // Forward to Claude API
        var response = await this.Context.SendAsync(this.Context.Request, this.CancellationToken)
            .ConfigureAwait(false);

        // Transform response to extract text from content array
        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            var result = JObject.Parse(responseString);

            // Extract text response from content array for simpler output
            if (result["content"] is JArray contentArray && contentArray.Count > 0)
            {
                string responseText = "";
                string thinkingText = "";

                foreach (var item in contentArray)
                {
                    var itemType = item["type"]?.ToString();
                    if (itemType == "text")
                    {
                        responseText = item["text"]?.ToString() ?? "";
                    }
                    else if (itemType == "thinking")
                    {
                        thinkingText = item["thinking"]?.ToString() ?? "";
                    }
                }

                // Add simplified response fields
                result["response"] = responseText;
                if (!string.IsNullOrEmpty(thinkingText))
                {
                    result["thinking_output"] = thinkingText;
                }
            }

            response.Content = CreateJsonContent(result.ToString());
        }

        return response;
    }
}
