# Guia de Execução - Sistema de Lançamentos e Consolidação

## 🚀 Quick Start

### Pré-requisitos
- Docker e Docker Compose instalados
- .NET 8 SDK (para desenvolvimento local)
- Git

### Executar com Docker Compose

```bash
# 1. Clonar ou navegar até o diretório
cd C:\code\teste\ServiceLancamentos

# 2. Parar containers anteriores (se existirem)
docker-compose down

# 3. Iniciar todos os serviços
docker-compose up --build

# 4. Aguardar inicialização completa
# Verá logs indicando:
# - ✅ PostgreSQL connected
# - ✅ RabbitMQ healthy
# - ✅ ServiceLancamentos started
# - ✅ ServiceConsolidado started
```

## 📊 Acessando os Serviços

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| **RabbitMQ Management** | http://localhost:15672 | rabbitmq-user / rabbitmq-user |
| **PostgreSQL** | localhost:5432 | postgres / postgres |
| **ServiceLancamentos** | http://localhost:5000 | N/A |
| **ServiceConsolidado** | http://localhost:5001 | N/A |

## 🧪 Testando os Serviços

### Opção 1: Usar RabbitMQ Management UI

1. Acesse http://localhost:15672
2. Autentique com `rabbitmq-user` / `rabbitmq-user`
3. Vá para **Queues** tab
4. Selecione `lancamentos.queue` ou `consolidado.queue`
5. Clique em **Publish message**
6. Cole o JSON da mensagem
7. Clique **Publish message**

### Opção 2: Usar o Producer Example (C#)

```csharp
// Editar ServiceConsolidado/Testing/ConsolidadoMessageProducer.cs
// Executar:
dotnet run --project ServiceConsolidado/Testing/ConsolidadoMessageProducer.cs
```

### Opção 3: Usar cURL/Postman

**Enviar Lançamento** (requer API endpoint):
```bash
curl -X POST http://localhost:5000/api/lancamentos \
  -H "Content-Type: application/json" \
  -d '{
    "id": "'$(uuidgen)'",
    "valor": 1000,
    "isCredito": true,
    "agenciaOrigem": "0001",
    "contaOrigem": "123456",
    "descricao": "Lançamento de teste",
    "usuario": "teste",
    "dataHora": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
  }'
```

**Enviar Consolidação** (via RabbitMQ):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "agencia": "0001",
  "conta": "123456",
  "dia": "2024-01-15T00:00:00Z"
}
```

## 📂 Visualizando Relatórios

Os relatórios JSON são salvos em volume Docker:

```bash
# Listar relatórios gerados
docker exec -it <container_id> ls -la /app/relatorios

# Copiar relatório para máquina local
docker cp <container_id>:/app/relatorios/<guid>.json ./relatorio.json

# Ou montar volume localmente (editar docker-compose.yml):
volumes:
  - ./relatorios:/app/relatorios
```

## 📝 Estrutura de Mensagens

### Lançamento (lancamentos.queue)
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "valor": 1000.00,
  "isCredito": true,
  "agenciaOrigem": "0001",
  "contaOrigem": "123456",
  "descricao": "Depósito inicial",
  "usuario": "vendedor-01",
  "dataHora": "2024-01-15T10:30:45Z"
}
```

### Consolidação (consolidado.queue)
```json
{
  "id": "660e8400-e29b-41d4-a716-446655440001",
  "agencia": "0001",
  "conta": "123456",
  "dia": "2024-01-15T00:00:00Z"
}
```

### Relatório Gerado
```json
{
  "id": "660e8400-e29b-41d4-a716-446655440001",
  "agencia": "0001",
  "conta": "123456",
  "dia": "2024-01-15T00:00:00Z",
  "saldoInicial": 0.00,
  "totalCreditos": 5000.00,
  "totalDebitos": 2000.00,
  "saldoFinal": 3000.00,
  "dataProcessamento": "2024-01-15T14:30:45.123Z"
}
```

## 🔍 Monitorando Logs

```bash
# Ver logs de todos os serviços
docker-compose logs -f

# Ver logs específicos
docker-compose logs -f service
docker-compose logs -f service-consolidado
docker-compose logs -f db
docker-compose logs -f rabbitmq
```

### Strings importantes nos logs

**ServiceLancamentos**:
- ✅ `Successfully connected to the database`
- ✅ `Processando mensagem com ID`
- ✅ `Mensagem processada com sucesso`

**ServiceConsolidado**:
- ✅ `Iniciando listener da fila consolidado`
- ✅ `Saldo consolidado calculado`
- ✅ `Relatório salvo com sucesso`

## 🛑 Parando os Serviços

```bash
# Parar sem remover (mantém dados)
docker-compose stop

# Parar e remover containers
docker-compose down

# Parar, remover e limpar volumes
docker-compose down -v
```

## 🐛 Troubleshooting

### Erro: "Connection refused"
- Verificar se PostgreSQL está healthy: `docker-compose ps`
- Aguardar 10-15 segundos após iniciar

### Erro: "RabbitMQ not ready"
- Verificar status: `docker-compose logs rabbitmq`
- Aguardar healthcheck passar (até 30 segundos)

### Mensagens não são processadas
1. Verificar se a fila existe em RabbitMQ Management UI
2. Verificar se a mensagem JSON está no formato correto
3. Verificar logs do worker: `docker-compose logs service-consolidado`

### Banco de dados vazio
- Database é criado automaticamente no startup
- Se tabelas não existem, EF Core cria com `EnsureCreated()`

## 📊 Banco de Dados

### Conectar ao PostgreSQL

```bash
docker exec -it <postgres_container_id> psql -U postgres -d lancamentos

# Listar tabelas
\dt

# Ver dados de lançamentos
SELECT * FROM "Lancamentos";

# Ver contagem
SELECT COUNT(*) FROM "Lancamentos";
```

### Backup do banco

```bash
docker exec <postgres_container_id> pg_dump -U postgres lancamentos > backup.sql
```

## 🚨 Considerações Importantes

1. **Idempotência**: Mensagens podem ser processadas múltiplas vezes
   - Use `Id` como chave primária
   - Implemente verificação de duplicata

2. **Ordem de Processamento**: Não é garantida
   - Use timestamps para ordenação
   - Implementar versionamento se necessário

3. **Performance**: Ajustar conforme carga
   - Aumentar workers
   - Configurar pool de conexões
   - Monitorar memory usage

## 📦 Desenvolvimento Local (sem Docker)

```bash
# 1. Iniciar PostgreSQL e RabbitMQ localmente (ou via Docker)
docker run -d --name postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:15
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# 2. Restaurar/Criar banco
# Executar migrations se usar Migrations

# 3. Executar os serviços
dotnet run --project ServiceLancamentos/ServiceLancamentos.csproj
dotnet run --project ServiceConsolidado/ServiceConsolidado.csproj
```

## 📚 Documentação Adicional

- [Arquitetura](./ARCHITECTURE.md)
- [ServiceLancamentos README](./ServiceLancamentos/README.md)
- [ServiceConsolidado README](./ServiceConsolidado/README.md)

---

**Dicas**: Sempre verifique os logs (`docker-compose logs`) ao enfrentar problemas!
