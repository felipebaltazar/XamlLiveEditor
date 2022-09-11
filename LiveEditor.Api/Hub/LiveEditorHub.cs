using LiveEditor.Api.Abstractions;
using LiveEditor.Api.Infrastructure.Exceptions;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using SignalHub = Microsoft.AspNetCore.SignalR.Hub;

namespace LiveEditor.Api.Hub
{
    public class LiveEditorHub : SignalHub
    {
        #region Fields

        private readonly IConnectionMonitor<LiveEditorHub> _connectionMonitor;

        #endregion

        #region Constructors

        public LiveEditorHub(IConnectionMonitor<LiveEditorHub> connectionMonitor)
        {
            _connectionMonitor = connectionMonitor;
        }

        #endregion

        #region Hub Methods

        /// <summary>
        /// Requisita a página atual para o app conectado
        /// </summary>
        /// <param name="deviceId">Id do device conectado</param>
        /// <returns>Task</returns>
        [HubMethodName(nameof(RequestCurrentPage))]
        public Task RequestCurrentPage(string deviceId)
        {
            return 
                Clients
                .Group(deviceId)
                .SendCoreAsync(nameof(RequestCurrentPage), Array.Empty<object>());
        }

        /// <summary>
        /// Ao alterar codigo no editor
        /// </summary>
        /// <param name="deviceId">Id do device conectado</param>
        /// <param name="code">Novo código</param>
        /// <returns>Task</returns>
        [HubMethodName(nameof(CodeChanged))]
        public Task CodeChanged(object deviceId, object code)
        {
            return Clients
                .Group(deviceId.ToString())
                .SendCoreAsync(nameof(CodeChanged), new object[] { code.ToString() });
        }

        /// <summary>
        /// Ocorre ao alterar a pagina atual
        /// </summary>
        /// <param name="deviceId">Id do device conectado</param>
        /// <param name="code">Código da pagina atual</param>
        /// <returns>Task</returns>
        [HubMethodName(nameof(CurrentPageChanged))]
        public Task CurrentPageChanged(object deviceId, object code)
        {
            return Clients
                .Group(deviceId.ToString())
                .SendCoreAsync(nameof(CurrentPageChanged), new object[] { code.ToString() });
        }

        #endregion

        #region Overrides

        public override async Task OnConnectedAsync()
        {
            try
            {
                await base.OnConnectedAsync().ConfigureAwait(false);
                await _connectionMonitor.InitConnectionMonitoringAsync(Context, this).ConfigureAwait(false);
            }
            catch (ConnectionAbortedException)
            {
                //TODO: logs
            }
        }

        public async override Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
                await _connectionMonitor.OnDisconnectedAsync(this, exception).ConfigureAwait(false);
            }
            catch (Exception)
            {
                //TODO: logs
            }
        }

        #endregion
    }
}
