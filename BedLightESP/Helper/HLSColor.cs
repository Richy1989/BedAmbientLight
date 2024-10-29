using System;
using System.Drawing;

namespace BedLightESP.Helper
{
    public class HSLColor
    {
        // Private data members below are on scale 0-1
        // They are scaled for use externally based on scale
        private double hue = 1.0;
        private double saturation = 1.0;
        private double luminosity = 1.0;

        public const double scale = 360.0;
        private const double percentage = 100.0;

        public static double Scale
        {
            get { return scale; }
        }

        public double Hue
        {
            get { return hue * scale; }
            set { hue = CheckRange(value / scale); }
        }

        public double Saturation
        {
            get { return saturation * percentage; }
            set { saturation = CheckRange(value / percentage); }
        }

        public double Luminosity
        {
            get { return luminosity * percentage; }
            set { luminosity = CheckRange(value / percentage); }
        }

        private double CheckRange(double value)
        {
            if (value < 0.0)
                value = 0.0;
            else if (value > 1.0)
                value = 1.0;
            return value;
        }

        public override string ToString()
        {
            return string.Format("H: {0:0.##} S: {1:0.##} L: {2:0.##}", Hue, Saturation, Luminosity);
        }

        public string ToRGBString()
        {
            Color color = (Color)this;
            return string.Format("R: {0:0.##} G: {1:0.##} B: {2:0.##}", color.R, color.G, color.B);
        }

        #region Casts to/from System.Drawing.Color
        public static implicit operator Color(HSLColor hslColor)
        {
            double r = 0, g = 0, b = 0;
            if (hslColor.luminosity != 0)
            {
                if (hslColor.saturation == 0)
                    r = g = b = hslColor.luminosity;
                else
                {
                    double temp2 = GetTemp2(hslColor);
                    double temp1 = 2.0 * hslColor.luminosity - temp2;

                    r = GetColorComponent(temp1, temp2, hslColor.hue + 1.0 / 3.0);
                    g = GetColorComponent(temp1, temp2, hslColor.hue);
                    b = GetColorComponent(temp1, temp2, hslColor.hue - 1.0 / 3.0);
                }
            }
            return Color.FromArgb((int)(255 * r), (int)(255 * g), (int)(255 * b));
        }

        private static double GetColorComponent(double temp1, double temp2, double temp3)
        {
            temp3 = MoveIntoRange(temp3);
            if (temp3 < 1.0 / 6.0)
                return temp1 + (temp2 - temp1) * 6.0 * temp3;
            else if (temp3 < 0.5)
                return temp2;
            else if (temp3 < 2.0 / 3.0)
                return temp1 + (temp2 - temp1) * (2.0 / 3.0 - temp3) * 6.0;
            else
                return temp1;
        }

        private static double MoveIntoRange(double temp3)
        {
            if (temp3 < 0.0)
                temp3 += 1.0;
            else if (temp3 > 1.0)
                temp3 -= 1.0;
            return temp3;
        }

        private static double GetTemp2(HSLColor hslColor)
        {
            double temp2;
            if (hslColor.luminosity < 0.5)  //<=??
                temp2 = hslColor.luminosity * (1.0 + hslColor.saturation);
            else
                temp2 = hslColor.luminosity + hslColor.saturation - hslColor.luminosity * hslColor.saturation;
            return temp2;
        }

        public static implicit operator HSLColor(Color color)
        {
            var hslColor = FromRGB(color.R, color.G, color.B);
            ////HSLColor hslColor = new HSLColor
            ////{
            ////    hue = color.GetHue() / 360.0, // we store hue as 0-1 as opposed to 0-360 
            ////    luminosity = color.GetBrightness(),
            ////    saturation = color.GetSaturation()
            ////};
            return hslColor;
        }
        #endregion

        public void SetRGB(int red, int green, int blue)
        {
            HSLColor hslColor = (HSLColor)Color.FromArgb(red, green, blue);
            hue = hslColor.hue;
            saturation = hslColor.saturation;
            luminosity = hslColor.luminosity;
        }

        public HSLColor() { }

        public HSLColor(Color color)
        {
            var hslColor = HSLColor.FromRGB(color.R, color.G, color.B);
            this.hue = hslColor.hue;
            this.saturation = hslColor.saturation;
            this.luminosity = hslColor.luminosity;
        }

        private HSLColor(int red, int green, int blue)
        {
            SetRGB(red, green, blue);
        }

        public HSLColor(double hue, double saturation, double luminosity)
        {
            Hue = hue;
            Saturation = saturation;
            Luminosity = luminosity;
        }

        public static HSLColor FromRGB(Color color) => FromRGB(color.R, color.G, color.B);

        public static HSLColor FromRGB(byte R, byte G, byte B)
        {
            float _R = (R / 255f);
            float _G = (G / 255f);
            float _B = (B / 255f);

            float _Min = Math.Min(Math.Min(_R, _G), _B);
            float _Max = Math.Max(Math.Max(_R, _G), _B);
            float _Delta = _Max - _Min;

            float H = 0;
            float S = 0;
            float L = (float)((_Max + _Min) / 2.0f);

            if (_Delta != 0)
            {
                if (L < 0.5f)
                {
                    S = (float)(_Delta / (_Max + _Min));
                }
                else
                {
                    S = (float)(_Delta / (2.0f - _Max - _Min));
                }


                if (_R == _Max)
                {
                    H = (_G - _B) / _Delta;
                }
                else if (_G == _Max)
                {
                    H = 2f + (_B - _R) / _Delta;
                }
                else if (_B == _Max)
                {
                    H = 4f + (_R - _G) / _Delta;
                }
            }

            //Convert to degrees
            H = H * 60f;
            if (H < 0) H += 360;

            return new HSLColor(H, S * percentage, L * percentage);
        }
    }
}
