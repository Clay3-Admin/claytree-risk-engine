# Azure Deployment Guide – PAN Verification Function

## 1. Prerequisites

- Azure CLI installed  
- .NET SDK (matching the function runtime, typically .NET 8 isolated worker)
- Azure subscription access
- Resource group created or permissions to create one

Login:

```bash
az login
```

Set subscription:

```bash
az account set --subscription "<SUBSCRIPTION_ID>"
```

---

# 2. Azure Resources Required

- Resource Group
- Storage Account (required by Azure Functions)
- Function App
- Application Insights
- SQL Database (for RiskDb)
- Key Vault (recommended for secrets)

---

# 3. Create Resource Group

```bash
az group create \
  --name claytree-risk-rg \
  --location centralindia
```

---

# 4. Create Storage Account

Azure Functions requires a storage account.

```bash
az storage account create \
  --name claytreeriskstorage \
  --location centralindia \
  --resource-group claytree-risk-rg \
  --sku Standard_LRS
```

---

# 5. Create Application Insights

```bash
az monitor app-insights component create \
  --app claytree-risk-ai \
  --location centralindia \
  --resource-group claytree-risk-rg \
  --application-type web
```

Retrieve the connection string:

```bash
az monitor app-insights component show \
  --app claytree-risk-ai \
  --resource-group claytree-risk-rg \
  --query connectionString
```

---

# 6. Create Azure Function App

Runtime: **.NET Isolated**

```bash
az functionapp create \
  --name claytree-risk-pan-func \
  --resource-group claytree-risk-rg \
  --storage-account claytreeriskstorage \
  --consumption-plan-location centralindia \
  --runtime dotnet-isolated \
  --functions-version 4
```

Attach Application Insights:

```bash
az functionapp config appsettings set \
  --name claytree-risk-pan-func \
  --resource-group claytree-risk-rg \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="<APPINSIGHTS_CONNECTION_STRING>"
```

---

# 7. Required App Settings

Configure application configuration required by the backend.

```bash
az functionapp config appsettings set \
  --name claytree-risk-pan-func \
  --resource-group claytree-risk-rg \
  --settings \
  "ConnectionStrings__RiskDb=<SQL_CONNECTION_STRING>" \
  "PAN_PROVIDER=KARZA" \
  "PAN_PROVIDER_BASE_URL=https://api.karza.in" \
  "PAN_PROVIDER_API_KEY=<API_KEY>" \
  "PAN_PROVIDER_TIMEOUT_MS=5000"
```

Explanation:

- ConnectionStrings__RiskDb  
  Used by `DbConnectionFactory`

- PAN_PROVIDER_*  
  Used by `KarzaPanProvider`

Recommended additional settings:

```bash
"FUNCTIONS_WORKER_RUNTIME=dotnet-isolated"
"WEBSITE_RUN_FROM_PACKAGE=1"
```

---

# 8. Build and Publish the Function

From the project root:

```bash
dotnet publish -c Release
```

Create deployment zip:

```bash
cd bin/Release/net8.0/publish
zip -r deploy.zip .
```

Deploy:

```bash
az functionapp deployment source config-zip \
  --resource-group claytree-risk-rg \
  --name claytree-risk-pan-func \
  --src deploy.zip
```

---

# 9. Function Endpoint

The HTTP trigger:

```
POST /api/risk/pan/verify
```

Example:

```
https://claytree-risk-pan-func.azurewebsites.net/api/risk/pan/verify
```

Authorization: **Function Key**

Retrieve key:

```bash
az functionapp function keys list \
  --function-name verifyPanHttpTrigger \
  --name claytree-risk-pan-func \
  --resource-group claytree-risk-rg
```

---

# 10. Monitoring Setup

## Enable Logs

```bash
az webapp log config \
  --name claytree-risk-pan-func \
  --resource-group claytree-risk-rg \
  --application-logging true
```

Stream logs:

```bash
az webapp log tail \
  --name claytree-risk-pan-func \
  --resource-group claytree-risk-rg
```

---

## Application Insights Queries

Example KQL for failures:

```kql
requests
| where name contains "verifyPanHttpTrigger"
| where success == false
| order by timestamp desc
```

Provider failures:

```kql
traces
| where message contains "PROVIDER_ERROR"
```

Latency monitoring:

```kql
requests
| where name contains "verifyPanHttpTrigger"
| summarize avg(duration), p95=percentile(duration,95)
```

---

# 11. Alerts (Recommended)

Create alert for high failure rate:

```bash
az monitor metrics alert create \
  --name pan-api-failure-alert \
  --resource-group claytree-risk-rg \
  --scopes $(az functionapp show --name claytree-risk-pan-func --resource-group claytree-risk-rg --query id -o tsv) \
  --condition "avg Http5xx > 5" \
  --description "PAN verification API high failure rate"
```

---

# 12. Rollback Strategy

## Option 1 — Slot Based Deployment (Recommended)

Create staging slot:

```bash
az functionapp deployment slot create \
  --name claytree-risk-pan-func \
  --resource-group claytree-risk-rg \
  --slot staging
```

Deploy to staging:

```bash
az functionapp deployment source config-zip \
  --resource-group claytree-risk-rg \
  --name claytree-risk-pan-func \
  --slot staging \
  --src deploy.zip
```

Swap:

```bash
az functionapp deployment slot swap \
  --name claytree-risk-pan-func \
  --resource-group claytree-risk-rg \
  --slot staging \
  --target-slot production
```

Rollback:

```bash
az functionapp deployment slot swap \
  --name claytree-risk-pan-func \
  --resource-group claytree-risk-rg \
  --slot production \
  --target-slot staging
```

---

## Option 2 — Redeploy Previous Package

If slots are not used:

1. Store deployment packages in blob storage or CI artifacts
2. Redeploy previous package

```bash
az functionapp deployment source config-zip \
  --resource-group claytree-risk-rg \
  --name claytree-risk-pan-func \
  --src previous-release.zip
```

---

# 13. Security Best Practices

- Store `PAN_PROVIDER_API_KEY` in **Azure Key Vault**
- Enable **Managed Identity** for the function
- Restrict SQL access via **Private Endpoint**
- Enable **HTTPS only**

```bash
az functionapp update \
  --name claytree-risk-pan-func \
  --resource-group claytree-risk-rg \
  --https-only true
```

---

# 14. Production Architecture (Recommended)

```
Client
   │
   ▼
API Gateway / APIM
   │
   ▼
Azure Function (PanVerificationFunction)
   │
   ├── Karza PAN API
   └── Azure SQL Database (RiskDb)
   │
   ▼
Application Insights Monitoring
```

---

If you'd like, I can also generate:

- **Terraform for this infrastructure**
- **Azure DevOps CI/CD pipeline**
- **GitHub Actions deployment pipeline**
- **SQL schema for PanVerificationRepository**
- **Production-grade Karza API integration**.