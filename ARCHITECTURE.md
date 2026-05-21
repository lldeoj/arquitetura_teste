# Arquitetura de Microserviços - Sistema de Lançamentos

## Visão Geral

O sistema é composto por múltiplos microserviços em .NET 8 que trabalham de forma desacoplada através de RabbitMQ para processamento assíncrono de lançamentos e consolidação de saldos.

## Componentes

### 1. **ServiceLancamentos** (Porta 5000)
- **Descrição**: Serviço responsável por processar e persistir lançamentos
- **Fila Primária**: `lancamentos.queue`
- **Fila de Retry**: `lancamentos.retry.queue`
- **Fila de Falha**: `lancamentos.fail.queue`
- **Responsabilidades**:
  - Receber requisições de lançamento
  - Persistir no banco de dados PostgreSQL
  - Gerenciar tentativas de retry automático
  - Registrar logs detalhados

**Modelos**:
```
RabbitMQRequest:
- Id: Guid (identificador da requisição)
- Valor: decimal
- IsCredito: bool
- AgenciaOrigem: string
- ContaOrigem: string
- Descricao: string
- Usuario: string
- DataHora: DateTime
```

### 2. **ServiceConsolidado** (Porta 5001) ✨ NOVO
- **Descrição**: Serviço responsável por consolidação de saldos diários
- **Fila Primária**: `consolidado.queue`
- **Fila de Retry**: `consolidado.retry.queue`
- **Fila de Falha**: `consolidado.fail.queue`
- **Responsabilidades**:
  - Receber requisições de consolidação
  - Calcular saldo consolidado (créditos - débitos)
  - Gerar relatórios em JSON
  - Salvar arquivos em sistema local

**Modelos**:
```
ConsolidadoRequest:
- Id: Guid (identificador da requisição)
- Agencia: string
- Conta: string
- Dia: DateTime

SaldoDiarioConsolidado:
- Id: Guid
- Agencia: string
- Conta: string
- Dia: DateTime
- SaldoInicial: decimal
- TotalCreditos: decimal
- TotalDebitos: decimal
- SaldoFinal: decimal
- DataProcessamento: DateTime
```

**Saída**: `/app/relatorios/{GUID}.json`

### 3. **Lancamentos.Library** (Compartilhada)
- **Descrição**: Biblioteca reutilizável com modelos e serviços
- **Contém**:
  - `AppDbContext`: Contexto EF Core
  - `Lancamento`: Modelo de domínio
  - `LancamentoDto`: DTO para transferência
  - `LancamentoRepository`: Acesso a dados
  - `LancamentoService`: Lógica de negócio
  - Interfaces do repositório e serviço

### 4. **RabbitMqMessage** (Compartilhada)
- **Descrição**: Biblioteca para comunicação RabbitMQ
- **Contém**:
  - `RabbitMqDataContext`: Gerenciamento de conexão e canal
  - `RabbitMqMessageRepository`: Interface unificada de mensagens
  - `RabbitMqConfiguration`: Configurações
  - Suporte para publish, consume e listeners

### 5. **PostgreSQL** (Banco de Dados)
- **Imagem**: `postgres:15`
- **Database**: `lancamentos`
- **Usuário**: `postgres`
- **Porta**: `5432` (acesso externo), `5432` (interno)
- **Volumes**: `db-data` (persistência)

### 6. **RabbitMQ** (Message Broker)
- **Imagem**: `rabbitmq:3-management`
- **Management UI**: `http://localhost:15672`
- **Credenciais**: `rabbitmq-user` / `rabbitmq-user`
- **Portas**: 
  - `5672` (AMQP)
  - `15672` (Management UI)

## Fluxo de Dados

### Cenário 1: Processamento de Lançamentos

```
Cliente HTTP
    ↓
ServiceLancamentos API (POST /lançamentos)
    ↓
ServiceLancamentos Worker
    ↓
RabbitMQ (lancamentos.queue)
    ↓
ServiceLancamentos.ListenerOnRabbitMqQueue
    ↓
LancamentoProcessService.ProcessMessageWithServicesAsync
    ↓
LancamentoService.CreateAsync
    ↓
PostgreSQL (salvar Lancamento)
    ↓
Log de sucesso
```

**Em caso de erro:**
```
Erro → lancamentos.retry.queue → Nova tentativa após delay
Falha após MaxRetry → lancamentos.fail.queue → Requer ação manual
```

### Cenário 2: Consolidação de Saldos

```
Cliente (envia para RabbitMQ)
    ↓
RabbitMQ (consolidado.queue)
    ↓
ServiceConsolidado Worker
    ↓
ServiceConsolidado.ListenerOnRabbitMqQueue
    ↓
ConsolidadoProcessService.ProcessMessageWithServicesAsync
    ↓
ConsolidadoService.CalcularSaldoConsolidadoAsync
    ↓
LancamentoService.GetLancamentosByAgencyAccountAndDateAsync
    ↓
PostgreSQL (consultar Lancamentos)
    ↓
Cálculo: SaldoFinal = SaldoInicial + Créditos - Débitos
    ↓
ConsolidadoService.SalvarRelatorioAsync
    ↓
/app/relatorios/{GUID}.json
    ↓
Log de sucesso
```

