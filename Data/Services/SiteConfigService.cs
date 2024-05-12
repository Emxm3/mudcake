using Microsoft.AspNetCore.SignalR;
using MudCake.core.Data.Hubs.Clients;
using MudCake.core.Data.Services;
using MudCake.core.Data.Site;
using MudCake.Data.SignalR.Hubs;

namespace MudCake.Data.Services
{
    public class SiteConfigService : AbstractHubService<SiteConfigHub, ISiteConfigClient>, ISiteConfigService
    {
        public SiteConfigService(IHubContext<SiteConfigHub, ISiteConfigClient> hubContext) : base(hubContext)
        {
        }

        public Task Update(ISiteConfig siteConfig)
        {
            return __hubContext.Clients.All.Update(siteConfig);
        }
    }
}
