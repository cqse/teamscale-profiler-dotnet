using System;
using System.Collections.Generic;
using System.Linq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace UploadDaemon_Test.Upload
{
    /// <summary>
    /// Mock server class that responds to requests at a given port.
    /// </summary>
    public class TeamscaleMockServer
    {
        private readonly WireMockServer Server;

        /// <summary>
        /// Constructor that starts the WireMockServer at the given port.
        /// </summary>
        /// <param name="port"></param>
        public TeamscaleMockServer(int port)
        {
            Server = WireMockServer.Start(port);
        }

        /// <summary>
        /// Sets the status code of the responses from this mock server.
        /// </summary>
        /// <param name="responseCode"></param>
        public void SetResponse(int responseCode)
        {
            this.Server.Given(Request.Create().UsingPost()).RespondWith(Response.Create().WithStatusCode(responseCode));
        }

        /// <summary>
        /// Get a list of requests that this server received.
        /// </summary>
        /// <returns></returns>
        public List<String> GetReceivedRequests()
        {
            return Server.LogEntries.Select(entry => entry.RequestMessage.Url).ToList();
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void StopServer()
        {
            Server.Stop();
        }
    }
}
