using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class Script : ScriptBase
{
    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        // ForIT AI proxy handles all routing - connector just passes through
        // The proxy at ai.forit.io validates license and routes to providers

        var response = await this.Context.SendAsync(this.Context.Request, this.CancellationToken);

        // Transform response for simple actions to extract just the text
        if (this.Context.OperationId == "AskAI" ||
            this.Context.OperationId == "AskClaude" ||
            this.Context.OperationId == "AskGemini")
        {
            response = await TransformResponse(response);
        }

        return response;
    }

    private async Task<HttpResponseMessage> TransformResponse(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            return response;
        }

        var content = await response.Content.ReadAsStringAsync();

        // Response from ForIT proxy is already normalized
        // Just ensure we have the expected format
        try
        {
            var json = JObject.Parse(content);

            // If response field exists, we're good
            if (json["response"] != null)
            {
                return response;
            }

            // Handle raw provider responses if proxy returns them
            string text = null;
            string model = null;
            string provider = null;

            // Claude format
            if (json["content"] != null)
            {
                var contentArray = json["content"] as JArray;
                if (contentArray != null)
                {
                    foreach (var item in contentArray)
                    {
                        if (item["type"]?.ToString() == "text")
                        {
                            text = item["text"]?.ToString();
                            break;
                        }
                    }
                }
                model = json["model"]?.ToString();
                provider = "claude";
            }
            // Gemini format
            else if (json["candidates"] != null)
            {
                var candidates = json["candidates"] as JArray;
                if (candidates != null && candidates.Count > 0)
                {
                    var parts = candidates[0]["content"]?["parts"] as JArray;
                    if (parts != null && parts.Count > 0)
                    {
                        text = parts[0]["text"]?.ToString();
                    }
                }
                model = json["modelVersion"]?.ToString();
                provider = "gemini";
            }

            if (text != null)
            {
                var result = new JObject
                {
                    ["response"] = text,
                    ["provider"] = provider,
                    ["model"] = model
                };

                response.Content = new StringContent(
                    result.ToString(),
                    Encoding.UTF8,
                    "application/json"
                );
            }
        }
        catch
        {
            // If parsing fails, return original response
        }

        return response;
    }
}
