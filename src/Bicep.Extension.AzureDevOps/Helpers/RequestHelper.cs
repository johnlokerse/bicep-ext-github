// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bicep.Local.Extension.Protocol;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;

namespace Bicep.Extension.AzureDevOps.Handlers;

public static class RequestHelper
{
    public static async Task<LocalExtensibilityOperationResponse> HandleRequest(JsonObject? config, string organization, Func<ServiceEndpointHttpClient, Task<LocalExtensibilityOperationResponse>> onExecuteFunc)
    {
        if (config == null)
        {
            return CreateErrorResponse("InvalidConfiguration", "Extension configuration is null. Please ensure the 'extension azuredevops with { personalAccessToken: ... }' block is properly configured.");
        }

        if (!config.ContainsKey("personalAccessToken"))
        {
            return CreateErrorResponse("InvalidConfiguration", "Personal access token is missing from configuration. Please ensure 'personalAccessToken' is provided in the extension configuration block.");
        }

        var personalAccessTokenNode = config["personalAccessToken"];
        if (personalAccessTokenNode == null)
        {
            return CreateErrorResponse("InvalidConfiguration", "Personal access token value is null. Please provide a valid personal access token.");
        }

        string personalAccessToken;
        try
        {
            personalAccessToken = personalAccessTokenNode.GetValue<string>();
        }
        catch (Exception ex)
        {
            return CreateErrorResponse("InvalidConfiguration", $"Failed to read personal access token: {ex.Message}");
        }

        if (string.IsNullOrEmpty(personalAccessToken))
        {
            return CreateErrorResponse("InvalidConfiguration", "Personal access token cannot be empty. Please provide a valid personal access token.");
        }

        var credentials = new VssBasicCredential(string.Empty, personalAccessToken);
        var organizationUrl = new Uri($"https://dev.azure.com/{organization}");

        using var connection = new VssConnection(organizationUrl, credentials);

        try
        {
            var serviceEndpointClient = connection.GetClient<ServiceEndpointHttpClient>();
            return await onExecuteFunc(serviceEndpointClient);
        }
        catch (Exception exception)
        {
            if (exception is VssServiceException vssException)
            {
                return CreateErrorResponse("VssServiceError", vssException.Message);
            }

            return CreateErrorResponse("UnhandledError", exception.Message);
        }
    }

    public static TProperties GetProperties<TProperties>(JsonObject properties)
        => properties.Deserialize<TProperties>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    public static LocalExtensibilityOperationResponse CreateSuccessResponse<TProperties, TIdentifiers>(ResourceReference request, TProperties properties, TIdentifiers identifiers)
    {
        return new(
            new(
                request.Type,
                request.ApiVersion,
                "Succeeded",
                (JsonNode.Parse(JsonSerializer.Serialize(identifiers, new JsonSerializerOptions(JsonSerializerDefaults.Web))) as JsonObject)!,
                request.Config,
                (JsonNode.Parse(JsonSerializer.Serialize(properties, new JsonSerializerOptions(JsonSerializerDefaults.Web))) as JsonObject)!),
            null);
    }

    public static LocalExtensibilityOperationResponse CreateSuccessResponse<TProperties, TIdentifiers>(ResourceSpecification request, TProperties properties, TIdentifiers identifiers)
    {
        return new(
            new(
                request.Type,
                request.ApiVersion,
                "Succeeded",
                (JsonNode.Parse(JsonSerializer.Serialize(identifiers, new JsonSerializerOptions(JsonSerializerDefaults.Web))) as JsonObject)!,
                request.Config,
                (JsonNode.Parse(JsonSerializer.Serialize(properties, new JsonSerializerOptions(JsonSerializerDefaults.Web))) as JsonObject)!),
            null);
    }

    public static LocalExtensibilityOperationResponse CreateErrorResponse(string code, string message, ErrorDetail[]? details = null, string? target = null)
    {
        return new LocalExtensibilityOperationResponse(
            null,
            new(new(code, target ?? "", message, details ?? [], [])));
    }
}
