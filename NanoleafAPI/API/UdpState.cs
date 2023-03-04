using System.Net;
using System.Net.Sockets;

namespace NanoleafAPI
{
    public readonly struct UdpState
    {
        public readonly IPEndPoint EndPoint { get; }
        public readonly UdpClient UdpClient { get; }

        public UdpState(in UdpClient udpClient, in IPEndPoint endPoint)
        {
            this.EndPoint = endPoint;
            this.UdpClient = udpClient;
        }
    }
}