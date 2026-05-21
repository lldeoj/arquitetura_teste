#!/bin/bash

# Script para enviar mensagem de teste para a fila consolidado
# Requer: jq e RabbitMQ rodando localmente

RABBITMQ_HOST="localhost"
RABBITMQ_PORT="5672"
RABBITMQ_USER="rabbitmq-user"
RABBITMQ_PASSWORD="rabbitmq-user"
QUEUE_NAME="consolidado.queue"

# Gerar um GUID aleatório
REQUEST_ID=$(uuidgen)

# Criar payload JSON
PAYLOAD=$(cat <<EOF
{
  "id": "$REQUEST_ID",
  "agencia": "0001",
  "conta": "123456",
  "dia": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
}
EOF
)

echo "Enviando mensagem para a fila: $QUEUE_NAME"
echo "Payload:"
echo "$PAYLOAD"
echo ""
echo "Request ID: $REQUEST_ID"

# Enviar via HTTP API do RabbitMQ Management (alternativa simples)
# Nota: Este é um exemplo simplificado. Para produção, use uma ferramenta apropriada

echo "Use RabbitMQ Management UI em http://localhost:15672 para enviar mensagens manualmente"
echo "Ou use uma ferramenta como Python/node.js com biblioteca RabbitMQ apropriada"
