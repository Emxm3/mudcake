
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using MudCake.core.Data.Services;


namespace MudCake.core.SignalR
{
    public class HubConnectionService(NavigationManager navman) : IHubConnectionService
    {
        protected readonly NavigationManager navman = navman;

        public HubConnection Get<THub>() where THub : Hub 
            => new HubConnectionBuilder()
                .WithUrl(navman.ToAbsoluteUri($"/{typeof(THub).Name.ToLower()}"), op =>
                {
                    op.UseDefaultCredentials = true;
                    op.HttpMessageHandlerFactory = o =>
                    {
                        var handler = new HttpClientHandler()
                        {
                            UseDefaultCredentials = true,
                            Credentials = System.Net.CredentialCache.DefaultCredentials,
                            AllowAutoRedirect = true,
                            ClientCertificateOptions = ClientCertificateOption.Manual,
                            SslProtocols = System.Security.Authentication.SslProtocols.Tls13 | System.Security.Authentication.SslProtocols.Tls12
                        };

                        handler.ServerCertificateCustomValidationCallback += (_, _, _, _) => true;

                        return handler;
                    };
                })
                .Build();
    }

    public interface IHubConnectionService : IDataService
    {
        HubConnection Get<THub>() where THub : Hub;
    }

}
