#!/bin/bash
#
# Cria a infraestrutura do FloodShield Doações na Azure.
# Executado pelo stage "Infraestrutura" do azure-pipeline.yml, que passa os parâmetros abaixo.
# Os comandos podem rodar de novo sem problema: se o recurso já existe, ele é mantido.
#

# Parâmetros (variáveis do pipeline)
WEBAPP_NAME=$1
SQL_SERVER=$2
SQL_DB=$3
SQL_ADMIN_USER=$4
SQL_ADMIN_PASS=$5

# Variáveis
RESOURCE_GROUP="rg-floodshield-gs"
LOCATION_SQL="centralus"
LOCATION_APP="westus3"          # única região com cota de App Service na assinatura
APP_SERVICE_PLAN="asp-floodshield-gs"
RUNTIME="DOTNETCORE:10.0"


# Registrar os Serviços utilizados
az provider register --namespace Microsoft.Web --wait
az provider register --namespace Microsoft.Sql --wait

# Criação do Grupo de Recursos
az group create --name $RESOURCE_GROUP --location $LOCATION_SQL

# Banco de Dados Azure SQL (PaaS)
az sql server create \
  --name $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION_SQL \
  --admin-user "$SQL_ADMIN_USER" \
  --admin-password "$SQL_ADMIN_PASS"

az sql db create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name $SQL_DB \
  --service-objective Basic \
  --backup-storage-redundancy Local

# Libera o acesso ao banco para os serviços da Azure (Web App)
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Criar o Plano de Serviço (F1 gratuito: a assinatura não tem cota para B1) e o Serviço de Aplicativo
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION_APP \
  --sku F1 \
  --is-linux

az webapp create \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "$RUNTIME"
