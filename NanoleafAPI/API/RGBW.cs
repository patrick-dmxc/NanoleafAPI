namespace NanoleafAPI
{
    public readonly struct RGBW
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
        public readonly byte W;

        public RGBW(in byte r, in byte g, in byte b) : this()
        {
            R = r;
            G = g;
            B = b;
        }

        public RGBW(in byte r, in byte g, in byte b, in byte w) : this()
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
            if (c1.W != c2.W)
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