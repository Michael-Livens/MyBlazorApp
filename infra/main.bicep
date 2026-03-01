@description('The location for all resources.')
param location string = resourceGroup().location

@description('The base name for resources.')
param baseName string = 'agntc${uniqueString(resourceGroup().id)}'

@description('The SQL administrator username.')
param sqlAdministratorLogin string

@description('The SQL administrator password.')
@secure()
param sqlAdministratorLoginPassword string

// --- Variables ---
var hostingPlanName = '${baseName}-plan'
var webSiteName = '${baseName}-web'
var sqlServerName = '${baseName}-sql'
var databaseName = 'AgenticBlazerDb'
var keyVaultName = '${baseName}-kv'
var storageAccountName = 'agntc${substring(uniqueString(resourceGroup().id), 0, 8)}st'

// --- Resources ---

// 1. Storage Account (Azure Blob Storage)
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
  }
}

// 2. SQL Server & Database
resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorLoginPassword
  }
}

resource sqlFirewallRule 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDB 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
}

// 3. App Service Plan & Web App
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'F1'        // Free tier
    tier: 'Free'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2022-09-01' = {
  name: webSiteName
  location: location
  identity: {
    type: 'SystemAssigned' // Grants the app its own Managed Identity
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0' // Align with .NET 10
      appSettings: [
        {
          name: 'KeyVaultName'
          value: keyVaultName
        }
        {
          name: 'AzureStorageBlob__Endpoint'
          value: storageAccount.properties.primaryEndpoints.blob
        }
        {
          name: 'DefaultConnection'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${databaseName};User Id=${sqlAdministratorLogin};Password=${sqlAdministratorLoginPassword};Encrypt=True;Connection Timeout=30;'
        }
      ]
    }
  }
}

// 4. Azure Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: webApp.identity.principalId // Grant Web App Access to Key Vault
        permissions: {
          secrets: [
            'get'
            'list'
            'set'
            'delete'
          ]
        }
      }
    ]
  }
}

// 5. Role Assignment for Storage Blob Data Contributor (for Web App MI)
resource storageBlobDataContributorRole 'Microsoft.Authorization/roleDefinitions@2022-04-01-preview' existing = {
  scope: subscription()
  name: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe' // Storage Blob Data Contributor
}

resource rawStorageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01-preview' = {
  name: guid(storageAccount.id, webApp.id, storageBlobDataContributorRole.id)
  scope: storageAccount
  properties: {
    roleDefinitionId: storageBlobDataContributorRole.id
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output webAppUrl string = webApp.properties.defaultHostName
output webAppName string = webApp.name
