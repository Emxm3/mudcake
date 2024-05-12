using Microsoft.AspNetCore.SignalR;
using MudCake.core.Data.Hubs.Clients;
using MudCake.core.Data.Site;
using System.Diagnostics;

namespace MudCake.Data.SignalR.Hubs
{
    public class SiteConfigHub : Hub<ISiteConfigClient>
    {

        public async Task SendNotification(ISiteConfig config)
        {
            await Clients.All.Update(config);
        }
    }
}
