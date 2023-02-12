using System.Net;
using System.Net.Sockets;

namespace NanoleafAPI
{
    public struct UdpState
    {
        public IPEndPoint EndPoint { get; }
        public UdpClient UdpClient { get; }

        public UdpState(UdpClient udpClient, IPEndPoint endPoint)
        {
            EndPoint = endPoint;
            UdpClient = udpClient;
        }
    }
}