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
        // Handle AskClaude - transform simple prompt to messages array
        if (this.Context.OperationId == "AskClaude" || this.Context.OperationId == "AskClaudeWithThinking")
        {
            return await this.TransformPromptToMessages().ConfigureAwait(false);
        }

        // Pass through all other operations unchanged
        return await this.Context.SendAsync(this.Context.Request, this.CancellationToken)
            .ConfigureAwait(false);
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
