using System;
using System.Drawing;
using BedLightESP.Settings;

namespace BedLightESP.Helper
{
    internal class ColorHelper
    {
        public static string ColorToHex(Color color)
        {
            string value =  $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            return value; 
                
        }

        public static Color HexToColor(string hex)
        {
            if (hex.StartsWith("#"))
            {
                hex = hex.Substring(1);
            }

            if (hex.Length != 6)
            {
                throw new ArgumentException("Hex color code must be 6 characters long.");
            }

            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);

            return Color.FromArgb(r, g, b);
        }

        public static Color[] GenerateRandomColorGradient(int length)
        {
            Random random = new();

            // Generate two random colors
            Color startColor = Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
            Color endColor = Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));

            Color[] colorArray = new Color[length];

            for (int i = 0; i < length; i++)
            {
                // Calculate the ratio of interpolation (i / (length - 1))
                float ratio = (float)i / (length - 1);

                // Interpolate between the two colors
                int r = (int)(startColor.R + ratio * (endColor.R - startColor.R));
                int g = (int)(startColor.G + ratio * (endColor.G - startColor.G));
                int b = (int)(startColor.B + ratio * (endColor.B - startColor.B));

                colorArray[i] = Color.FromArgb(r, g, b);
            }

            return colorArray;
        }

        public static Color[] CalculateRandomGradientHUE(int size)
        {
            Random random = new();

            int number1 = random.Next(361);
            int number2 = random.Next(361);

            // Generate two random colors
            Color startColor = new HSLColor(number1, 100, 50);
            Color endColor = new HSLColor(number2, 100, 50);

            return CalculateGradientHUE(startColor, endColor, size);
        }

        public static Color[] CalculateGradientHUE(Color startColor, Color endColor, int size)
        {
            Color[] colors = new Color[size];
            HSLColor startHlsColor = startColor;
            HSLColor endHlsColor = endColor;
            int discreteUnits = size;

            for (int i = 0; i < discreteUnits; i++)
            {
                var hueAverage = endHlsColor.Hue + (int)((startHlsColor.Hue - endHlsColor.Hue) * i / size);
                var saturationAverage = endHlsColor.Saturation;// + (int)((startHlsColor.Saturation - endHlsColor.Saturation) * i / size);
                var luminosityAverage = endHlsColor.Luminosity;// + (int)((startHlsColor.Luminosity - endHlsColor.Luminosity) * i / size);

                colors[i] = (Color)new HSLColor(hueAverage, saturationAverage, luminosityAverage);
            }
            return colors;
        }
    }
}