using System.Text.Json;

namespace NanoleafAPI
{
    public readonly struct Command
    {
        public readonly object AnonymousType;
        public readonly object? Payload = null;
        public Command(in object anonymousType, in object? payload = null)
        {
            this.AnonymousType = anonymousType;
            this.Payload = payload;
        }
        public override string ToString()
        {
            if (Payload == null)
            {
                return JsonSerializer.Serialize(this.AnonymousType);
            }
            else
            {
                string command = JsonSerializer.Serialize(this.AnonymousType);
                string payload = JsonSerializer.Serialize(this.Payload);

                payload = payload.Remove(0, 1);
                payload = payload.Remove(payload.Length - 1, 1);
                string ret = command.Replace("\"PAYLOAD\":\"\"", payload);
                return ret;
            }
        }
    }
}