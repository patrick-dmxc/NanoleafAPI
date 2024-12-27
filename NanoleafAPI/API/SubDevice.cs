using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public class SubDevice
    {
        public string IP { get; protected set; }
        public int ID { get; protected set; }

        private RGBW streamingColor;
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public RGBW StreamingColor
        {
            get { return streamingColor; }
            set
            {
                if (streamingColor == value)
                    return;

                streamingColor = value;
                LastUpdate = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;
            }
        }
        public double LastUpdate
        {
            get;
            protected set;
        }

#pragma warning disable CS8618
        [JsonConstructor]
        public SubDevice(string ip, int id)
        {
            IP = ip;
            ID = id;
        }
#pragma warning restore CS8618

        public override string ToString()
        {
            return $"ID: {ID}";
        }
    }
}
