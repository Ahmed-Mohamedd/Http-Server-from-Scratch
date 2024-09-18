using codecrafters_http_server.src.Services;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;



internal class Program
{
    private static async Task Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("Server started on port 4221...");

        while (true)
        {
            // Accept a new client connection asynchronously
            var client = await server.AcceptTcpClientAsync();

            #region Handle the client connection in a new thread

            //// Handle the client connection in a new thread
            //Thread thread = new Thread(() => HandleClient(client));
            //thread.Start();
            #endregion

            #region Queue the client connection to the thread pool

            //// Queue the client connection to the thread pool
            //_=ThreadPool.QueueUserWorkItem(HandleClient, client);
            #endregion

            // Handle the client connection asynchronously
            _ = Task.Run(() => HandleClientAsync(client));

        }

    }

    static async Task HandleClientAsync(TcpClient client)
    {

        var stream = client.GetStream();

        var reader = new StreamReader(stream);
        string request = await reader.ReadLineAsync();
        Console.WriteLine("Received request: " + request);

        var requestLines = request.Split("\r\n");
        var line0Parts = requestLines[0].Split(" ");
        var (method, path, httpVer) = (line0Parts[0], line0Parts[1], line0Parts[2]); /*  Get / Http/1.1  */

        await CompleteRoute(method, path, stream, reader);

        client.Close();
    }

    static async Task CompleteRoute(string method, string path, NetworkStream stream, StreamReader reader)
    {
        var handleMethod = new Methodhandler();

        if (method == "GET"&&(path.Contains(".txt")||path.Contains(".png")||path.Contains(".jpg")||path.Contains(".pdf")))
        {
            path = path.Trim('/');
            path = $"E:\\Back End\\Http Server From Scratch\\codecrafters-http-server-csharp~\\src\\Images\\{path}";
            if (Path.Exists(path))
            {
                // Get the MIME type based on the file extension
                var ContentType = GetContentType(path);
                var responsee = await handleMethod.ServeFile(path, stream, ContentType);
                await SendResponse(stream, responsee, path);
            }
            else
            {
                await handleMethod.HandleGetRequest("**", stream);
            }
        }

        if (method == "GET" && (!path.Contains(".txt")||!path.Contains(".png")||!path.Contains(".jpg")||!path.Contains(".pdf")))
        {
            var response = await handleMethod.HandleGetRequest(path, stream);
            await SendResponse(stream, response);
        }
        else if (method == "POST")
        {
            var response = await handleMethod.HandlePostRequest(reader, stream);
            await SendResponse(stream, response);
        }
        else
        {
            var response = await handleMethod.HandleNotAllowedRequest();
            await SendResponse(stream, response);
        }
    }


    static async Task SendGzipCompressedResponse(string responseBody, NetworkStream stream, TcpClient client)
    {
        try
        {
            // Compress the response body using Gzip
            byte[] compressedBody = await CompressGzip(responseBody);

            // Create response headers with Gzip encoding
            string responseHeaders = "HTTP/1.1 200 OK\r\n" +
                                     "Content-Type: text/html\r\n" +
                                     "Content-Encoding: gzip\r\n" +
                                     $"Content-Length: {compressedBody.Length}\r\n" +
                                     "\r\n";

            byte[] headerBytes = Encoding.UTF8.GetBytes(responseHeaders);

            // Write headers and compressed body to the stream
            await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
            await stream.WriteAsync(compressedBody, 0, compressedBody.Length);

            await stream.FlushAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    static async Task SendResponse(NetworkStream stream, string response)
    {
        var buffer = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(buffer, 0, buffer.Length);
        
    }

    static async Task SendResponse(NetworkStream stream, string response , string filePath)
    {
        // Write the response headers and file content
        byte[] headerBytes = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(headerBytes, 0, headerBytes.Length);

        // Write the file content
        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
        byte[] CompressedFile = await CompressGzip(bytess: fileBytes);
        await stream.WriteAsync(CompressedFile, 0, CompressedFile.Length);

        await stream.FlushAsync();
    }



    // Helper method to compress data using Gzip
    static async Task<byte[]> CompressGzip( string data = null , byte[] bytess = null)
    {
        byte[] bytes = bytess ?? new byte[1024];
        if (!string.IsNullOrEmpty(data))
        {
            bytes = Encoding.UTF8.GetBytes(data);
        }
       
        using (var memoryStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                await gzipStream.WriteAsync(bytes, 0, bytes.Length);
            }
            return memoryStream.ToArray();
        }
    }

    // Function to read headers from the request
    static async Task<Dictionary<string, string>> ReadHeaders(StreamReader reader)
    {
        var headers = new Dictionary<string, string>();
        string line;

        // Read headers until we encounter a blank line
        while (!string.IsNullOrWhiteSpace(line = await reader.ReadLineAsync()))
        {
            var headerParts = line.Split(new[] { ": " }, 2, StringSplitOptions.None);
            if (headerParts.Length == 2)
            {
                headers[headerParts[0]] = headerParts[1];
            }
        }

        return headers;
    }


   static string GetContentType(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();

        // Simplified MIME type detection
        return extension switch
        {
            ".html" => "text/html",
            ".txt" => "text/plain",
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream", // Default binary type
        };
    }

}


#region test 
//var responseBuffer = new byte[1024];
//int receivedBytes = socket.Receive(responseBuffer);
//var lines = ASCIIEncoding.UTF8.GetString(responseBuffer).Split("\r\n");
//var response = "HTTP/1.1 200 OK\r\n\r\n";
//var line0Parts = lines[0].Split(" ");
//var (method, path, httpVer) = (line0Parts[0], line0Parts[1], line0Parts[2]);
// response = (path == "/" ? $"{httpVer} 200 OK\r\n\r\n"
//                           : $"{httpVer} 404 Not Found\r\n\r\n");

//socket.Send(ASCIIEncoding.UTF8.GetBytes(response));
#endregion



