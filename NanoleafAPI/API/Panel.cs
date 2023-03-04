using static NanoleafAPI.PanelPosition;

namespace NanoleafAPI
{
    public class Panel
    {
        public string IP { get; private set; }
        public int ID { get; private set; }
        private float x;
        public float X
        {
            get { return x; }
            private set
            {
                if (x == value)
                    return;

                x = value;
                XChanged?.InvokeFailSafe(this, EventArgs.Empty);
            }
        }
        private float y;
        public float Y
        {
            get { return y; }
            private set
            {
                if (y == value)
                    return;

                y = value;
                YChanged?.InvokeFailSafe(this, EventArgs.Empty);
            }
        }
        private float orientation;
        public float Orientation
        {
            get { return orientation; }
            private set
            {
                if (orientation == value)
                    return;

                orientation = value;
                OrientationChanged?.InvokeFailSafe(this, EventArgs.Empty);
            }
        }
        public EShapeType Shape { get; private set; }

        private RGBW streamingColor;
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
            private set;
        }

        public double SideLength { get; internal set; }

        public event EventHandler XChanged;
        public event EventHandler YChanged;
        public event EventHandler OrientationChanged;

#pragma warning disable CS8618
        public Panel(string ip, PanelPosition pp)
        {
            IP = ip;
            ID = pp.PanelId;
            X = pp.X;
            Y = pp.Y;
            Orientation = pp.Orientation;
            Shape = pp.ShapeType;
            SideLength = pp.SideLength;
            Communication.StaticOnLayoutEvent += Communication_StaticOnLayoutEvent;
        }
#pragma warning restore CS8618

        private void Communication_StaticOnLayoutEvent(object? sender, LayoutEventArgs e)
        {
            if (!IP.Equals(e.IP))
                return;

            foreach (var @event in e.LayoutEvents.Events)
            {
                if (@event.Layout.HasValue)
                {
                    var pp = @event.Layout.Value.PanelPositions?.FirstOrDefault(p => p.PanelId.Equals(ID));
                    if (pp.HasValue)
                    {
                        X = pp.Value.X;
                        Y = pp.Value.Y;
                        Orientation = pp.Value.Orientation;
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"ID: {ID} X: {X} Y: {y} Orientation: {Orientation}";
        }
    }
}
