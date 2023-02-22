namespace NanoleafAPI
{
    public class DiscoveredEventArgs : EventArgs
    {
        public readonly DiscoveredDevice DiscoveredDevice;
        public DiscoveredEventArgs(DiscoveredDevice discoveredDevice)
        {
            DiscoveredDevice = discoveredDevice;
        }
    }
}
