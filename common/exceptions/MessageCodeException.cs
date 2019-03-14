using System;

namespace si.birokrat.next.common.exceptions {
    public class MessageCodeException : Exception {
        public MessageCodeException(string message, int code = 0)
            : base(message) {
            Code = code;
        }

        public int Code { get; }
    }
}
