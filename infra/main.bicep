targetScope = 'resourceGroup'

param location string = resourceGroup().location
param functionAppName string = 'event-payment-func'
param eventGridTopicName string = 'fintech-transaction-${uniqueString(resourceGroup().id)}'


var storageAccountName = 'payapi${uniqueString(resourceGroup().id)}'
var appInsightsName = '${functionAppName}-insights'

// Always use Consumption Plan (Y1) for minimal cost
resource storage 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: take(storageAccountName, 24)
  location: location
  sku: {
    name: 'Standard_LRS'  // Cheapest replication
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Cool'  // Cheaper storage tier
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
  }
}

// Lifecycle policy to auto-delete old logs (reduce storage costs)
resource lifecyclePolicy 'Microsoft.Storage/storageAccounts/managementPolicies@2022-09-01' = {
  name: 'default'
  parent: storage
  properties: {
    policy: {
      rules: [
        {
          name: 'DeleteOldLogs'
          enabled: true
          type: 'Lifecycle'
          definition: {
            actions: {
              baseBlob: {
                delete: {
                  daysAfterModificationGreaterThan: 30
                }
              }
            }
            filters: {
              blobTypes: ['blockBlob']
              prefixMatch: ['azure-webjobs-hosts']
            }
          }
        }
      ]
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    RetentionInDays: 30  // Minimum retention to reduce costs
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}
// Consumption Plan (Y1) - Pay per execution, no base cost
resource plan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: 'funcPlan'
  location: location
  sku: {
    name: 'Y1'      // Consumption Plan (FREE tier with generous limits)
    tier: 'Dynamic' // Pay-per-execution
  }
  kind: 'functionapp'
}

resource functionApp 'Microsoft.Web/sites@2022-03-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: plan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${az.environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${az.environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionAppName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
      ]
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      functionAppScaleLimit: 5  // Max 5 concurrent instances
    }
    httpsOnly: true
  }
}

//
// 🟦 Add Service Bus namespace and queue
//

param serviceBusNamespaceName string = 'fintech-sb-${uniqueString(resourceGroup().id)}'

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: serviceBusNamespaceName
  location: location
  sku: {
    name: 'Basic' // use Standard if you need topics
    tier: 'Basic'
  }
}

resource serviceBusQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'transactions'
  properties: {
    enablePartitioning: true
  }
}

//
// 🟩 Output connection string so you can use it in local.settings.json
//
// output serviceBusConnectionString string = listKeys(
//   resourceId('Microsoft.ServiceBus/namespaces/AuthorizationRules', serviceBusNamespace.name, 'RootManageSharedAccessKey'),
//   '2022-10-01-preview'
// ).primaryConnectionString


resource eventGridTopic 'Microsoft.EventGrid/topics@2025-02-15' = {
  name: eventGridTopicName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    inputSchema: 'CloudEventSchemaV1_0'
  }
}


output eventGridTopicEndpoint string = eventGridTopic.properties.endpoint
output eventGridTopicKey string = listKeys(eventGridTopic.id, '2025-02-15').key1
