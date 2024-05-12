using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;

namespace MudCake.Data.SignalR
{
    public static class Extensions
    {
        /// <summary>
        /// Simplifies adding SignalR Hubs and forcing them to adhere to naming conventions
        /// </summary>
        /// <typeparam name="THub"></typeparam>
        /// <param name="endpoint"></param>
        public static void AddHub<THub>(this IEndpointRouteBuilder endpoint) where THub : Hub
        {
            string endpointName = typeof(THub).Name.ToLower();
            endpoint.MapHub<THub>($"/{endpointName}");
        }

        /// <summary>
        /// Invokes multiple actions on a HubConnection before starting it up if it's not already.
        /// Used to register multiple listeners for a hub easily
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="connectionActions"></param>
        /// <returns></returns>
        public static async Task<HubConnection> Invokes(this HubConnection connection, Action<HubConnection>OnConnect, params Action<HubConnection>[] connectionActions)
        {

            //run each action agains the hub
            connectionActions.ToList().ForEach(con => con.Invoke(connection));

            //If it's not connected, run it.
            if (connection.State != HubConnectionState.Connected)
            {
                await connection.StartAsync();
                OnConnect?.Invoke(connection);
            }


                return connection;
           
        }

    }
}
