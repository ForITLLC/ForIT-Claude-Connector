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

        // Handle AskGemini - transform simple prompt to contents array
        if (this.Context.OperationId == "AskGemini")
        {
            return await this.TransformPromptToContents().ConfigureAwait(false);
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

            // Remove the license header before forwarding to Gemini API
            this.Context.Request.Headers.Remove("x-forit-license");

            // Call ForIT license validation endpoint
            using (var client = new HttpClient())
            {
                var validateRequest = new HttpRequestMessage(HttpMethod.Post, LICENSE_VALIDATION_URL);
                validateRequest.Content = CreateJsonContent(JsonConvert.SerializeObject(new
                {
                    license_key = licenseKey,
                    product = "forit-gemini-connector"
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
