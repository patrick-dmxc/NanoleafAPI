using System;

namespace NanoleafAPI
{
    public class StateEventArgs : EventArgs
    {
        public readonly string IP;
        public readonly StateEvents StateEvents;
        public StateEventArgs(string ip, StateEvents stateEvents)
        {
            IP = ip;
            StateEvents = stateEvents;
        }
    }
}
