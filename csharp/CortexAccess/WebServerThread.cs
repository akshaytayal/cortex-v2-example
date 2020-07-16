using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using vtortola.WebSockets;
using vtortola.WebSockets.Deflate;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography.X509Certificates;

namespace CortexAccess
{
    public class WebServerThread
    {
        static bool mStop = false;

        static List<WebSocket> sockets = new List<WebSocket>();

        private static short port = 8080;
        private const string ipAddress = "127.0.0.1";
        private static BlockingCollection<string> outBoundMessageQueue = new BlockingCollection<string>();
        private static BlockingCollection<string> inBoundMessageQueue = new BlockingCollection<string>();

        public static void Start()
        {

            CancellationTokenSource cancellation = new CancellationTokenSource();

            // local endpoint
            var endpoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            var options = new WebSocketListenerOptions()
            {
                SubProtocols = new[] { "text", "com.microsoft.minecraft.wsencrypt" },
                PingTimeout = TimeSpan.FromSeconds(15),
                NegotiationTimeout = TimeSpan.FromSeconds(5),
                ParallelNegotiations = 16,
                NegotiationQueueCapacity = 256,
                TcpBacklog = 1000,
                BufferManager = BufferManager.CreateBufferManager((8192 + 1024) * 1000, 8192 + 1024)
            };

            // starting the server
            WebSocketListener server = new WebSocketListener(endpoint, options);
            var rfc6455 = new vtortola.WebSockets.Rfc6455.WebSocketFactoryRfc6455(server);
            // adding the deflate extension
            rfc6455.MessageExtensions.RegisterExtension(new WebSocketDeflateExtension());
            server.Standards.RegisterStandard(rfc6455);

          
            server.Start();

            Log("Socket server started at " + endpoint.ToString());

            var acceptingTask = Task.Run(() => AcceptWebSocketClients(server, cancellation.Token));

            string outBound;
            while (mStop == false)
            {
                while (outBoundMessageQueue.Count > 0)
                {
                    outBound = outBoundMessageQueue.Take();
                    if (outBound.Length < 0)
                    {
                        continue;
                    }

                    foreach (WebSocket socket in sockets.ToArray())
                    {
                        if ((socket != null) && socket.IsConnected)
                        {
                            socket.WriteString(outBound);
                        }
                    }
                }
            }

            Log("Server stopping");

            server.Stop();
            cancellation.Cancel();
            acceptingTask.Wait();
        }


        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log(string.Format("Unhandled Exception: ", e.ExceptionObject as Exception));
        }

        static async Task AcceptWebSocketClients(WebSocketListener server, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var ws = await server.AcceptWebSocketAsync(token).ConfigureAwait(false);
                    if (ws == null)
                        continue;

                    sockets.Add(ws);

                    Log("Accepted connection: " + ws.RemoteEndpoint);

                    await Task.Run(() => HandleConnectionAsync(ws, token));

                    _ = Task.Run(() => ReadInputsync(ws, token));


                }
                catch (Exception aex)
                {
                    var ex = aex.GetBaseException();
                    Log("Error Accepting client: " + ex.GetType().Name + ": " + ex.Message);
                }
            }
            Log("Server Stop accepting clients");
        }


        static async Task ReadInputsync(WebSocket ws, CancellationToken cancellation)
        {
            try
            {
                while (ws.IsConnected && !cancellation.IsCancellationRequested)
                {
                    String msg = await ws.ReadStringAsync(cancellation).ConfigureAwait(false);
                    if (msg == null)
                        continue;

                    inBoundMessageQueue.Add(msg);
                }
            }
            catch (TaskCanceledException)
            {

            }
            catch (Exception aex)
            {
                Log("Error Handling connection: " + aex.GetBaseException().Message);

            }
            finally
            {

            }
        }

        static async Task HandleConnectionAsync(WebSocket ws, CancellationToken cancellation)
        {
            try
            {
                while (ws.IsConnected && !cancellation.IsCancellationRequested)
                {
                    String msg = await ws.ReadStringAsync(cancellation).ConfigureAwait(false);
                    if (msg == null)
                        continue;

                    Log("Message received: " + msg);
                }

                Log($"Socket disconnected: (Latency: {ws.Latency.TotalSeconds} seconds)");
            }
            catch (TaskCanceledException)
            {

            }
            catch (Exception aex)
            {
                Log("Error Handling connection: " + aex.GetBaseException().Message);
                try { ws.Close(); }
                catch { }
            }
            finally
            {
                ws.Dispose();
            }
        }



        static void Log(String line)
        {
            Console.Write("\n{0}\n> ", DateTime.Now.ToString("dd/MM/yyy hh:mm:ss.fff ") + line);
        }
 

        public class CommandOrigin
        {
            public string type { get; set; }
        }

        public class CommandHeader
        {
            public string requestId { get; set; }
            public string messagePurpose { get; set; }
            public int version { get; set; }
 
        }

        public class CommandBody
        {
            public int version { get; set; }
            public string commandLine { get; set; }
            public CommandOrigin origin { get; set; }

            public CommandBody()
            {
                origin = new CommandOrigin();
            }

        }

        public class CommandPacket
        {
            public CommandHeader header { get; set; }
            public CommandBody body { get; set; }

            public CommandPacket()
            {
                header = new CommandHeader();
                header.requestId = Guid.NewGuid().ToString();
                header.version = 1;
                header.messagePurpose = "commandRequest";

                body = new CommandBody();
                body.version = 1;
                body.origin.type = "player";
            }

            public void setCommand(string command)
            {
                body.commandLine = command;
            }
        }
     
        static public void AddCommand(string command)
        {
                CommandPacket packet = new CommandPacket();
                packet.setCommand(command);

                string jsonString;
                jsonString = JsonSerializer.Serialize(packet);

                outBoundMessageQueue.Add(jsonString);
        }



        public class InputBody
        {
            public int version { get; set; }
            public string device { get; set; }
            public int name { get; set; }
            public float value { get; set; }
            public string mode { get; set; }
            public float x { get; set; }
            public float y { get; set; }


        }

        public class InputPacket
        {
            public CommandHeader header { get; set; }
            public InputBody body { get; set; }

            public InputPacket()
            {
                header = new CommandHeader();
                header.requestId = Guid.NewGuid().ToString();
                header.version = 1;
                header.messagePurpose = "controller";

                body = new InputBody();
                body.version = 1;
              

            }

            public void setValues(string _device, int _name, float _value)
            {
                body.device = _device;
                body.name = _name;
                body.value = _value;
                body.x = 0.0f;
                body.y = 0.0f;
                body.mode = "";
            }


            public void setValues(string _device, int _name, float _value, string mode, double x, double y)
            {
                body.device = _device;
                body.name = _name;
                body.value = _value;
                body.x = (float)x;
                body.y = (float)y;
                body.mode = mode;
            }

        }

        static public void AddInput(string device, int name, float value, string mode = "", double x = 0.0f, double y = 0.0f)
        {
            InputPacket packet = new InputPacket();

            packet.setValues(device, name, value, mode, x, y);

            string jsonString;
            jsonString = JsonSerializer.Serialize(packet);

            outBoundMessageQueue.Add(jsonString);
        }

    }
}
