namespace NanoleafAPI
{
    public class LayoutEventArgs : EventArgs
    {
        public readonly string IP;
        public readonly LayoutEvent LayoutEvent;
        public LayoutEventArgs(string ip, LayoutEvent layoutEvent)
        {
            IP = ip;
            LayoutEvent = layoutEvent;
        }
    }
}
