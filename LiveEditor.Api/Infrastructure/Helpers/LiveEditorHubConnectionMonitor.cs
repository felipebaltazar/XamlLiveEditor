using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiveEditor.Api.Abstractions;
using LiveEditor.Api.Domain.Models;
using LiveEditor.Api.Extensions;
using LiveEditor.Api.Hub;
using LiveEditor.Api.Infrastructure.Exceptions;

namespace LiveEditor.Api.Infrastructure.Helpers
{
    public sealed class LiveEditorHubConnectionMonitor : IConnectionMonitor<LiveEditorHub>
    {
        #region Fields

        private readonly List<HubClient> _connections = new List<HubClient>();
        private readonly HashSet<string> _pendingConnections = new HashSet<string>();

        private readonly object _pendingConnectionsLock = new object();
        private readonly object _connectionsLock = new object();

        #endregion

        #region IConectionMonitor<LiveEditorHub>

        /// <inheritdoc/>
        public async Task InitConnectionMonitoringAsync(HubCallerContext context, LiveEditorHub hub)
        {
            var id = context.ConnectionId;
            var deviceId = context?.ToConnectionId();
            var isEditor = context.CheckIsEditor();

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                context.Abort();
                throw new ConnectionAbortedException(nameof(LiveEditorHub), $"Conexão inválida para o device '{deviceId}'");
            }

            await hub.Groups.AddToGroupAsync(id, deviceId).ConfigureAwait(false);

            _connections.Add(new HubClient(id, deviceId, isEditor));

            var feature = context.Features.Get<IConnectionHeartbeatFeature>();
            feature.OnHeartbeat(state =>
            {
                if (_pendingConnections.Contains(context.ConnectionId))
                {
                    context.Abort();
                    lock (_pendingConnectionsLock)
                    {
                        _pendingConnections.Remove(context.ConnectionId);
                    }
                }

            }, context.ConnectionId);

            if (isEditor)
                await hub.RequestCurrentPage(deviceId).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public string GetId(string username) => GetConnectionId(username);

        /// <inheritdoc/>
        public Task OnDisconnectedAsync(LiveEditorHub hub, Exception exception)
        {
            var id = hub.Context.ConnectionId;
            var roomName = hub.Context.ToConnectionId();

            lock (_connectionsLock)
            {
                var connToRemove = _connections.FirstOrDefault(c => c.ConnectionId == id);
                if (connToRemove == null) return Task.CompletedTask;

                _connections.Remove(connToRemove);
            }


            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public bool CloseConnection(string deviceId)
        {
            var connection = _connections.FirstOrDefault(c => c.DeviceId == deviceId);

            if (string.IsNullOrEmpty(connection?.ConnectionId)) return false;

            var connectionId = connection?.ConnectionId;
            if (!_pendingConnections.Contains(connectionId))
            {
                lock (_pendingConnectionsLock)
                {
                    _pendingConnections.Add(connectionId);
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Private Methods

        private string GetConnectionId(string deviceId)
        {
            lock (_connectionsLock)
            {
                return _connections.FirstOrDefault(c => c.DeviceId == deviceId)?.ConnectionId;
            }
        }

        #endregion
    }
}
