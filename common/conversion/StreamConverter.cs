using si.birokrat.next.common.encoding;
using System.IO;
using System.Text;

namespace si.birokrat.next.common.conversion {
    public static class StreamConverter {
        public static MemoryStream StringToStream(string data, Encoding encoding = null) {
            if (encoding == null) {
                encoding = Encoding.UTF8;
            }
            return new MemoryStream(encoding.GetBytes(data ?? string.Empty));
        }

        public static MemoryStream StringToStream(string data, string encodingName = "utf-8") {
            var encoding = EncodingUtils.FindByName(encodingName);
            return encoding == null ? null : new MemoryStream(encoding.GetBytes(data ?? string.Empty));
        }

        public static MemoryStream StringToStream(string data, int encodingCodePage = 65001) {
            var encoding = EncodingUtils.FindByCodePage(encodingCodePage);
            return encoding == null ? null : new MemoryStream(encoding.GetBytes(data ?? string.Empty));
        }

        public static string StreamToString(MemoryStream data, Encoding encoding = null) {
            if (encoding == null) {
                encoding = Encoding.UTF8;
            }
            return encoding?.GetString(data.ToArray());
        }

        public static string StreamToString(MemoryStream data, string encodingName = "utf-8") {
            var encoding = EncodingUtils.FindByName(encodingName);
            return encoding?.GetString(data.ToArray());
        }

        public static string StreamToString(MemoryStream data, int encodingCodePage = 65001) {
            var encoding = EncodingUtils.FindByCodePage(encodingCodePage);
            return encoding?.GetString(data.ToArray());
        }

        public static string StreamToString(Stream data, Encoding encoding = null) {
            MemoryStream stream = new MemoryStream();
            data.CopyTo(stream);
            return StreamToString(stream, encoding);
        }

        public static string StreamToString(Stream data, string encodingName = "utf-8") {
            MemoryStream stream = new MemoryStream();
            data.CopyTo(stream);
            return StreamToString(stream, encodingName);
        }

        public static string StreamToString(Stream data, int encodingCodePage = 65001) {
            MemoryStream stream = new MemoryStream();
            data.CopyTo(stream);
            return StreamToString(stream, encodingCodePage);
        }
    }
}
