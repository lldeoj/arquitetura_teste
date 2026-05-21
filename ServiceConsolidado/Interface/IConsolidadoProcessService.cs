using ServiceConsolidado.Models;

namespace ServiceConsolidado.Interface
{
    public interface IConsolidadoProcessService
    {
        void ListenerOnRabbitMqQueue(CancellationToken cancellationToken);
    }
}
