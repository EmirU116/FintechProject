targetScope = 'resourceGroup'

param location string = resourceGroup().location
param functionAppName string = 'event-payment-func'

resource storage 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: 'payapi${uniqueString(resourceGroup().id)}'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

resource plan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: 'funcPlan'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

resource functionApp 'Microsoft.Web/sites@2022-03-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: plan.id
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
output serviceBusConnectionString string = listKeys(
  resourceId('Microsoft.ServiceBus/namespaces/AuthorizationRules', serviceBusNamespace.name, 'RootManageSharedAccessKey'),
  '2022-10-01-preview'
).primaryConnectionString
