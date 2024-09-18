using codecrafters_http_server.src.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_http_server.src.Services
{
    public class Methodhandler : IMethodhandler 
    {


        public async Task<string> HandleGetRequest(string path, NetworkStream stream)
        {
            var responseBody = string.Empty;
            var responseHeaders = string.Empty;
            if (path == "/" || path.ToLower() == "/home")
            {
                 responseBody = "<h1>Welcome to the Homepage</h1>";
                // Compress the response body using Gzip
                byte[] compressedBody = await CompressGzipData(responseBody);
                // Create response headers with Gzip encoding
                responseHeaders = "HTTP/1.1 200 OK\r\n" +
                                        "Content-Type: text/html\r\n" +
                                        "Content-Encoding: gzip\r\n" +
                                        $"Content-Length: {compressedBody.Length}\r\n\r\n";
                                         
            }
            else if (path.ToLower() == "/about")
            {
                responseBody = "<h1>About Page</h1>";
                byte[] compressedBody = await CompressGzipData(responseBody);
                responseHeaders = "HTTP/1.1 200 OK\r\n" +
                                         "Content-Type: text/html\r\n" +
                                         "Content-Encoding: gzip\r\n" +
                                         $"Content-Length: {compressedBody.Length}\r\n" +
                                         $"\r\n";
            }
            else
            {
                byte[] compressedBody = await CompressGzipData(responseBody);
                responseHeaders = "HTTP/1.1 200 OK\r\n" +
                                         "Content-Type: text/html\r\n" +
                                         "Content-Encoding: gzip\r\n" +
                                         $"Content-Length: {compressedBody.Length}\r\n" +
                                         $"\r\n";
            }
            return responseHeaders;
        }

        public async Task<string> HandleNotAllowedRequest()
            => "HTTP/1.1 405 Method Not Allowed\r\nContent-Type: text/html\r\n\r\n<h1>Method Not Allowed</h1>";

        public async Task<string> HandlePostRequest(StreamReader reader, NetworkStream stream)
        {
            string line;
            int contentLength = 0;
            while (!string.IsNullOrWhiteSpace(line = await reader.ReadLineAsync()))
            {
                if (line.StartsWith("Content-Length:"))
                {
                    contentLength = int.Parse(line.Split(':')[1].Trim());
                    Console.WriteLine( $"contentLength:{contentLength}");
                }
            }

            // Read the body (form data or JSON)
            char[] bodyBuffer = new char[contentLength];
            await reader.ReadAsync(bodyBuffer, 0, contentLength);
            string body = new string(bodyBuffer);

            Console.WriteLine("Received POST data: " + body);

            // Process POST data here (e.g., save to a database)
            var response = "HTTP/1.1 200 OK\r\nContent-Type: text/html\r\n\r\n<h1>POST Data Received</h1>";
            return response;
        }

        public async Task<string> ServeFile(string filePath, NetworkStream stream , string ContentType)
        {
            
            string contentType = ContentType;
            // Read the file contents compressed
            byte[] compressedFileBytes = await CompressGzip(filePath);
            // Create the response headers
            string response = $"HTTP/1.1 200 OK\r\n" +
                              $"Content-Type: {contentType}\r\n" +
                              "Content-Encoding: gzip\r\n" +
                              $"Content-Length: {compressedFileBytes.Length}\r\n" +
                              "\r\n";
            return response;

        }

        static async Task<byte[]> CompressGzip(string filePath )
        {
            byte[] bytes = await File.ReadAllBytesAsync( filePath );
          

            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    await gzipStream.WriteAsync(bytes, 0, bytes.Length);
                }
                return memoryStream.ToArray();
            }
        }

        // Helper method to compress data using Gzip
        static async Task<byte[]> CompressGzipData(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);

            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    await gzipStream.WriteAsync(bytes, 0, bytes.Length);
                }
                return memoryStream.ToArray();
            }
        }
    }
}
