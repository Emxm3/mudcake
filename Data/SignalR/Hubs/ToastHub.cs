using Microsoft.AspNetCore.SignalR;
using MudBlazor;
using MudCake.core.SignalR.Hubs;
using System.Diagnostics;

namespace MudCake.Data.SignalR.Hubs
{
    public class ToastHub() : Hub<IToastClient>, IToastClient
    {
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public Task Send(string message, Severity severity = Severity.Normal, string key = "")
        {
            
            return Clients.All.Send(message, severity, key);
        }


    }
}
