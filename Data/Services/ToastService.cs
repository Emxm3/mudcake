using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using MudCake.core.Data.Services;
using MudCake.core.SignalR;
using MudCake.core.SignalR.Hubs;
using MudCake.Data.SignalR;
using MudCake.Data.SignalR.Hubs;
using System.Diagnostics;

namespace MudCake.Data.Services
{
    public class ToastService(IHubContext<ToastHub, IToastClient> hubContext, IHubConnectionService hubConnectionFactory) : AbstractHubService<ToastHub, IToastClient>(hubContext), IToastService
    {
        public event Func<HubConnection,string>? OnConnectionRequested;
        HubConnection? connection;
        string? connectionId;

        public async Task Connect()
        {
            if (connection != null && connection.State == HubConnectionState.Connected) return;
            connection = hubConnectionFactory.Get<ToastHub>();
            connectionId = OnConnectionRequested?.Invoke(connection);

            await SendSelf("Toast Service Connected!", Severity.Success);

            await Task.CompletedTask;
        }

        public Task Send(string message, Severity severity = Severity.Normal, string key = "")
        {
            return __hubContext.Clients.All.Send(message, severity,key);
        }

        public Task SendOthers(string message, Severity severity = Severity.Normal, string key = "")
        {
            if (connectionId == null) throw new Exception("You must run Connect() before attempting this method");
            return __hubContext.Clients.AllExcept([connectionId]).Send(message, severity, key);
        }

        public Task SendSelf(string message, Severity severity = Severity.Normal, string key = "")
        {
            if (connectionId == null) throw new Exception("You must run Connect() before attempting this method");
            return __hubContext.Clients.Client(connectionId).Send(message, severity, key);
        }


    }
}
