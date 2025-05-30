// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Text.Json;
using System.Text.Json.Nodes;
using Bicep.Local.Extension.Protocol;
using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Bicep.Extension.AzureDevOps.Handlers;

public class ServiceConnectionResourceHandler : IResourceHandler
{
    public string ResourceType => "ServiceConnection";

    private record Identifiers(
        string? Organization,
        string? Name);

    public Task<LocalExtensibilityOperationResponse> Delete(ResourceReference request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<LocalExtensibilityOperationResponse> Get(ResourceReference request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<LocalExtensibilityOperationResponse> Preview(ResourceSpecification request, CancellationToken cancellationToken)
        => HandleServiceConnectionRequest(request, isPreview: true);

    public Task<LocalExtensibilityOperationResponse> CreateOrUpdate(ResourceSpecification request, CancellationToken cancellationToken)
        => HandleServiceConnectionRequest(request, isPreview: false);

    private Task<LocalExtensibilityOperationResponse> HandleServiceConnectionRequest(ResourceSpecification request, bool isPreview)
    {
        var properties = RequestHelper.GetProperties<Types.AzureDevOps.Models.ServiceConnection>(request.Properties);

        // Try to get PAT from config, but fallback to properties.PersonalAccessToken if config is null
        string? pat = null;
        if (request.Config != null && request.Config.TryGetPropertyValue("personalAccessToken", out var patNode))
        {
            pat = patNode?.GetValue<string>();
        }
        if (string.IsNullOrEmpty(pat) && !string.IsNullOrEmpty(properties.PersonalAccessToken))
        {
            pat = properties.PersonalAccessToken;
        }

        if (string.IsNullOrEmpty(properties.Organization))
        {
            return Task.FromResult(RequestHelper.CreateErrorResponse("InvalidConfiguration", "Organization is required"));
        }
        if (string.IsNullOrEmpty(pat))
        {
            return Task.FromResult(RequestHelper.CreateErrorResponse("InvalidConfiguration", "Personal access token is required for authentication."));
        }

        // Build a config object to pass to RequestHelper
        var config = new System.Text.Json.Nodes.JsonObject
        {
            ["personalAccessToken"] = pat
        };

        return RequestHelper.HandleRequest(config, properties.Organization, async client =>
        {
            if (isPreview)
            {
                await Task.Yield();
                return RequestHelper.CreateSuccessResponse(request, properties, new Identifiers(properties.Organization, properties.Name));
            }

            var serviceEndpoint = CreateServiceEndpointFromProperties(properties);
            try
            {
                var createdEndpoint = await client.CreateServiceEndpointAsync(serviceEndpoint, properties.ProjectId);
                properties.Name = createdEndpoint.Name;
                return RequestHelper.CreateSuccessResponse(request, properties, new Identifiers(properties.Organization, properties.Name));
            }
            catch (Exception ex)
            {
                return RequestHelper.CreateErrorResponse("ServiceEndpointCreationFailed", $"Failed to create service endpoint: {ex.Message}");
            }
        });
    }

    private ServiceEndpoint CreateServiceEndpointFromProperties(Types.AzureDevOps.Models.ServiceConnection properties)
    {
        var endpoint = new ServiceEndpoint
        {
            Name = properties.Name,
            Description = properties.Description,
            Url = !string.IsNullOrEmpty(properties.Url) ? new Uri(properties.Url) :
                  !string.IsNullOrEmpty(GetDefaultUrlForType(properties.Type)) ? new Uri(GetDefaultUrlForType(properties.Type)) : null,
            Type = GetServiceEndpointType(properties.Type),
            IsShared = properties.IsShared ?? false,
            IsReady = true,
            Owner = "library"
        };

        // Set up authorization based on the scheme
        var authorizationParameters = new Dictionary<string, string>();
        var authorizationScheme = GetAuthorizationScheme(properties.AuthorizationScheme, properties.Type);

        switch (authorizationScheme?.ToLowerInvariant())
        {
            case "serviceprincipal":
                if (!string.IsNullOrEmpty(properties.TenantId))
                {
                    authorizationParameters["tenantid"] = properties.TenantId;
                }
                if (!string.IsNullOrEmpty(properties.ServicePrincipalId))
                {
                    authorizationParameters["serviceprincipalid"] = properties.ServicePrincipalId;
                }
                if (!string.IsNullOrEmpty(properties.ServicePrincipalKey))
                {
                    authorizationParameters["serviceprincipalkey"] = properties.ServicePrincipalKey;
                    authorizationParameters["authenticationType"] = "spnKey";
                }
                break;

            case "workloadidentityfederation":
                if (!string.IsNullOrEmpty(properties.TenantId))
                {
                    authorizationParameters["tenantid"] = properties.TenantId;
                }
                if (!string.IsNullOrEmpty(properties.ServicePrincipalId))
                {
                    authorizationParameters["serviceprincipalid"] = properties.ServicePrincipalId;
                }
                break;

            case "usernamepassword":
                if (!string.IsNullOrEmpty(properties.Username))
                {
                    authorizationParameters["username"] = properties.Username;
                }
                if (!string.IsNullOrEmpty(properties.Password))
                {
                    authorizationParameters["password"] = properties.Password;
                }
                break;

            case "personalaccesstoken":
                if (!string.IsNullOrEmpty(properties.PersonalAccessToken))
                {
                    authorizationParameters["accessToken"] = properties.PersonalAccessToken;
                }
                break;
        }

        endpoint.Authorization = new EndpointAuthorization
        {
            Scheme = authorizationScheme,
            Parameters = authorizationParameters
        };

        // Set up data properties for Azure RM connections
        if (properties.Type == Types.AzureDevOps.Models.ServiceConnectionType.AzureRM)
        {
            endpoint.Data = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(properties.SubscriptionId))
            {
                endpoint.Data["subscriptionId"] = properties.SubscriptionId;
            }
            if (!string.IsNullOrEmpty(properties.SubscriptionName))
            {
                endpoint.Data["subscriptionName"] = properties.SubscriptionName;
            }
            if (!string.IsNullOrEmpty(properties.Environment))
            {
                endpoint.Data["environment"] = properties.Environment;
            }
            if (!string.IsNullOrEmpty(properties.ScopeLevel))
            {
                endpoint.Data["scopeLevel"] = properties.ScopeLevel;
            }
            if (!string.IsNullOrEmpty(properties.CreationMode))
            {
                endpoint.Data["creationMode"] = properties.CreationMode;
            }
        }

        // Set up project references if projectId and projectName are provided
        if (!string.IsNullOrEmpty(properties.ProjectId) && !string.IsNullOrEmpty(properties.ProjectName))
        {
            endpoint.ProjectReferences = new List<ProjectReference>
            {
                new ProjectReference
                {
                    Id = properties.ProjectId,
                    Name = properties.ProjectName
                }
            };
        }

        return endpoint;
    }

    private string GetServiceEndpointType(Types.AzureDevOps.Models.ServiceConnectionType? type)
    {
        return type switch
        {
            Types.AzureDevOps.Models.ServiceConnectionType.AzureRM => "AzureRM",
            Types.AzureDevOps.Models.ServiceConnectionType.Generic => "Generic",
            Types.AzureDevOps.Models.ServiceConnectionType.Kubernetes => "Kubernetes",
            Types.AzureDevOps.Models.ServiceConnectionType.DockerRegistry => "dockerregistry",
            Types.AzureDevOps.Models.ServiceConnectionType.GitHub => "github",
            Types.AzureDevOps.Models.ServiceConnectionType.Azure => "Azure",
            _ => "Generic"
        };
    }

    private string GetDefaultUrlForType(Types.AzureDevOps.Models.ServiceConnectionType? type)
    {
        return type switch
        {
            Types.AzureDevOps.Models.ServiceConnectionType.AzureRM => "https://management.azure.com/",
            Types.AzureDevOps.Models.ServiceConnectionType.Azure => "https://management.azure.com/",
            _ => ""
        };
    }

    private string GetAuthorizationScheme(Types.AzureDevOps.Models.AuthorizationScheme? scheme, Types.AzureDevOps.Models.ServiceConnectionType? type)
    {
        // If scheme is explicitly set, use it
        if (scheme.HasValue)
        {
            return scheme.Value switch
            {
                Types.AzureDevOps.Models.AuthorizationScheme.ServicePrincipal => "ServicePrincipal",
                Types.AzureDevOps.Models.AuthorizationScheme.WorkloadIdentityFederation => "WorkloadIdentityFederation",
                Types.AzureDevOps.Models.AuthorizationScheme.UsernamePassword => "UsernamePassword",
                Types.AzureDevOps.Models.AuthorizationScheme.OAuth => "OAuth",
                Types.AzureDevOps.Models.AuthorizationScheme.PersonalAccessToken => "PersonalAccessToken",
                Types.AzureDevOps.Models.AuthorizationScheme.Certificate => "Certificate",
                _ => "UsernamePassword"
            };
        }

        // Default based on connection type
        return type switch
        {
            Types.AzureDevOps.Models.ServiceConnectionType.AzureRM => "ServicePrincipal",
            Types.AzureDevOps.Models.ServiceConnectionType.Azure => "ServicePrincipal",
            _ => "UsernamePassword"
        };
    }
}
