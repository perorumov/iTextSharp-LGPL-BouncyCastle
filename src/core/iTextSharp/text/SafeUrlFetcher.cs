using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace iTextSharp.text {
    internal static class SafeUrlFetcher {
        private const int TimeoutMilliseconds = 15000;
        private const int MaximumRedirects = 5;
        private const int BufferSize = 8192;
        private const long MaximumResponseBytes = 64L * 1024 * 1024;

        internal static Stream GetStream(Uri uri) {
            return new MemoryStream(Get(uri));
        }

        internal static byte[] Get(Uri uri) {
            string contentEncoding;
            return Fetch(uri, "GET", null, null, null, null, true, out contentEncoding);
        }

        internal static byte[] Post(Uri uri, string contentType, string accept, byte[] requestBody, string authorization, out string contentEncoding) {
            return Fetch(uri, "POST", contentType, accept, requestBody, authorization, false, out contentEncoding);
        }

        private static byte[] Fetch(Uri uri, string method, string contentType, string accept, byte[] requestBody, string authorization, bool followRedirects, out string contentEncoding) {
            Uri currentUri = uri;
            Stopwatch elapsed = Stopwatch.StartNew();
            for (int redirectCount = 0; ; ++redirectCount) {
                ValidateUri(currentUri);
                int remainingTimeout = TimeoutMilliseconds - (int)elapsed.ElapsedMilliseconds;
                if (remainingTimeout <= 0)
                    throw new WebException("The URL request timed out.", WebExceptionStatus.Timeout);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(currentUri);
                request.AllowAutoRedirect = false;
                request.Proxy = null;
                request.Timeout = remainingTimeout;
                request.ReadWriteTimeout = remainingTimeout;
                request.Method = method;
                request.KeepAlive = false;
                if (contentType != null)
                    request.ContentType = contentType;
                if (accept != null)
                    request.Accept = accept;
                if (authorization != null)
                    request.Headers[HttpRequestHeader.Authorization] = authorization;
                if (requestBody != null) {
                    request.ContentLength = requestBody.Length;
                    using (Stream requestStream = request.GetRequestStream())
                        requestStream.Write(requestBody, 0, requestBody.Length);
                }

                HttpWebResponse response;
                try {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException exception) {
                    response = exception.Response as HttpWebResponse;
                    if (response == null)
                        throw;
                }

                int statusCode = (int)response.StatusCode;
                if (IsRedirect(statusCode)) {
                    string location = response.Headers[HttpResponseHeader.Location];
                    response.Close();
                    if (!followRedirects || redirectCount >= MaximumRedirects)
                        throw new IOException("The URL response redirected too many times or redirects are not permitted.");
                    if (String.IsNullOrEmpty(location))
                        throw new IOException("The URL redirect did not specify a destination.");
                    currentUri = new Uri(currentUri, location);
                    continue;
                }
                if (statusCode < 200 || statusCode >= 300) {
                    response.Close();
                    throw new IOException("Invalid HTTP response: " + statusCode);
                }

                contentEncoding = response.ContentEncoding;
                using (response) {
                    return ReadBounded(response, elapsed);
                }
            }
        }

        private static byte[] ReadBounded(HttpWebResponse response, Stopwatch elapsed) {
            if (response.ContentLength > MaximumResponseBytes)
                throw new IOException("The URL response exceeds the maximum permitted size.");

            using (Stream input = response.GetResponseStream()) {
                using (MemoryStream output = new MemoryStream()) {
                    byte[] buffer = new byte[BufferSize];
                    int count;
                    while (true) {
                        int remainingTimeout = TimeoutMilliseconds - (int)elapsed.ElapsedMilliseconds;
                        if (remainingTimeout <= 0)
                            throw new WebException("The URL request timed out.", WebExceptionStatus.Timeout);
                        if (input.CanTimeout)
                            input.ReadTimeout = remainingTimeout;
                        count = input.Read(buffer, 0, buffer.Length);
                        if (count == 0)
                            break;
                        if (output.Length > MaximumResponseBytes - count)
                            throw new IOException("The URL response exceeds the maximum permitted size.");
                        output.Write(buffer, 0, count);
                    }
                    return output.ToArray();
                }
            }
        }

        private static bool IsRedirect(int statusCode) {
            return statusCode == 301 || statusCode == 302 || statusCode == 303 || statusCode == 307 || statusCode == 308;
        }

        private static void ValidateUri(Uri uri) {
            if (uri == null || !uri.IsAbsoluteUri || !String.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) || !String.IsNullOrEmpty(uri.UserInfo))
                throw new IOException("Only absolute HTTPS URLs without user information are permitted.");

            IPAddress literalAddress;
            IPAddress[] addresses;
            if (IPAddress.TryParse(uri.DnsSafeHost, out literalAddress))
                addresses = new IPAddress[] { literalAddress };
            else {
                try {
                    addresses = Dns.GetHostAddresses(uri.DnsSafeHost);
                }
                catch (Exception exception) {
                    throw new IOException("The URL host could not be resolved.", exception);
                }
            }

            if (addresses == null || addresses.Length == 0)
                throw new IOException("The URL host did not resolve to an address.");
            foreach (IPAddress address in addresses) {
                if (!IsPublicAddress(address))
                    throw new IOException("The URL host resolved to a non-public IP address.");
            }
        }

        private static bool IsPublicAddress(IPAddress address) {
            byte[] bytes = address.GetAddressBytes();
            if (address.AddressFamily == AddressFamily.InterNetworkV6) {
                bool mappedIpv4 = true;
                for (int i = 0; i < 10; ++i)
                    mappedIpv4 &= bytes[i] == 0;
                mappedIpv4 &= bytes[10] == 0xff && bytes[11] == 0xff;
                if (mappedIpv4)
                    return IsPublicIpv4(bytes[12], bytes[13], bytes[14], bytes[15]);

                if ((bytes[0] & 0xe0) != 0x20 || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast || IPAddress.IsLoopback(address))
                    return false;
                if (bytes[0] == 0x20 && bytes[1] == 0x01 && (bytes[2] == 0x00 || bytes[2] == 0x02 || bytes[2] == 0x10 || (bytes[2] == 0x0d && bytes[3] == 0xb8)))
                    return false;
                return true;
            }

            if (address.AddressFamily != AddressFamily.InterNetwork)
                return false;
            return IsPublicIpv4(bytes[0], bytes[1], bytes[2], bytes[3]);
        }

        private static bool IsPublicIpv4(byte a, byte b, byte c, byte d) {
            if (a == 0 || a == 10 || a == 127 || a >= 224)
                return false;
            if (a == 100 && b >= 64 && b <= 127)
                return false;
            if (a == 169 && b == 254)
                return false;
            if (a == 172 && b >= 16 && b <= 31)
                return false;
            if (a == 192 && (b == 0 || (b == 168) || (b == 88 && c == 99)))
                return false;
            if (a == 192 && b == 0 && c == 2)
                return false;
            if (a == 198 && (b == 18 || b == 19 || (b == 51 && c == 100)))
                return false;
            if (a == 203 && b == 0 && c == 113)
                return false;
            return true;
        }
    }
}
