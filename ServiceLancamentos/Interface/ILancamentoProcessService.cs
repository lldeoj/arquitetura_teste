using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lancamentos.Library.Interface
{
    public interface ILancamentoProcessService
    {
        void ListenerOnRabbitMqQueue(CancellationToken cancellationToken);
    }
}
