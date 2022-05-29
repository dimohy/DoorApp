namespace DoorApp.Data
{
    public class Message
    {
        public string Command { get; }
        public string DeviceType { get; }
        public string DeviceId { get; }

        public Message(string command, string deviceType, string deviceId)
        {
            Command = command;
            DeviceType = deviceType;
            DeviceId = deviceId;
        }
    }
}
