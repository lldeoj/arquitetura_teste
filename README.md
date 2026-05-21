# ServiceLancamentos

Este repositório contém o serviço ServiceLancamentos, um repositório e código de integração com RabbitMQ e Postgres.

## Execução com Docker

O docker-compose inclui:
- Postgres
- RabbitMQ (com management UI)
- O serviço ServiceLancamentos

Inicie tudo:

```bash
docker-compose up --build
```

RabbitMQ Management UI: http://localhost:15672 (user: rabbitmq-user / rabbitmq-user)

Postgres: host=db, database=lancamentos, user=postgres, password=postgres

Fila e exchange usados:
- Exchange: lancamentos.exchange
- Queue: lancamentos.queue

## Teste completo (end-to-end)

1. Inicie o ambiente com `docker-compose up --build`.
2. Acesse o RabbitMQ Management UI (http://localhost:15672) e publique uma mensagem na fila `lancamentos.queue` ligada ao exchange `lancamentos.exchange`.

Exemplo de body JSON da mensagem (RabbitMQRequest):

```json
{
  "Id": "<GUID>",
  "Valor": 100.0,
  "IsCredito": true,
  "AgenciaOrigem": "001",
  "ContaOrigem": "12345-6",
  "Descricao": "Teste",
  "Usuario": "usuario",
  "DataHora": "2026-01-01T00:00:00Z"
}
```

3. Após a mensagem ser processada pelo serviço, verifique no Postgres se o registro foi inserido:

   - Conecte ao banco (psql) ou use uma ferramenta GUI apontando para localhost:5432 (usuário postgres / senha postgres) e execute:

```sql
SELECT * FROM "Lancamentos" WHERE "Id" = '<GUID>';
```

4. O registro inserido deverá ter os campos correspondentes à mensagem.

Observações:
- A configuração do serviço aponta para os hosts `db` e `rabbitmq` quando executado via docker-compose.
- Ajuste GUID no SELECT pelo valor usado na mensagem.
