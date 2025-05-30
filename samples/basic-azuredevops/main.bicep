targetScope = 'local'

@secure()
param patToken string

extension azuredevops with {
    personalAccessToken: patToken
}

resource resServiceConnection 'ServiceConnection' = {
    name: 'local-deploy-bicep'
    organization: 'john-lokerse'
    projectName: 'DevOps-Playground'
    authorizationScheme: 'WorkloadIdentityFederation'
    scopeLevel: 'Subscription'
    subscriptionName: 'Visual Studio Enterprise Subscription'
    subscriptionId: '94a06e28-fc8c-4fad-9963-717577c5dd93'
    tenantId: '2fd31e1e-1612-45b8-94c4-a472a78267f0'
    type: 'AzureRM'
    projectId: '669169bc-2290-4140-8154-2632838e273e'
}
