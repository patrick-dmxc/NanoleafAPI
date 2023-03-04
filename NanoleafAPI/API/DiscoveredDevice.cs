﻿namespace NanoleafAPI
{
    public readonly struct DiscoveredDevice
    {
        public readonly string IP;
        public readonly string Port;
        public readonly string Name;
        public readonly string ID;
        public readonly EDeviceType DeviceTyp;
        public DiscoveredDevice(in string ip, in string port, in string name, in string id, in EDeviceType deviceType) : this()
        {
            IP = ip;
            Port = port;
            Name = name;
            ID = id;
            DeviceTyp = deviceType;
        }
        public override string ToString()
        {
            return $"{Name} {IP}:{Port} {DeviceTyp}";
        }
    }
}