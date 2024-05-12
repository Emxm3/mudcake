using Microsoft.AspNetCore.SignalR;
using MudCake.core.SignalR.Hubs;
using MudCake.Data.SignalR.Hubs;

namespace MudCake.Data.Services
{
    public abstract class AbstractHubService<THub, TClient> 
        where THub : Hub<TClient>
        where TClient : class
    {
        protected readonly IHubContext<THub, TClient> __hubContext;

        public AbstractHubService(IHubContext<THub, TClient> hubContext)
        {
            this.__hubContext = hubContext;
        }
    }
}
