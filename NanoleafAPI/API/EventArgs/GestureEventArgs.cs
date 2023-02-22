namespace NanoleafAPI
{
    public class GestureEventArgs : EventArgs
    {
        public readonly string IP;
        public readonly GestureEvents GestureEvents;
        public GestureEventArgs(string ip, GestureEvents gestureEvents)
        {
            IP = ip;
            GestureEvents = gestureEvents;
        }
    }
}
