targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment used to generate a unique resource token')
param environmentName string

@description('Primary location for all resources')
param location string

@description('OpenAI chat model name')
param chatModelName string = 'gpt-4o'

@description('OpenAI chat model version')
param chatModelVersion string = '2024-11-20'

@description('OpenAI embedding model name')
param embeddingModelName string = 'text-embedding-3-small'

@description('OpenAI embedding model version')
param embeddingModelVersion string = '1'

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module identity './identity.bicep' = {
  name: 'identity'
  scope: rg
  params: {
    location: location
    tags: tags
    identityName: '${abbrs.managedIdentityUserAssignedIdentities}${resourceToken}'
  }
}

module monitoring './monitoring.bicep' = {
  name: 'monitoring'
  scope: rg
  params: {
    location: location
    tags: tags
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
  }
}

module cosmos './cosmos.bicep' = {
  name: 'cosmos'
  scope: rg
  params: {
    location: location
    tags: tags
    accountName: '${abbrs.documentDBDatabaseAccounts}${resourceToken}'
    databaseName: 'classicrag'
    containerName: 'properties'
    leaseContainerName: 'leases'
    principalId: identity.outputs.principalId
  }
}

module search './search.bicep' = {
  name: 'search'
  scope: rg
  params: {
    location: location
    tags: tags
    searchServiceName: '${abbrs.searchSearchServices}${resourceToken}'
    principalId: identity.outputs.principalId
  }
}

module openai './openai.bicep' = {
  name: 'openai'
  scope: rg
  params: {
    location: location
    tags: tags
    openAiName: '${abbrs.cognitiveServicesAccounts}${resourceToken}'
    chatModelName: chatModelName
    chatModelVersion: chatModelVersion
    embeddingModelName: embeddingModelName
    embeddingModelVersion: embeddingModelVersion
    principalId: identity.outputs.principalId
  }
}

module registry './registry.bicep' = {
  name: 'registry'
  scope: rg
  params: {
    location: location
    tags: tags
    registryName: '${abbrs.containerRegistryRegistries}${resourceToken}'
    principalId: identity.outputs.principalId
  }
}

module app './app.bicep' = {
  name: 'app'
  scope: rg
  params: {
    location: location
    tags: tags
    containerAppName: '${abbrs.appContainerApps}${resourceToken}'
    containerAppsEnvironmentName: '${abbrs.appManagedEnvironments}${resourceToken}'
    containerRegistryName: registry.outputs.name
    logAnalyticsWorkspaceName: monitoring.outputs.logAnalyticsWorkspaceName
    identityId: identity.outputs.id
    identityClientId: identity.outputs.clientId
    applicationInsightsConnectionString: monitoring.outputs.applicationInsightsConnectionString
    cosmosDbEndpoint: cosmos.outputs.endpoint
    searchEndpoint: search.outputs.endpoint
    openAiEndpoint: openai.outputs.endpoint
    chatModelDeploymentName: openai.outputs.chatDeploymentName
    embeddingModelDeploymentName: openai.outputs.embeddingDeploymentName
  }
}

module web './web.bicep' = {
  name: 'web'
  scope: rg
  params: {
    location: location
    tags: tags
    containerAppName: 'ca-web-${resourceToken}'
    containerAppsEnvironmentName: '${abbrs.appManagedEnvironments}${resourceToken}'
    containerRegistryName: registry.outputs.name
    identityId: identity.outputs.id
    apiBaseUrl: app.outputs.uri
  }
}

output AZURE_COSMOSDB_ENDPOINT string = cosmos.outputs.endpoint
output AZURE_SEARCH_ENDPOINT string = search.outputs.endpoint
output AZURE_OPENAI_ENDPOINT string = openai.outputs.endpoint
output AZURE_OPENAI_CHAT_DEPLOYMENT string = openai.outputs.chatDeploymentName
output AZURE_OPENAI_EMBEDDING_DEPLOYMENT string = openai.outputs.embeddingDeploymentName
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = registry.outputs.loginServer
output AZURE_CONTAINER_REGISTRY_NAME string = registry.outputs.name
output AZURE_CLIENT_ID string = identity.outputs.clientId
output SERVICE_API_NAME string = app.outputs.name
output SERVICE_API_URI string = app.outputs.uri
output SERVICE_WEB_NAME string = web.outputs.name
output SERVICE_WEB_URI string = web.outputs.uri
