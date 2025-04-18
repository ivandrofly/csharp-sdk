﻿using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol.Messages;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using ModelContextProtocol.Utils;
using ModelContextProtocol.Utils.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ModelContextProtocol.Client;

/// <summary>Provides extension methods for interacting with an <see cref="IMcpClient"/>.</summary>
public static class McpClientExtensions
{
    /// <summary>
    /// Sends a ping request to verify server connectivity.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task that completes when the ping is successful.</returns>
    public static Task PingAsync(this IMcpClient client, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        return client.SendRequestAsync(
            RequestMethods.Ping,
            parameters: null,
            McpJsonUtilities.JsonContext.Default.Object!,
            McpJsonUtilities.JsonContext.Default.Object,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Retrieves a list of available tools from the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="serializerOptions">The serializer options governing tool parameter serialization.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A list of all available tools.</returns>
    public static async Task<IList<McpClientTool>> ListToolsAsync(
        this IMcpClient client,
        JsonSerializerOptions? serializerOptions = null,
        CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        serializerOptions ??= McpJsonUtilities.DefaultOptions;
        serializerOptions.MakeReadOnly();

        List<McpClientTool>? tools = null;
        string? cursor = null;
        do
        {
            var toolResults = await client.SendRequestAsync(
                RequestMethods.ToolsList,
                new() { Cursor = cursor },
                McpJsonUtilities.JsonContext.Default.ListToolsRequestParams,
                McpJsonUtilities.JsonContext.Default.ListToolsResult,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            tools ??= new List<McpClientTool>(toolResults.Tools.Count);
            foreach (var tool in toolResults.Tools)
            {
                tools.Add(new McpClientTool(client, tool, serializerOptions));
            }

            cursor = toolResults.NextCursor;
        }
        while (cursor is not null);

        return tools;
    }

    /// <summary>
    /// Creates an enumerable for asynchronously enumerating all available tools from the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="serializerOptions">The serializer options governing tool parameter serialization.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>An asynchronous sequence of all available tools.</returns>
    /// <remarks>
    /// Every iteration through the returned <see cref="IAsyncEnumerable{McpClientTool}"/>
    /// will result in requerying the server and yielding the sequence of available tools.
    /// </remarks>
    public static async IAsyncEnumerable<McpClientTool> EnumerateToolsAsync(
        this IMcpClient client,
        JsonSerializerOptions? serializerOptions = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        serializerOptions ??= McpJsonUtilities.DefaultOptions;
        serializerOptions.MakeReadOnly();

        string? cursor = null;
        do
        {
            var toolResults = await client.SendRequestAsync(
                RequestMethods.ToolsList,
                new() { Cursor = cursor },
                McpJsonUtilities.JsonContext.Default.ListToolsRequestParams,
                McpJsonUtilities.JsonContext.Default.ListToolsResult,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var tool in toolResults.Tools)
            {
                yield return new McpClientTool(client, tool, serializerOptions);
            }

            cursor = toolResults.NextCursor;
        }
        while (cursor is not null);
    }

    /// <summary>
    /// Retrieves a list of available prompts from the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A list of all available prompts.</returns>
    public static async Task<IList<McpClientPrompt>> ListPromptsAsync(
        this IMcpClient client, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        List<McpClientPrompt>? prompts = null;
        string? cursor = null;
        do
        {
            var promptResults = await client.SendRequestAsync(
                RequestMethods.PromptsList,
                new() { Cursor = cursor },
                McpJsonUtilities.JsonContext.Default.ListPromptsRequestParams,
                McpJsonUtilities.JsonContext.Default.ListPromptsResult,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            prompts ??= new List<McpClientPrompt>(promptResults.Prompts.Count);
            foreach (var prompt in promptResults.Prompts)
            {
                prompts.Add(new McpClientPrompt(client, prompt));
            }

            cursor = promptResults.NextCursor;
        }
        while (cursor is not null);

        return prompts;
    }

    /// <summary>
    /// Creates an enumerable for asynchronously enumerating all available prompts from the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>An asynchronous sequence of all available prompts.</returns>
    /// <remarks>
    /// Every iteration through the returned <see cref="IAsyncEnumerable{Prompt}"/>
    /// will result in requerying the server and yielding the sequence of available prompts.
    /// </remarks>
    public static async IAsyncEnumerable<Prompt> EnumeratePromptsAsync(
        this IMcpClient client, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        string? cursor = null;
        do
        {
            var promptResults = await client.SendRequestAsync(
                RequestMethods.PromptsList,
                new() { Cursor = cursor },
                McpJsonUtilities.JsonContext.Default.ListPromptsRequestParams,
                McpJsonUtilities.JsonContext.Default.ListPromptsResult,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var prompt in promptResults.Prompts)
            {
                yield return prompt;
            }

            cursor = promptResults.NextCursor;
        }
        while (cursor is not null);
    }

    /// <summary>
    /// Retrieves a specific prompt with optional arguments.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="name">The name of the prompt to retrieve</param>
    /// <param name="arguments">Optional arguments for the prompt</param>
    /// <param name="serializerOptions">The serialization options governing argument serialization.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task containing the prompt's content and messages.</returns>
    public static Task<GetPromptResult> GetPromptAsync(
        this IMcpClient client,
        string name,
        IReadOnlyDictionary<string, object?>? arguments = null,
        JsonSerializerOptions? serializerOptions = null,
        CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);
        Throw.IfNullOrWhiteSpace(name);
        serializerOptions ??= McpJsonUtilities.DefaultOptions;
        serializerOptions.MakeReadOnly();

        return client.SendRequestAsync(
            RequestMethods.PromptsGet,
            new() { Name = name, Arguments = ToArgumentsDictionary(arguments, serializerOptions) },
            McpJsonUtilities.JsonContext.Default.GetPromptRequestParams,
            McpJsonUtilities.JsonContext.Default.GetPromptResult,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Retrieves a list of available resource templates from the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A list of all available resource templates.</returns>
    public static async Task<IList<ResourceTemplate>> ListResourceTemplatesAsync(
        this IMcpClient client, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        List<ResourceTemplate>? templates = null;

        string? cursor = null;
        do
        {
            var templateResults = await client.SendRequestAsync(
                RequestMethods.ResourcesTemplatesList,
                new() { Cursor = cursor },
                McpJsonUtilities.JsonContext.Default.ListResourceTemplatesRequestParams,
                McpJsonUtilities.JsonContext.Default.ListResourceTemplatesResult,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (templates is null)
            {
                templates = templateResults.ResourceTemplates;
            }
            else
            {
                templates.AddRange(templateResults.ResourceTemplates);
            }

            cursor = templateResults.NextCursor;
        }
        while (cursor is not null);

        return templates;
    }

    /// <summary>
    /// Creates an enumerable for asynchronously enumerating all available resource templates from the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>An asynchronous sequence of all available resource templates.</returns>
    /// <remarks>
    /// Every iteration through the returned <see cref="IAsyncEnumerable{ResourceTemplate}"/>
    /// will result in requerying the server and yielding the sequence of available resource templates.
    /// </remarks>
    public static async IAsyncEnumerable<ResourceTemplate> EnumerateResourceTemplatesAsync(
        this IMcpClient client, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        string? cursor = null;
        do
        {
            var templateResults = await client.SendRequestAsync(
                RequestMethods.ResourcesTemplatesList,
                new() { Cursor = cursor },
                McpJsonUtilities.JsonContext.Default.ListResourceTemplatesRequestParams,
                McpJsonUtilities.JsonContext.Default.ListResourceTemplatesResult,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var template in templateResults.ResourceTemplates)
            {
                yield return template;
            }

            cursor = templateResults.NextCursor;
        }
        while (cursor is not null);
    }

    /// <summary>
    /// Retrieves a list of available resources from the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A list of all available resources.</returns>
    public static async Task<IList<Resource>> ListResourcesAsync(
        this IMcpClient client, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        List<Resource>? resources = null;

        string? cursor = null;
        do
        {
            var resourceResults = await client.SendRequestAsync(
                RequestMethods.ResourcesList,
                new() { Cursor = cursor },
                McpJsonUtilities.JsonContext.Default.ListResourcesRequestParams,
                McpJsonUtilities.JsonContext.Default.ListResourcesResult,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (resources is null)
            {
                resources = resourceResults.Resources;
            }
            else
            {
                resources.AddRange(resourceResults.Resources);
            }

            cursor = resourceResults.NextCursor;
        }
        while (cursor is not null);

        return resources;
    }

    /// <summary>
    /// Creates an enumerable for asynchronously enumerating all available resources from the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>An asynchronous sequence of all available resources.</returns>
    /// <remarks>
    /// Every iteration through the returned <see cref="IAsyncEnumerable{Resource}"/>
    /// will result in requerying the server and yielding the sequence of available resources.
    /// </remarks>
    public static async IAsyncEnumerable<Resource> EnumerateResourcesAsync(
        this IMcpClient client, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        string? cursor = null;
        do
        {
            var resourceResults = await client.SendRequestAsync(
                RequestMethods.ResourcesList,
                new() { Cursor = cursor },
                McpJsonUtilities.JsonContext.Default.ListResourcesRequestParams,
                McpJsonUtilities.JsonContext.Default.ListResourcesResult,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var resource in resourceResults.Resources)
            {
                yield return resource;
            }

            cursor = resourceResults.NextCursor;
        }
        while (cursor is not null);
    }

    /// <summary>
    /// Reads a resource from the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="uri">The uri of the resource.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    public static Task<ReadResourceResult> ReadResourceAsync(
        this IMcpClient client, string uri, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);
        Throw.IfNullOrWhiteSpace(uri);

        return client.SendRequestAsync(
            RequestMethods.ResourcesRead,
            new() { Uri = uri },
            McpJsonUtilities.JsonContext.Default.ReadResourceRequestParams,
            McpJsonUtilities.JsonContext.Default.ReadResourceResult,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets the completion options for a resource or prompt reference and (named) argument.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="reference">A resource (uri) or prompt (name) reference</param>
    /// <param name="argumentName">Name of argument. Must be non-null and non-empty.</param>
    /// <param name="argumentValue">Value of argument. Must be non-null.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    public static Task<CompleteResult> CompleteAsync(this IMcpClient client, Reference reference, string argumentName, string argumentValue, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);
        Throw.IfNull(reference);
        Throw.IfNullOrWhiteSpace(argumentName);

        if (!reference.Validate(out string? validationMessage))
        {
            throw new ArgumentException($"Invalid reference: {validationMessage}", nameof(reference));
        }

        return client.SendRequestAsync(
            RequestMethods.CompletionComplete,
            new()
            {
                Ref = reference,
                Argument = new Argument { Name = argumentName, Value = argumentValue }
            },
            McpJsonUtilities.JsonContext.Default.CompleteRequestParams,
            McpJsonUtilities.JsonContext.Default.CompleteResult,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Subscribes to a resource on the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="uri">The uri of the resource.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    public static Task SubscribeToResourceAsync(this IMcpClient client, string uri, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);
        Throw.IfNullOrWhiteSpace(uri);

        return client.SendRequestAsync(
            RequestMethods.ResourcesSubscribe,
            new() { Uri = uri },
            McpJsonUtilities.JsonContext.Default.SubscribeRequestParams,
            McpJsonUtilities.JsonContext.Default.EmptyResult,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Unsubscribes from a resource on the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="uri">The uri of the resource.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    public static Task UnsubscribeFromResourceAsync(this IMcpClient client, string uri, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);
        Throw.IfNullOrWhiteSpace(uri);

        return client.SendRequestAsync(
            RequestMethods.ResourcesUnsubscribe,
            new() { Uri = uri },
            McpJsonUtilities.JsonContext.Default.UnsubscribeRequestParams,
            McpJsonUtilities.JsonContext.Default.EmptyResult,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Invokes a tool on the server with optional arguments.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="toolName">The name of the tool to call.</param>
    /// <param name="arguments">Optional arguments for the tool.</param>
    /// <param name="serializerOptions">The serialization options governing argument serialization.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task containing the tool's response.</returns>
    public static Task<CallToolResponse> CallToolAsync(
        this IMcpClient client,
        string toolName,
        IReadOnlyDictionary<string, object?>? arguments = null,
        JsonSerializerOptions? serializerOptions = null,
        CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);
        Throw.IfNull(toolName);
        serializerOptions ??= McpJsonUtilities.DefaultOptions;
        serializerOptions.MakeReadOnly();

        return client.SendRequestAsync(
            RequestMethods.ToolsCall,
            new() { Name = toolName, Arguments = ToArgumentsDictionary(arguments, serializerOptions) },
            McpJsonUtilities.JsonContext.Default.CallToolRequestParams,
            McpJsonUtilities.JsonContext.Default.CallToolResponse,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Converts the contents of a <see cref="CreateMessageRequestParams"/> into a pair of
    /// <see cref="IEnumerable{ChatMessage}"/> and <see cref="ChatOptions"/> instances to use
    /// as inputs into a <see cref="IChatClient"/> operation.
    /// </summary>
    /// <param name="requestParams"></param>
    /// <returns>The created pair of messages and options.</returns>
    internal static (IList<ChatMessage> Messages, ChatOptions? Options) ToChatClientArguments(
        this CreateMessageRequestParams requestParams)
    {
        Throw.IfNull(requestParams);

        ChatOptions? options = null;

        if (requestParams.MaxTokens is int maxTokens)
        {
            (options ??= new()).MaxOutputTokens = maxTokens;
        }

        if (requestParams.Temperature is float temperature)
        {
            (options ??= new()).Temperature = temperature;
        }

        if (requestParams.StopSequences is { } stopSequences)
        {
            (options ??= new()).StopSequences = stopSequences.ToArray();
        }

        List<ChatMessage> messages = [];
        foreach (SamplingMessage sm in requestParams.Messages)
        {
            ChatMessage message = new()
            {
                Role = sm.Role == Role.User ? ChatRole.User : ChatRole.Assistant,
            };

            if (sm.Content is { Type: "text" })
            {
                message.Contents.Add(new TextContent(sm.Content.Text));
            }
            else if (sm.Content is { Type: "image" or "audio", MimeType: not null, Data: not null })
            {
                message.Contents.Add(new DataContent(Convert.FromBase64String(sm.Content.Data), sm.Content.MimeType));
            }
            else if (sm.Content is { Type: "resource", Resource: not null })
            {
                message.Contents.Add(sm.Content.Resource.ToAIContent());
            }

            messages.Add(message);
        }

        return (messages, options);
    }

    /// <summary>Converts the contents of a <see cref="ChatResponse"/> into a <see cref="CreateMessageResult"/>.</summary>
    /// <param name="chatResponse">The <see cref="ChatResponse"/> whose contents should be extracted.</param>
    /// <returns>The created <see cref="CreateMessageResult"/>.</returns>
    internal static CreateMessageResult ToCreateMessageResult(this ChatResponse chatResponse)
    {
        Throw.IfNull(chatResponse);

        // The ChatResponse can include multiple messages, of varying modalities, but CreateMessageResult supports
        // only either a single blob of text or a single image. Heuristically, we'll use an image if there is one
        // in any of the response messages, or we'll use all the text from them concatenated, otherwise.

        ChatMessage? lastMessage = chatResponse.Messages.LastOrDefault();

        Content? content = null;
        if (lastMessage is not null)
        {
            foreach (var lmc in lastMessage.Contents)
            {
                if (lmc is DataContent dc && (dc.HasTopLevelMediaType("image") || dc.HasTopLevelMediaType("audio")))
                {
                    content = new()
                    {
                        Type = dc.HasTopLevelMediaType("image") ? "image" : "audio",
                        MimeType = dc.MediaType,
                        Data = dc.GetBase64Data(),
                    };
                }
            }
        }

        content ??= new()
        {
            Text = lastMessage?.Text ?? string.Empty,
            Type = "text",
        };

        return new()
        {
            Content = content,
            Model = chatResponse.ModelId ?? "unknown",
            Role = lastMessage?.Role == ChatRole.User ? "user" : "assistant",
            StopReason = chatResponse.FinishReason == ChatFinishReason.Length ? "maxTokens" : "endTurn",
        };
    }

    /// <summary>
    /// Creates a sampling handler for use with <see cref="SamplingCapability.SamplingHandler"/> that will
    /// satisfy sampling requests using the specified <see cref="IChatClient"/>.
    /// </summary>
    /// <param name="chatClient">The <see cref="IChatClient"/> with which to satisfy sampling requests.</param>
    /// <returns>The created handler delegate.</returns>
    public static Func<CreateMessageRequestParams?, IProgress<ProgressNotificationValue>, CancellationToken, Task<CreateMessageResult>> CreateSamplingHandler(
        this IChatClient chatClient)
    {
        Throw.IfNull(chatClient);

        return async (requestParams, progress, cancellationToken) =>
        {
            Throw.IfNull(requestParams);

            var (messages, options) = requestParams.ToChatClientArguments();
            var progressToken = requestParams.Meta?.ProgressToken;

            List<ChatResponseUpdate> updates = [];
            await foreach (var update in chatClient.GetStreamingResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
            {
                updates.Add(update);

                if (progressToken is not null)
                {
                    progress.Report(new()
                    {
                        Progress = updates.Count,
                    });
                }
            }

            return updates.ToChatResponse().ToCreateMessageResult();
        };
    }

    /// <summary>
    /// Configures the minimum logging level for the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="level">The minimum log level of messages to be generated.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    public static Task SetLoggingLevel(this IMcpClient client, LoggingLevel level, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        return client.SendRequestAsync(
            RequestMethods.LoggingSetLevel,
            new() { Level = level },
            McpJsonUtilities.JsonContext.Default.SetLevelRequestParams,
            McpJsonUtilities.JsonContext.Default.EmptyResult,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Configures the minimum logging level for the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="level">The minimum log level of messages to be generated.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    public static Task SetLoggingLevel(this IMcpClient client, LogLevel level, CancellationToken cancellationToken = default) =>
        SetLoggingLevel(client, McpServer.ToLoggingLevel(level), cancellationToken);

    /// <summary>Convers a dictionary with <see cref="object"/> values to a dictionary with <see cref="JsonElement"/> values.</summary>
    private static IReadOnlyDictionary<string, JsonElement>? ToArgumentsDictionary(
        IReadOnlyDictionary<string, object?>? arguments, JsonSerializerOptions options)
    {
        var typeInfo = options.GetTypeInfo<object?>();

        Dictionary<string, JsonElement>? result = null;
        if (arguments is not null)
        {
            result = new(arguments.Count);
            foreach (var kvp in arguments)
            {
                result.Add(kvp.Key, kvp.Value is JsonElement je ? je : JsonSerializer.SerializeToElement(kvp.Value, typeInfo));
            }
        }

        return result;
    }
}