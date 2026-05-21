# ServiceConsolidado - Serviço de Consolidação de Saldos Diários

## Descrição

O **ServiceConsolidado** é um microserviço em .NET 8 que recebe requisições de consolidação de saldos diários através de uma fila RabbitMQ e gera relatórios em formato JSON.

## Funcionalidades

- 📨 Consome mensagens da fila RabbitMQ `consolidado.queue`
- 📊 Calcula saldo consolidado (saldo inicial + créditos - débitos)
- 💾 Salva relatórios em arquivo JSON local
- 🔄 Suporte a retry automático em caso de falhas
- 🔗 Integração com banco de dados PostgreSQL

## Estrutura da Mensagem Recebida

```json
{
  "id": "guid-da-requisicao",
  "agencia": "0001",
  "conta": "123456",
  "dia": "2024-01-15T00:00:00Z"
}
```

## Arquivo de Saída

Os relatórios são salvos em `/app/relatorios/{GUID}.json`:

```json
{
  "id": "guid-da-requisicao",
  "agencia": "0001",
  "conta": "123456",
  "dia": "2024-01-15T00:00:00Z",
  "saldoInicial": 1000.00,
  "totalCreditos": 5000.00,
  "totalDebitos": 2000.00,
  "saldoFinal": 4000.00,
  "dataProcessamento": "2024-01-15T10:30:45.123Z"
}
```

## Configuração

### appsettings.json

```json
{
  "DataBase": {
    "ConnectionString": "Host=db;Database=lancamentos;Username=postgres;Password=postgres"
  },
  "RabbitMqConfiguration": {
    "Connection": {
      "Host": "rabbitmq",
      "User": "rabbitmq-user",
      "Password": "rabbitmq-user",
      "Port": 5672
    },
    "Queues": {
      "Exchange": "consolidado.exchange",
      "Queue": "consolidado.queue",
      "QueueRetry": "consolidado.retry.queue",
      "QueueFail": "consolidado.fail.queue",
      "MaxRetry": "3"
    }
  },
  "ConsolidadoSettings": {
    "OutputPath": "/app/relatorios"
  }
}
```

## Execução com Docker Compose

O serviço é automaticamente iniciado quando você executa:

```bash
docker-compose up --build
```

O serviço estará disponível como `service-consolidado` na porta `5001`.

## Dependências

- **PostgreSQL**: Para armazenar lançamentos
- **RabbitMQ**: Para fila de mensagens
- **Lancamentos.Library**: Biblioteca compartilhada com repositórios e serviços
- **RabbitMqMessage**: Biblioteca para comunicação RabbitMQ

## Fluxo de Processamento

```
1. Mensagem recebida na fila "consolidado.queue"
2. Validação da mensagem
3. Cálculo do saldo consolidado:
   - Busca lançamentos do dia para a agência e conta
   - Soma créditos
   - Soma débitos
   - Calcula saldo final
4. Serializa para JSON
5. Salva em arquivo {GUID}.json
6. Confirma processamento na fila
```

## Tratamento de Erros

- **Falhas Temporárias**: Automáticamente enviadas para `consolidado.retry.queue`
- **Falhas Persistentes**: Enviadas para `consolidado.fail.queue` após `MaxRetry` tentativas
- **Logs**: Todas as operações são registradas com nível de detalhe apropriado

## Logs

Os logs são estruturados e seguem o padrão:

```
info: ServiceConsolidado.Service.ConsolidadoProcessService[0]
      Processando mensagem com ID: {guid}, Agência: {agencia}, Conta: {conta}, Dia: {dia}

info: ServiceConsolidado.Service.ConsolidadoService[0]
      Relatório salvo com sucesso no caminho: /app/relatorios/{guid}.json
```

## Desenvolvimento Local

### Pré-requisitos
- .NET 8 SDK
- Docker e Docker Compose
- PostgreSQL 15
- RabbitMQ 3

### Executar com docker-compose

```bash
# Parar e remover containers existentes
docker-compose down

# Iniciar todos os serviços
docker-compose up --build

# Ver logs do serviço
docker-compose logs -f service-consolidado
```

### Testar o Serviço

Você pode enviar mensagens diretamente para a fila usando uma ferramenta como:

1. **RabbitMQ Management UI**: http://localhost:15672
   - Usuário: `rabbitmq-user`
   - Senha: `rabbitmq-user`

2. **Cliente .NET ou Python** para enviar mensagens à fila `consolidado.queue`

## Estrutura do Projeto

```
ServiceConsolidado/
├── Models/
│   ├── ConsolidadoRequest.cs      # Modelo de entrada
│   └── SaldoDiarioConsolidado.cs  # Modelo de saída (relatório)
├── Interface/
│   ├── IConsolidadoProcessService.cs
│   └── IConsolidadoService.cs
├── Service/
│   ├── ConsolidadoService.cs           # Lógica de negócio
│   └── ConsolidadoProcessService.cs    # Processamento de mensagens
├── Worker.cs                    # Background service
├── Program.cs                   # Configuração e inicialização
├── ServiceCollectionExtensions.cs # Registro de dependências
├── appsettings.json            # Configuração
└── Dockerfile                  # Containerização
```

## Autor

Desenvolvido como parte da arquitetura de microserviços de processamento de lançamentos.
