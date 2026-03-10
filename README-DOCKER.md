# Docker Development Guide - NMJ Backend

## 🐳 Comandos Principais

### Iniciar/Parar Containers

```bash
# Iniciar (primeira vez ou após mudanças no código)
docker-compose up -d --build

# Iniciar (sem rebuild)
docker-compose up -d

# Parar containers
docker-compose down

# Parar e remover volumes (limpa banco de dados)
docker-compose down -v
```

### Ver Logs

```bash
# Logs da API em tempo real
docker-compose logs -f api

# Logs do SQL Server
docker-compose logs -f sqlserver

# Últimas 50 linhas
docker logs nmj-api --tail 50
```

### Status dos Containers

```bash
# Ver containers rodando
docker ps

# Ver todos os containers (incluindo parados)
docker ps -a
```

## 🔧 Executar Comandos Dotnet no Container

Agora o container da API tem o **SDK do .NET** instalado, permitindo executar comandos `dotnet ef`:

### Migrations

```bash
# Criar nova migration
docker exec nmj-api dotnet ef migrations add NomeDaMigration --project /src/APIServer/APIServer.csproj

# Aplicar migrations
docker exec nmj-api dotnet ef database update --project /src/APIServer/APIServer.csproj

# Listar migrations
docker exec nmj-api dotnet ef migrations list --project /src/APIServer/APIServer.csproj

# Reverter última migration
docker exec nmj-api dotnet ef database update PreviousMigrationName --project /src/APIServer/APIServer.csproj

# Remover última migration (se não aplicada)
docker exec nmj-api dotnet ef migrations remove --project /src/APIServer/APIServer.csproj
```

### Outros Comandos Dotnet

```bash
# Build
docker exec nmj-api dotnet build /src/APIServer/APIServer.csproj

# Restore packages
docker exec nmj-api dotnet restore /src/APIServer/APIServer.csproj

# Executar testes
docker exec nmj-api dotnet test /src/APIServer/APIServer.csproj
```

### Acessar Shell do Container

```bash
# Bash interativo
docker exec -it nmj-api bash

# Dentro do container, você pode executar qualquer comando dotnet:
dotnet --version
dotnet ef --version
cd /src/APIServer && dotnet build
```

## 🗄️ SQL Server no Docker

### Conectar ao SQL Server

```bash
# Via sqlcmd dentro do container
docker exec -it nmj-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Password123" -C

# Da sua máquina local (se tiver sqlcmd instalado)
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Password123" -C
```

### Connection Strings

**Dentro do Docker** (container → container):

```
Server=sqlserver,1433;Database=nmj;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True
```

**Da sua máquina** (localhost → Docker):

```
Server=localhost,1433;Database=nmj;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True
```

## 🔄 Workflow de Desenvolvimento

### Opção 1: Desenvolvimento com Docker (Recomendado para testes completos)

```bash
# 1. Fazer mudanças no código
# 2. Rebuild e restart
docker-compose down
docker-compose up -d --build

# 3. Ver logs
docker-compose logs -f api
```

### Opção 2: Desenvolvimento Local (Mais rápido para debug)

```bash
# 1. Garantir que SQL Server está rodando no Docker
docker-compose up -d sqlserver

# 2. Rodar API localmente
cd APIServer
dotnet run

# A API vai conectar ao SQL Server no Docker (porta 1433)
```

### Opção 3: Híbrido (Melhor dos dois mundos)

```bash
# SQL Server no Docker
docker-compose up -d sqlserver

# API local com hot-reload
cd APIServer
dotnet watch run

# Quando quiser testar no ambiente completo
docker-compose up -d --build
```

## 📝 Aplicar Mudanças no Banco

### Após criar uma nova Migration

```bash
# 1. Criar migration localmente (mais rápido)
cd APIServer
dotnet ef migrations add NomeDaMigration

# 2. Aplicar no Docker
docker exec nmj-api dotnet ef database update --project /src/APIServer/APIServer.csproj

# OU rebuild o container (aplica automaticamente)
docker-compose down
docker-compose up -d --build
```

## 🐛 Troubleshooting

### Container não inicia

```bash
# Ver logs de erro
docker logs nmj-api

# Verificar se SQL Server está healthy
docker ps
# STATUS deve mostrar "healthy" para nmj-sqlserver
```

### Erro de conexão com banco

```bash
# Verificar se SQL Server está rodando
docker ps | grep sqlserver

# Testar conexão
docker exec nmj-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Password123" -Q "SELECT 1" -C
```

### Limpar tudo e recomeçar

```bash
# Para containers e remove volumes
docker-compose down -v

# Remove imagens antigas
docker rmi backend-api

# Rebuild do zero
docker-compose up -d --build
```

## 📊 Diferenças: SDK vs Runtime

### Antes (Runtime apenas)

- ✅ Menor tamanho da imagem (~200MB)
- ✅ Mais seguro (menos ferramentas)
- ❌ Não pode executar `dotnet ef`
- ❌ Não pode fazer build dentro do container

### Agora (SDK completo)

- ✅ Pode executar `dotnet ef` e migrations
- ✅ Pode fazer build e debug
- ✅ Ferramentas de desenvolvimento disponíveis
- ⚠️ Imagem maior (~700MB)
- ⚠️ Não recomendado para produção

### Para Produção

Use o `docker-compose.yml` original com runtime apenas. Para desenvolvimento, use este setup com SDK.

## 🚀 Comandos Úteis Resumidos

```bash
# Iniciar tudo
docker-compose up -d --build

# Ver logs
docker-compose logs -f api

# Aplicar migration
docker exec nmj-api dotnet ef database update --project /src/APIServer/APIServer.csproj

# Acessar container
docker exec -it nmj-api bash

# Parar tudo
docker-compose down

# Limpar tudo
docker-compose down -v && docker rmi backend-api
```
