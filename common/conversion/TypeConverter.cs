using si.birokrat.next.common.encoding;
using System;
using System.Globalization;
using System.Text;

namespace si.birokrat.next.common.conversion {
    public static class TypeConverter {
        public static bool StringToBoolean(string data) {
            return bool.Parse(data);
        }

        public static string BooleanToString(bool data) {
            return data.ToString();
        }

        public static byte StringToByte(string data) {
            return byte.Parse(data);
        }

        public static string ByteToString(byte data) {
            return data.ToString();
        }

        public static short StringToShort(string data) {
            return short.Parse(data);
        }

        public static string ShortToString(short data) {
            return data.ToString();
        }

        public static int StringToInteger(string data) {
            return int.Parse(data);
        }

        public static string IntegerToString(int data) {
            return data.ToString();
        }

        public static long StringToLong(string data) {
            return long.Parse(data);
        }

        public static string LongToString(float data) {
            return data.ToString();
        }

        public static float StringToFloat(string data, bool slovene = false) {
            return float.Parse(data, slovene ? new CultureInfo("sl-SI") : new CultureInfo("en-US"));
        }

        public static string FloatToString(float data, string format = "###,###,##0.00", bool slovene = false) {
            return data.ToString(format, slovene ? new CultureInfo("sl-SI") : new CultureInfo("en-US"));
        }

        public static string FloatToEur(float data) {
            return FloatToString(data) + " \u20AC";
        }

        public static double StringToDouble(string data, bool slovene = false) {
            return double.Parse(data, slovene ? new CultureInfo("sl-SI") : new CultureInfo("en-US"));
        }

        public static string DoubleToString(double data, string format = "###,###,##0.00", bool slovene = false) {
            return data.ToString(format, slovene ? new CultureInfo("sl-SI") : new CultureInfo("en-US"));
        }

        public static string DoubleToEur(double data) {
            return DoubleToString(data) + " \u20AC";
        }

        public static decimal StringToDecimal(string data, bool slovene = false) {
            return decimal.Parse(data, slovene ? new CultureInfo("sl-SI") : new CultureInfo("en-US"));
        }

        public static string DecimalToString(decimal data, string format = "###,###,##0.00", bool slovene = false) {
            return data.ToString(format, slovene ? new CultureInfo("sl-SI") : new CultureInfo("en-US"));
        }

        public static string DecimalToEur(decimal data) {
            return DecimalToString(data) + " \u20AC";
        }

        public static DateTime StringToDateTime(string data, string format) {
            return DateTime.ParseExact(data, format, CultureInfo.InvariantCulture);
        }

        public static string DateTimeToString(DateTime? data, string format) {
            return data == null ? string.Empty : ((DateTime)data).ToString(format);
        }

        public static string ReformatDateTime(string data, string fromFormat, string toFormat) {
            return StringToDateTime(data, fromFormat).ToString(toFormat);
        }

        public static string ReencodeString(string value, Encoding sourceEncoding = null, Encoding targetEncoding = null) {
            if (sourceEncoding == null) {
                sourceEncoding = Encoding.Default;
            }
            if (targetEncoding == null) {
                targetEncoding = Encoding.UTF8;
            }
            var bytes = sourceEncoding.GetBytes(value);
            return targetEncoding.GetString(bytes);
        }

        public static string ReencodeString(string value, string sourceEncodingName = "", string targetEncodingName = "utf-8") {
            var sourceEncoding = string.IsNullOrEmpty(sourceEncodingName) ? Encoding.Default : EncodingUtils.FindByName(sourceEncodingName);
            var targetEncoding = EncodingUtils.FindByName(targetEncodingName);
            return ReencodeString(value, sourceEncoding, targetEncoding);
        }

        public static string ReencodeString(string value, int sourceEncodingCodePage = -1, int targetEncodingCodePage = 65001) {
            var sourceEncoding = sourceEncodingCodePage == -1 ? Encoding.Default : EncodingUtils.FindByCodePage(sourceEncodingCodePage);
            var targetEncoding = EncodingUtils.FindByCodePage(targetEncodingCodePage);
            return ReencodeString(value, sourceEncoding, targetEncoding);
        }
    }
}
