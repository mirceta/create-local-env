using System.Drawing;

namespace si.birokrat.next.common.conversion {
    public static class ColorConverter {
        public static string OleToHex(string value) {
            var color = ColorTranslator.FromOle(TypeConverter.StringToInteger(value));
            return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }
    }
}
