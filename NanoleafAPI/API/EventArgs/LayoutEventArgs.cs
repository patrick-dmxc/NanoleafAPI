namespace NanoleafAPI
{
    public class LayoutEventArgs : EventArgs
    {
        public readonly string IP;
        public readonly LayoutEvents LayoutEvents;
        public LayoutEventArgs(string ip, LayoutEvents layoutEvents)
        {
            IP = ip;
            LayoutEvents = layoutEvents;
        }
    }
}
