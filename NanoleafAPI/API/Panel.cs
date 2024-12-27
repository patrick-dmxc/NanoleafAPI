using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public class Panel : SubDevice
    {
        public EPanelType PanelType { get; protected set; }

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

        public double SideLength { get; internal set; }

        public event EventHandler XChanged;
        public event EventHandler YChanged;
        public event EventHandler OrientationChanged;

#pragma warning disable CS8618
        public Panel(string ip, PanelPosition pp) : base(ip, pp.PanelId)
        {
            PanelType = pp.ShapeType;
            X = pp.X;
            Y = pp.Y;
            Orientation = pp.Orientation;
            SideLength = pp.SideLength;
            Communication.StaticOnLayoutEvent += Communication_StaticOnLayoutEvent;
        }
        [JsonConstructor]
        public Panel(string ip, int id,float x, float y, float orientation, EPanelType panelType, double sideLength) : base(ip, id)
        {
            PanelType = panelType;
            X = x;
            Y = y;
            Orientation = orientation;
            SideLength = sideLength;
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
