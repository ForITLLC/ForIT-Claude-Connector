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

        // Handle AskClaude - transform simple prompt to messages array
        if (this.Context.OperationId == "AskClaude")
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
            var attachment = body["attachment"]?.ToString();
            var attachmentType = body["attachment_type"]?.ToString();

            // Build content - either string or array with image
            object content;
            if (!string.IsNullOrEmpty(attachment) && !string.IsNullOrEmpty(attachmentType))
            {
                // Multimodal content with image
                content = new JArray
                {
                    new JObject
                    {
                        ["type"] = "image",
                        ["source"] = new JObject
                        {
                            ["type"] = "base64",
                            ["media_type"] = attachmentType,
                            ["data"] = attachment
                        }
                    },
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = prompt
                    }
                };
            }
            else
            {
                // Text-only content
                content = prompt;
            }

            // Create messages array
            var messages = new JArray
            {
                new JObject
                {
                    ["role"] = "user",
                    ["content"] = content
                }
            };

            // Replace prompt with messages array
            body.Remove("prompt");
            body.Remove("attachment");
            body.Remove("attachment_type");
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
