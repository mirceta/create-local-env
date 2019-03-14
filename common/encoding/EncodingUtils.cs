using System.Text;

namespace si.birokrat.next.common.encoding {
    public static class EncodingUtils {
        public static Encoding FindByName(string encodingName) {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(encodingName);
        }

        public static Encoding FindByCodePage(int encodingCodePage) {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(encodingCodePage);
        }
    }
}
