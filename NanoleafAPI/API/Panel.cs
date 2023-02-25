﻿using Newtonsoft.Json.Linq;
using static NanoleafAPI.PanelPosition;

namespace NanoleafAPI
{
    public class Panel
    {
        public string IP { get; private set; }
        public int ID { get; private set; }
        private int x;
        public int X
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
        private int y;
        public int Y
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
        private int orientation;
        public int Orientation
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
        public Panel(JToken json)
        {
#pragma warning disable CS8604
#pragma warning disable CS8600
#pragma warning disable CS8601
            IP = (string)json[nameof(IP)];
            ID = (int)json[nameof(ID)];
            X = (int)json[nameof(X)];
            Y = (int)json[nameof(Y)];
            Orientation = (int)json[nameof(Orientation)];
            Shape = (EShapeType)(int)json[nameof(Shape)];
            SideLength = (double)json[nameof(SideLength)];
#pragma warning restore CS8601
#pragma warning restore CS8604
#pragma warning restore CS8600
            Communication.StaticOnLayoutEvent += Communication_StaticOnLayoutEvent;
        }

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

            var pp = e.LayoutEvent.Layout?.PanelPositions?.FirstOrDefault(p => p.PanelId.Equals(ID));
            if (pp != null)
            {
                X = pp.X;
                Y = pp.Y;
                Orientation = pp.Orientation;
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
                return c1.Equals(c2);
            }

            public static bool operator !=(RGBW c1, RGBW c2)
            {
                return !c1.Equals(c2);
            }
            public override string ToString()
            {
                return $"{R}; {G}; {B}; {W}";
            }

            public override bool Equals(object obj)
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
