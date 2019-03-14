using si.birokrat.next.common.conversion;
using si.birokrat.next.common.exceptions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace si.birokrat.next.common.networking {
    public static class Http {
        public async static Task<string> Request(string baseUri, string requestUri, string method = "Get", Dictionary<string, string> headers = null,
            string data = "", string contentType = "application/json") {
            var client = new HttpClient { BaseAddress = new Uri(baseUri) };

            var request = new HttpRequestMessage(new HttpMethod(method), requestUri);
            if (headers != null) {
                foreach (var header in headers) {
                    request.Headers.Add(header.Key, HttpConverter.Encode(header.Value, Encoding.UTF8));
                }
            }
            if (!string.IsNullOrEmpty(data)) {
                request.Content = new StringContent(data);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }

            string content = null;
            try {
                var response = await client.SendAsync(request);
                content = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK) {
                    throw new MessageCodeException(content, (int)response.StatusCode);
                }
            } catch (HttpRequestException ex) {
                throw new MessageCodeException(ex.Message);
            }

            return content;
        }
    }
}
