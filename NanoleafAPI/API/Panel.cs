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
        public readonly struct RGBW
        {
            public readonly byte R;
            public readonly byte G;
            public readonly byte B;
            public readonly byte W;

            public RGBW(byte r, byte g, byte b) : this()
            {
                R = r;
                G = g;
                B = b;
            }

            public RGBW(byte r, byte g, byte b, byte w) : this()
            {
                R = r;
                G = g;
                B = b;
                W = w;
            }
            public static bool operator ==(RGBW c1, RGBW c2)
            {
                if (c1.R != c2.R)
                    return false;
                if (c1.G != c2.G)
                    return false;
                if (c1.B != c2.B)
                    return false;
                if (c1.W!= c2.W)
                    return false;

                return true;
            }

            public static bool operator !=(RGBW c1, RGBW c2)
            {
                if (c1.R != c2.R)
                    return true;
                if (c1.G != c2.G)
                    return true;
                if (c1.B != c2.B)
                    return true;
                if (c1.W != c2.W)
                    return true;

                return false;
            }
            public override string ToString()
            {
                return $"{R}; {G}; {B}; {W}";
            }

#pragma warning disable CS8765
            public override bool Equals(object obj)
#pragma warning restore CS8765
            {
                if (obj is RGBW rgbw)
                    return this == rgbw;

                return false;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(R, G, B, W);
            }
        }
    }
}