## Diagrama de Filas RabbitMQ

```
┌─────────────────────────────────────────────────────────────┐
│                         RabbitMQ                             │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  Exchange: lancamentos.exchange                              │
│  └─→ lancamentos.queue        → ServiceLancamentos Worker   │
│  └─→ lancamentos.retry.queue  → ServiceLancamentos Worker   │
│  └─→ lancamentos.fail.queue   → [Monitoramento]            │
│                                                               │
│  Exchange: consolidado.exchange                              │
│  └─→ consolidado.queue        → ServiceConsolidado Worker   │
│  └─→ consolidado.retry.queue  → ServiceConsolidado Worker   │
│  └─→ consolidado.fail.queue   → [Monitoramento]            │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

## Injeção de Dependências

### ServiceLancamentos
```csharp
services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(connectionString));
services.AddScoped<ILancamentoRepository, LancamentoRepository>();
services.AddScoped<ILancamentoService, LancamentoService>();
services.AddScoped<ILancamentoProcessService, LancamentoProcessService>();
services.AddSingleton<IRabbitMqMessageRepository, RabbitMqMessageRepository>();
```

### ServiceConsolidado
```csharp
services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(connectionString));
services.AddScoped<ILancamentoRepository, LancamentoRepository>();
services.AddScoped<ILancamentoService, LancamentoService>();
services.AddScoped<IConsolidadoService, ConsolidadoService>();
services.AddScoped<IConsolidadoProcessService, ConsolidadoProcessService>();
services.AddSingleton<IRabbitMqMessageRepository, RabbitMqMessageRepository>();
```

## Configuração com Docker Compose

```yaml
services:
  db:
    image: postgres:15
    healthcheck: pg_isready -U postgres -d lancamentos

  rabbitmq:
    image: rabbitmq:3-management
    healthcheck: rabbitmqctl status

  service:                    # ServiceLancamentos
    build: ServiceLancamentos/Dockerfile
    depends_on:
      db: service_healthy
      rabbitmq: service_healthy

  service-consolidado:        # ServiceConsolidado
    build: ServiceConsolidado/Dockerfile
    depends_on:
      db: service_healthy
      rabbitmq: service_healthy
    volumes:
      - relatorios-data:/app/relatorios
```

## Tecnologias

- **.NET**: 8.0
- **ORM**: Entity Framework Core
- **Banco de Dados**: PostgreSQL 15
- **Message Broker**: RabbitMQ 3
- **Containerização**: Docker & Docker Compose
- **Padrão de Projeto**: Microserviços com CQRS (async messaging)
- **Logging**: Microsoft.Extensions.Logging

## Escalabilidade

### Horizontal
- Múltiplas instâncias de ServiceLancamentos podem consumir da mesma fila
- Múltiplas instâncias de ServiceConsolidado podem processar em paralelo
- RabbitMQ distribui mensagens entre consumidores

### Vertical
- Aumentar recursos (CPU, RAM) dos containers
- Ajustar tamanho de batches de processamento
- Configurar timeouts apropriados

## Monitoramento

### Logs
```
info: ServiceLancamentos.Service.LancamentoProcessService[0]
      Processando mensagem com ID: {guid}

error: ServiceLancamentos.Service.LancamentoProcessService[0]
       Erro ao processar a mensagem recebida do RabbitMQ.
```

### RabbitMQ Management UI
- **URL**: http://localhost:15672
- **Visualize**: Filas, mensagens, consumidores
- **Monitore**: Taxa de publicação/consumo

### Saúde do Banco
- Healthcheck automático no startup
- Retry automático com backoff exponencial
- Conexão garantida antes de iniciar processamento

## Tratamento de Erros

| Tipo de Erro | Ação |
|---|---|
| `SocketException` | Retry com backoff exponencial |
| `NpgsqlException` | Retry com backoff exponencial |
| Erro de validação | Log e descarte (DLQ) |
| Erro de processamento | Enviar para fila de retry |
| Falha após MaxRetry | Enviar para fila de falha |

## Próximos Passos

1. **Implementar API Gateway** para roteamento inteligente
2. **Adicionar Circuit Breaker** para resiliência
3. **Implementar metrics/observability** com Prometheus
4. **Adicionar autenticação/autorização** (OAuth2)
5. **Configurar CI/CD pipeline** (GitHub Actions/Azure Pipelines)
6. **Implementar Event Sourcing** para auditoria
7. **Adicionar cache distribuído** (Redis)

---

**Versão**: 1.0
