
# Arquitetura Teste — Microserviços de Lançamentos e Consolidação

Este repositório contém uma arquitetura de microserviços em .NET 8 para processamento assíncrono de lançamentos e geração de relatórios consolidados.

Projetos principais
- ServiceLancamentos — Worker + API para processar lançamentos (porta 5000)
- ServiceConsolidado — Worker para consolidação diária e geração de relatórios (porta 5001)
- ApiGateway — API com endpoints públicos (POST /api/lancamentos, POST /api/consolidado, GET /api/relatorio/{file}) e Swagger (porta 5002)
- Lancamentos.Library — biblioteca compartilhada (modelos, repositório, serviços)
- RabbitMqMessage — biblioteca compartilhada para integração com RabbitMQ

Visão geral de execução
1. Mensagens de lançamento ou consolidado são publicadas em exchanges/filas do RabbitMQ.
2. Workers (ServiceLancamentos e ServiceConsolidado) consomem, processam e persistem em PostgreSQL ou salvam relatórios JSON.
3. ApiGateway expõe endpoints para publicar mensagens e recuperar relatórios gerados.

Como executar (Docker)
1. No diretório raiz, pare containers antigos:

```powershell
docker-compose down
```

2. Suba todos os serviços:

```powershell
docker-compose up --build -d
```

Serviços e acessos
- RabbitMQ Management UI: http://localhost:15672 (usuário/senha: rabbitmq-user / rabbitmq-user)
- PostgreSQL: host=db (no container), porta externa 5432, db=lancamentos, user=postgres, password=postgres
- ServiceLancamentos API: http://localhost:5000/
- ServiceConsolidado API: http://localhost:5001/
- ApiGateway + Swagger UI: http://localhost:5002/ (Swagger na raiz)

Observações de configuração
- No ambiente Docker os serviços usam host `rabbitmq` para conectar ao broker.
- Relatórios gerados pelo ServiceConsolidado são salvos em /app/relatorios e expostos via volume `relatorios-data`.

Como testar
- Enviar mensagem via RabbitMQ Management UI (Queues → Publish message) ou usar os endpoints do ApiGateway.

Exemplos de payloads
- Lancamento (RabbitMQRequest)

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "valor": 1000.00,
  "isCredito": true,
  "agenciaOrigem": "0001",
  "contaOrigem": "123456",
  "descricao": "Depósito inicial",
  "usuario": "usuario",
  "dataHora": "2026-01-01T00:00:00Z"
}
```

- Consolidado (ConsolidadoRequest)

```json
{
  "id": "660e8400-e29b-41d4-a716-446655440001",
  "agencia": "0001",
  "conta": "123456",
  "dia": "2026-01-15T00:00:00Z"
}
```

Recuperar relatório
- Após processamento, o relatório JSON será salvo em /app/relatorios/{GUID}.json; para copiar para host:

```powershell
docker cp <container_id>:/app/relatorios/<guid>.json ./relatorio.json
```

Logs e troubleshooting
- Ver logs em tempo real:

```powershell
docker-compose logs -f
```

- Filtrar por serviço:

```powershell
docker-compose logs -f api-gateway
docker-compose logs -f service
docker-compose logs -f service-consolidado
docker-compose logs -f rabbitmq
```

- Problemas comuns:
  - Erro de conexão RabbitMQ: confirme `RabbitMqConfiguration.Connection.Host` = `rabbitmq` no ambiente Docker.
  - Erro de portas: verifique mapeamentos em docker-compose.yml (5000, 5001, 5002)

Estrutura de filas (RabbitMQ)
- Exchange: lancamentos.exchange → lancamentos.queue, lancamentos.retry.queue, lancamentos.fail.queue
- Exchange: consolidado.exchange → consolidado.queue, consolidado.retry.queue, consolidado.fail.queue

Banco de dados
- Conectar ao PostgreSQL dentro do container:

```powershell
docker exec -it <postgres_container_id> psql -U postgres -d lancamentos
```

Exemplo de consulta:

```sql
SELECT * FROM "Lancamentos" WHERE "Id" = '550e8400-e29b-41d4-a716-446655440000';
```

Documentação adicional (conteúdo agregado)
- ARCHITECTURE.md — visão da arquitetura, diagramas de filas, injeção de dependências, escalabilidade e monitoração
- EXECUTION_GUIDE.md — guia passo a passo, exemplos de cURL, acesso aos relatórios e troubleshooting profundo
- CHANGELOG.md — histórico de alterações e arquivos adicionados
- ServiceConsolidado/README.md — documentação específica do serviço de consolidação

Contribuição
- Abrir issues e PRs no repositório.
- Para desenvolvimento local sem Docker, garanta RabbitMQ e Postgres rodando localmente e ajuste ConnectionStrings em appsettings.

Licença e contato
- Repositório: https://github.com/lldeoj/arquitetura_teste

---

