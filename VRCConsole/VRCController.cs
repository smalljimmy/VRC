using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using vrc.Properties;
using System.Collections.Specialized;
using System.Configuration;

namespace vrc
{
    /// <summary>
    /// Startpoint of VRC service.
    /// </summary>
    public class VRCController : _Thread
    {


        /// <summary>
        /// The entry point for starting as console application.
        /// </summary>

        static void Main(string[] args)
        {
            VRCController vrController = new VRCController();
            vrController.run();
        }


        protected override void ThreadProc()
        {
            OnStart();
        }

       
        protected void OnStart()
        {


            try
            {

                // initialize configuration 
                if (!Configuration.loadConfig(null))
                {
                    throw new Exception("Load configuration failed");
                }


                //expose the remoting objects...
                startRemoting();

                // start Pool optimizer
                PoolOptimizer poolOptimizer = PoolOptimizer.Instance;
                poolOptimizer.run();

                // start Connection guard
                ConnectionGuard connectionGauard = ConnectionGuard.Instance;
                connectionGauard.run();

                //start VR-Controller on the given port
                StartListening(Convert.ToInt32(Configuration.vrcpPort));

            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to start VRC. Connection closed. Details: " + e);
                LogWriter.error("Unable to start VRC. Connection closed. Details: " + e);
                //Console.ReadLine();

            }

        }



        /// <summary>
        /// Hind the stop method of base class
        /// Stop thread and remoting
        /// </summary>
        public new void stop() {


            PoolOptimizer poolOptimizer = PoolOptimizer.Instance;
            poolOptimizer.stop();

            // start Connection guard
            ConnectionGuard connectionGauard = ConnectionGuard.Instance;
            connectionGauard.stop();

            // stop remoting
            this.stopRemoting();

            // stop main thread
            base.stop();

        }


        /// <summary>
        /// stop remoting and release the tcp port
        /// </summary>
        private void stopRemoting()
        {
            // get registered channels
            IChannel[] channels = ChannelServices.RegisteredChannels;

            // close our channel (named "RemotingService")
            foreach (IChannel eachChannel in channels)
            {
                if (eachChannel.ChannelName == "RemotingService")
                {
                    TcpChannel tcpmonremotingChannel = (TcpChannel)eachChannel;

                    // stop listening
                    tcpmonremotingChannel.StopListening(null);

                    // unregister channel
                    ChannelServices.UnregisterChannel(tcpmonremotingChannel);
                }
            }
        }

        /// <summary>
        /// Create a TCP channel to support remoting communication
        /// </summary>
        private static void startRemoting()
        {
            try
            {
                var tcpmonremotingChannel = new TcpChannel(int.Parse(new Settings().RemotingTcpPort));
                ChannelServices.RegisterChannel(tcpmonremotingChannel, false);
                RemotingConfiguration.RegisterWellKnownServiceType(new WellKnownServiceTypeEntry(typeof(RemotingService
                    ), "RemotingService", WellKnownObjectMode
                    .SingleCall));

            }
            catch (Exception e)
            {
                MailSender.Instance.send("VRC ERROR: Fail to start remoting. The Monitoring and remote configuration won't work for this run. Details: " + e);
                LogWriter.error("VRController.startRemoting: Fail to start remoting. The Monitoring and remote configuration won't work for this run. Details: " + e);
            }

        }



        #region members

        // svr
        public static ManualResetEvent allDone = new ManualResetEvent(false); // Thread signal.

        private static ManualResetEvent receiveDone = new ManualResetEvent(false); // signal end of asychronised receive
        private static ManualResetEvent sendDone = new ManualResetEvent(false); // signal end of asychronised send

        #endregion

        #region constructor
        public VRCController()
        {
        }
        #endregion

        #region methods

        public  void StartListening(int port)
        {

            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);

            // log start 
            LogWriter.debug("VRController is started ");

            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {

                listener.Bind(localEndPoint);
                listener.Listen(100);

                Boolean loop = true;

                // Declare the events to wait on
                WaitHandle[] waitOn = new WaitHandle[2];
                waitOn[0] = this._stopper;
                waitOn[1] = allDone;


                while (loop)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    LogWriter.info("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);


                    int index = WaitHandle.WaitAny(waitOn);
                    if (index == 0)
                    {
                        // thread is stopped. Stop listening
                        loop = false;
                    }
                    else
                    {
                        // a connection is made
                        // go on, do nothing 
                    }
                }

            }
            catch (Exception e)
            {
                LogWriter.error(e.ToString());

            }
            finally
            {
                try
                {
                    listener.Close();
                }
                catch
                {
                    listener = null;
                }
            }

            Console.WriteLine("\nPress ENTER to continue...");

            Console.Read();

        }


        public static void AcceptCallback(IAsyncResult ar)
        {
            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = null;

            // add exception handler 
            // because listener can be disposed beforehand when the VRController
            // thread is stopped
            try
            {
                handler = listener.EndAccept(ar);
            }
            catch
            {
                return;
            }

            String remoteClientIP = handler.RemoteEndPoint.ToString();
            Console.WriteLine("Connected with " + remoteClientIP);
            // dw
            LogWriter.debug("Connected with " + remoteClientIP);

            // Signal the main thread to continue.
            allDone.Set();

            // create a new session for the connection
            Session curSession = new Session(handler);

            try
            {
                HandleRequest(handler, curSession);
            }
            catch (Exception e)
            {
                Console.Write(e);
                handler.Close();
            }

        }


        private static void HandleRequest(Socket client, Session session)
        {
            try
            {
                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = client;
                state.session = session;

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);

            }
            catch (Exception e)
            {

                String remoteClientIP = client.RemoteEndPoint.ToString();
                LogWriter.debug("Disconnected with " + remoteClientIP + " Details: " + e);

                try
                {

                    client.Close();
                }
                catch { }
                finally { client = null; }


            }
        }


        public static void ReadCallback(IAsyncResult ar)
        {

                // Retrieve the state object and the handler socket
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;
                Session session = state.session;
                
            
            try
                {
                // Read data from the client socket. 
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There  might be more data, so store the data received so far.
                    String content = Encoding.ASCII.GetString(
                        state.buffer, 0, bytesRead);


                    // boolean flag to check whether the connection with client will be closed 
                    // Boolean fCloseConnection;

                    // let the session objekt process the request and deliver the reply
                    String reply = session.delegateCmd(content); //, out fCloseConnection);

                    // dw
                    LogWriter.debug("content: " + handler.RemoteEndPoint.ToString() + ": " + content);
                    LogWriter.debug("reply: " + reply);
                    Send(handler, reply);

                    // close connection after processing the command (logout, login fail)
                    //if (fCloseConnection)
                    //{
                    //    // dispose session (count down client connections)
                    //    session.Dispose();
                    //    return;
                    //}


                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);

                }
                else
                {
                    // bytesRead == 0. Client ends the connection by closing socket
                    LogWriter.debug("VRController.ReadCallback:  The socket was closed by remote client");
                    session.Dispose();
                    return;
                }

            }
            catch
            {
                // client disconnected without closing socket (e.g. close Program)
                LogWriter.error("VRController.ReadCallback: client is disconnected");
                session.Dispose();
            }

        }

        private static void Send(Socket handler, String data)
        {

            // make sure the data is ended with new line character '\n'(Hex(10))
            if (!data.EndsWith("\n"))
            {
                data = data + "\n";
            }

            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);

        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);

            }
            catch 
            {
                //Console.WriteLine(e.ToString());
            }
        }

        #endregion


    }

    // State object for reading client data asynchronously
    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];

        // Session
        public Session session = null;
    }

}
