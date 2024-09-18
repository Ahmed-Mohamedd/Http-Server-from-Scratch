using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_http_server.src.Services.Interfaces
{
    public interface IMethodhandler
    {
        Task<string>  HandleGetRequest(string path, NetworkStream stream);
        Task<string> HandlePostRequest(StreamReader reader, NetworkStream stream);
        Task<string> HandleNotAllowedRequest();

        Task<string> ServeFile(string path, NetworkStream stream , string ContentType);

    }
}
