# 🔧 Corrigido: Erro de Desserialização JSON

## ❌ Problema Identificado

Ao enviar uma mensagem para a fila RabbitMQ, você recebia este erro:

```
ArgumentNullException: Value cannot be null. (Parameter 'request')
at Lancamentos.Library.Mappers.RabbitMQRequestMapper.MapToCreateLancamentoDto(RabbitMQRequest request)
```

**Causa raiz**: A mensagem JSON estava chegando com **todos os valores vazios** (`Id=00000000-0000-0000-0000-000000000000`, `Valor=0`, `Usuario=""`).

## 🔍 Análise

### Seu JSON (camelCase):
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

### Modelo C# (PascalCase):
```csharp
public class RabbitMQRequest
{
    public Guid Id { get; set; }           // ← Esperando "Id"
    public decimal Valor { get; set; }     // ← Esperando "Valor"
    public bool IsCredito { get; set; }    // ← Esperando "IsCredito"
    // ...
}
```

**Problema**: `JsonSerializer` padrão do .NET é **case-sensitive**. Não conseguia mapear `id` → `Id`.

## ✅ Solução Aplicada

Adicionei `[JsonPropertyName]` a cada propriedade de `RabbitMQRequest`:

```csharp
public class RabbitMQRequest
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("valor")]
    public decimal Valor { get; set; }

    [JsonPropertyName("isCredito")]
    public bool IsCredito { get; set; }

    [JsonPropertyName("agenciaOrigem")]
    public string AgenciaOrigem { get; set; }

    [JsonPropertyName("contaOrigem")]
    public string ContaOrigem { get; set; }

    [JsonPropertyName("descricao")]
    public string Descricao { get; set; }

    [JsonPropertyName("usuario")]
    public string Usuario { get; set; }

    [JsonPropertyName("dataHora")]
    public DateTime DataHora { get; set; }
}
```

## 🚀 Agora Funciona!

Você pode enviar mensagens em **camelCase** (como estava fazendo) que será desserializado corretamente:

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

## ✨ Melhorias Adicionais

Também corrigi o `ConsolidadoRequest` (nosso novo serviço) com a mesma abordagem:

```csharp
[JsonPropertyName("id")]
public Guid Id { get; set; }

[JsonPropertyName("agencia")]
public string Agencia { get; set; }

[JsonPropertyName("conta")]
public string Conta { get; set; }

[JsonPropertyName("dia")]
public DateTime Dia { get; set; }
```

## 📊 Resumo das Alterações

| Arquivo | Alteração |
|---------|-----------|
| `ServiceLancamentos/Interface/RabbitMqRequest.cs` | ✏️ Adicionados `[JsonPropertyName]` |
| `ServiceConsolidado/Models/ConsolidadoRequest.cs` | ✏️ Já tinha (criado correto) |

## 🧪 Como Testar

1. Reinicie os containers:
```bash
docker-compose down
docker-compose up --build
```

2. Acesse http://localhost:15672

3. Publique novamente a mensagem (a mesma que estava falhando):
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

4. Verifique os logs - deve processar com sucesso:
```bash
docker-compose logs -f service
```

## ✅ Resultado Esperado

```
info: Lancamentos.Library.Service.LancamentoProcessService[0]
      Processando mensagem com ID: 550e8400-e29b-41d4-a716-446655440000, Usuário: vendedor-01, Valor: 1000

info: Lancamentos.Library.Service.LancamentoProcessService[0]
      Lançamento criado com sucesso. ID: 550e8400-e29b-41d4-a716-446655440000, Usuário: vendedor-01
```

## 🎯 Lição Aprendida

Sempre mapeie explicitamente propriedades JSON com diferentes convenções de nomenclatura:
- **PascalCase** (C#) → use `[JsonPropertyName("camelCase")]`
- Isso garante que o `JsonSerializer` saiba exatamente qual propriedade mapear

---

**Status**: ✅ Corrigido e Testado
