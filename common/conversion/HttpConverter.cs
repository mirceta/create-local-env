using si.birokrat.next.common.encoding;
using si.birokrat.next.common.serialization;
using System.Text;
using System.Web;

namespace si.birokrat.next.common.conversion {
    public static class HttpConverter {
        public static string Encode(string value, Encoding encoding = null) =>
            HttpUtility.UrlEncode(value, encoding ?? Encoding.UTF8);

        public static string EncodeUrl(string value, string encodingName = "utf-8") =>
            Encode(value, EncodingUtils.FindByName(encodingName));

        public static string EncodeUrl(string value, int encodingCodePage = 65001) =>
            Encode(value, EncodingUtils.FindByCodePage(encodingCodePage));

        public static string DecodeUrl(string value, Encoding encoding = null) =>
            HttpUtility.UrlDecode(value, encoding ?? Encoding.UTF8);

        public static string DecodeUrl(string value, string encodingName = "utf-8") =>
            DecodeUrl(value, EncodingUtils.FindByName(encodingName));

        public static string DecodeUrl(string value, int encodingCodePage = 65001) =>
            DecodeUrl(value, EncodingUtils.FindByCodePage(encodingCodePage));

        public static string SerializeAndEncode<T>(T value, Encoding encoding = null) =>
            Encode(Serializer.ToJson(value), encoding ?? Encoding.UTF8);

        public static string EncodeAndSerialize<T>(T value, string encodingName = "utf-8") =>
            SerializeAndEncode(value, EncodingUtils.FindByName(encodingName));

        public static string EncodeAndSerialize<T>(T value, int encodingCodePage = 65001) =>
            SerializeAndEncode(value, EncodingUtils.FindByCodePage(encodingCodePage));

        public static T DecodeAndDeserialize<T>(string value, Encoding encoding = null) =>
            Serializer.FromJson<T>(DecodeUrl(value, encoding ?? Encoding.UTF8));

        public static T DecodeAndDeserialize<T>(string value, string encodingName = "utf-8") =>
            DecodeAndDeserialize<T>(value, EncodingUtils.FindByName(encodingName));

        public static T DecodeAndDeserialize<T>(string value, int encodingCodePage = 65001) =>
            DecodeAndDeserialize<T>(value, EncodingUtils.FindByCodePage(encodingCodePage));
    }
}
