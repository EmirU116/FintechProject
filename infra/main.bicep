targetScope = 'resourceGroup'

param location string = resourceGroup().location
param functionAppName string = 'event-payment-func'

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
        // Event Grid Topic settings will be appended via config resource below
      ]
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      functionAppScaleLimit: 5  // Max 5 concurrent instances
    }
    httpsOnly: true
  }
}

//
// 🟦 Service Bus for critical payment processing
//

@description('Service Bus namespace for critical/high-value payment processing')
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: 'fintech-sb-${uniqueString(resourceGroup().id)}'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
}

@description('Service Bus queue for critical payments with DLQ and duplicate detection')
resource serviceBusQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'critical-payments'
  properties: {
    maxDeliveryCount: 10
    lockDuration: 'PT5M'
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    enablePartitioning: false
    deadLetteringOnMessageExpiration: true
    maxSizeInMegabytes: 1024
  }
}

@description('Service Bus authorization rule for Function App access')
resource serviceBusAuthRule 'Microsoft.ServiceBus/namespaces/authorizationRules@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'FunctionAppAccess'
  properties: {
    rights: [
      'Send'
      'Listen'
      'Manage'
    ]
  }
}

//
// 🟩 Storage Queue for standard payment processing (lower idle cost)
//
resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2022-09-01' = {
  name: 'default'
  parent: storage
}

resource transactionsQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2022-09-01' = {
  name: 'transactions'
  parent: queueService
}

//
// Storage connection string for local dev
// Note: Avoid outputting secrets (AccountKey). Use `az storage account show-connection-string` locally.

// 🟧 Event Grid Topic for domain events
@description('Event Grid custom topic for transaction domain events')
resource eventGridTopic 'Microsoft.EventGrid/topics@2022-06-15' = {
  name: 'fintech-transactions-${uniqueString(resourceGroup().id)}'
  location: location
  properties: {
    inputSchema: 'EventGridSchema'
    publicNetworkAccess: 'Enabled'
  }
}

var eventGridEndpoint = 'https://${eventGridTopic.name}.eventgrid.azure.net/api/events'

// Apply settings to Function App
// Assign Event Grid Event Publisher role to the Function App's system-assigned identity
resource functionIdentity 'Microsoft.Web/sites@2022-03-01' existing = {
  name: functionAppName
}

@description('Grant Event Publisher role to Function App MSI on the Event Grid topic scope')
resource eventPublisherRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(eventGridTopic.id, 'eventPublisherRole')
  scope: eventGridTopic
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e467ea0-369d-4d8b-b3d8-5b28b9fa7d2e')
    principalId: functionIdentity.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Set Event Grid endpoint and Service Bus connection in app settings
resource functionAppConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: functionApp
  name: 'appsettings'
  properties: {
    'EventGrid:TopicEndpoint': eventGridEndpoint
    ServiceBusConnection__fullyQualifiedNamespace: '${serviceBusNamespace.name}.servicebus.windows.net'
    ServiceBusConnectionString: serviceBusAuthRule.listKeys().primaryConnectionString
  }
}

// Outputs for reference
output serviceBusNamespace string = serviceBusNamespace.name
output serviceBusQueueName string = serviceBusQueue.name
output storageAccountName string = storage.name
output eventGridTopicName string = eventGridTopic.name
