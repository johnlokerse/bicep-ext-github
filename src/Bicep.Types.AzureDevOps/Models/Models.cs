using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Serialization;
using Azure.Bicep.Types;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Azure.Bicep.Types.Serialization;

namespace Bicep.Types.AzureDevOps.Models;

[AttributeUsage(AttributeTargets.Property)]
public class TypeAnnotationAttribute : Attribute
{
    public TypeAnnotationAttribute(
        string? description,
        ObjectTypePropertyFlags flags = ObjectTypePropertyFlags.None,
        bool isSecure = false)
    {
        Description = description;
        Flags = flags;
        IsSecure = isSecure;
    }

    public string? Description { get; }

    public ObjectTypePropertyFlags Flags { get; }

    public bool IsSecure { get; }
}

public enum ServiceConnectionType
{
    AzureRM,
    Generic,
    Kubernetes,
    DockerRegistry,
    GitHub,
    Azure
}

public enum AuthorizationScheme
{
    ServicePrincipal,
    WorkloadIdentityFederation,
    UsernamePassword,
    OAuth,
    PersonalAccessToken,
    Certificate
}

public class ServiceConnection
{
    [TypeAnnotation("The Azure DevOps organization name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string? Organization { get; set; }

    [TypeAnnotation("The service connection name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string? Name { get; set; }

    [TypeAnnotation("The project ID where the service connection will be created", ObjectTypePropertyFlags.Required)]
    public string? ProjectId { get; set; }

    [TypeAnnotation("The project name where the service connection will be created", ObjectTypePropertyFlags.Required)]
    public string? ProjectName { get; set; }

    [TypeAnnotation("The type of service connection")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ServiceConnectionType? Type { get; set; }

    [TypeAnnotation("The service connection description")]
    public string? Description { get; set; }

    [TypeAnnotation("The URL of the service endpoint")]
    public string? Url { get; set; }

    [TypeAnnotation("Whether the service connection is shared with other projects")]
    public bool? IsShared { get; set; }

    [TypeAnnotation("The authorization scheme")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AuthorizationScheme? AuthorizationScheme { get; set; }

    [TypeAnnotation("The tenant ID for Azure service connections")]
    public string? TenantId { get; set; }

    [TypeAnnotation("The service principal ID for Azure service connections")]
    public string? ServicePrincipalId { get; set; }

    [TypeAnnotation("The service principal key for Azure service connections", isSecure: true)]
    public string? ServicePrincipalKey { get; set; }

    [TypeAnnotation("The subscription ID for Azure service connections")]
    public string? SubscriptionId { get; set; }

    [TypeAnnotation("The subscription name for Azure service connections")]
    public string? SubscriptionName { get; set; }

    [TypeAnnotation("The Azure environment (AzureCloud, AzureUSGovernment, etc.)")]
    public string? Environment { get; set; }

    [TypeAnnotation("The scope level (Subscription, ManagementGroup)")]
    public string? ScopeLevel { get; set; }

    [TypeAnnotation("The creation mode (Manual, Automatic)")]
    public string? CreationMode { get; set; }

    [TypeAnnotation("Username for basic authentication", isSecure: true)]
    public string? Username { get; set; }

    [TypeAnnotation("Password for basic authentication", isSecure: true)]
    public string? Password { get; set; }

    [TypeAnnotation("Personal access token for token-based authentication", isSecure: true)]
    public string? PersonalAccessToken { get; set; }
}
