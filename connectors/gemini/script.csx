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
    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        // Remove ForIT license header if present (validated at connection time, not per-request)
        if (this.Context.Request.Headers.Contains("x-forit-license"))
        {
            this.Context.Request.Headers.Remove("x-forit-license");
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
