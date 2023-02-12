using System;

namespace NanoleafAPI
{
    public class EffectEventArgs : EventArgs
    {
        public readonly string IP;
        public readonly EffectEvents EffectEvents;
        public EffectEventArgs(string ip, EffectEvents effectEvents)
        {
            IP = ip;
            EffectEvents = effectEvents;
        }
    }
}
