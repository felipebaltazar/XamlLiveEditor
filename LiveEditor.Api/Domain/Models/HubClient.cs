namespace LiveEditor.Api.Domain.Models
{
    internal sealed class HubClient
    {
        public HubClient(string connectionId, string deviceId, bool isEditor)
        {
            ConnectionId = connectionId;
            DeviceId = deviceId;
            IsEditor = isEditor;
        }

        public string ConnectionId { get; }

        public string DeviceId { get; }

        public bool IsEditor { get; }
    }
}
