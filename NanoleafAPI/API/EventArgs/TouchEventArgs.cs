namespace NanoleafAPI
{
    public class TouchEventArgs : EventArgs
    {
        public readonly string IP;
        public readonly TouchEvent TouchEvent;
        public TouchEventArgs(string ip, TouchEvent touchEvent)
        {
            IP = ip;
            TouchEvent = touchEvent;
        }
    }
}
