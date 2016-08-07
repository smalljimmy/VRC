using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;

namespace vrc
{
	class SocketStateObject
	{
		private Socket _workSocket;
		public byte[] buffer;
		public StringBuilder sb = new StringBuilder();
		public bool complete;
		public const int BUFFER_SIZE = 1024;

		public SocketStateObject(Socket _socket)
		{
			buffer = new byte[BUFFER_SIZE];
			complete = false;
			_workSocket = _socket;
		}

		public Socket WorkSocket
		{
			get { return _workSocket; }
		}
	}

	public class AsynchSocketManager
	{

        public string hostname; 
        public int port;
  
        
        
        private Queue TransmitQueue = new Queue();
		private Queue ReceiveQueue = new Queue();
		private ReaderWriterLock TransmitLock = new ReaderWriterLock();
		private ReaderWriterLock ReceiveLock = new ReaderWriterLock();
		private Socket _Socket;
		
		private ManualResetEvent StopEvent = new ManualResetEvent(false);
		private AutoResetEvent DataReady = new AutoResetEvent(false);

		private int MyId;
		static private int Id;
		private long StartTime;

        public delegate void OnSendDelegate(AsynchSocketManager socketMgr, string msg);
        public delegate void OnReceiveDelegate(AsynchSocketManager socketMgr, string msg);
        public delegate void OnConnectDelegate(AsynchSocketManager socketMgr, bool bSuccess);
        public delegate void OnDisconnectDelegate(AsynchSocketManager socketMgr);

        public OnSendDelegate OnSend;
		public OnReceiveDelegate OnReceive;
		public OnConnectDelegate OnConnect;
		public OnDisconnectDelegate OnDisconnect;


        public AsynchSocketManager()
        {
        }

        public AsynchSocketManager(string _hostName, int _port)
        {
            hostname = _hostName;
            port = _port;

            Id++; // increment our "global" id - could use 'real' quid's here...
            MyId = Id; // save the value locally
            StartTime = DateTime.Now.Ticks;
        }


		~ AsynchSocketManager()
		{
			Disconnect();
		}

		public int SessionID
		{
			get { return MyId; }
		}


        /// <summary>
        /// let client change the time out value of socket
        /// </summary>
        /// <param name="timeOut">
        /// millisenconds for timeout
        /// 0: never time out
        /// </param>
        public void setTimeOut(int timeOut)
        {
            _Socket.ReceiveTimeout = timeOut;
            _Socket.SendTimeout = timeOut;
        }


		public int Connect()
		{
			if (this.IsConnected)
				return -1; //'no need to do anything once connected

			// resolve...
			if ( hostname != null && hostname.Length > 0 )
			{
                IPAddress ipAddress;

                try
                {
                    // try IP address first
                    ipAddress = IPAddress.Parse(hostname);
                }
                catch (FormatException)
                {

                    // not IP, use DNS 
                    try
                    {
                        IPHostEntry serverHostEntry = Dns.GetHostEntry(hostname);
                        ipAddress = serverHostEntry.AddressList[0];
                    }
                    catch (SocketException)
                    {
                        // can't resolve the host by DNS either
                        LogWriter.error("ServerEntry: Can't parse the server :" + hostname);
                        return -1;
                    }
                }




                IPEndPoint endPoint = new IPEndPoint(ipAddress, port);
                //IPEndPoint endPoint = new IPEndPoint(Dns.GetHostAddresses(hostname)[0], port);


				_Socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );

				try
				{
					_Socket.Connect( endPoint );
				}
				catch (SocketException)
				{
					OnConnect(this, false);
					return -1;
				}


                OnConnect(this, true);

                StopEvent.Reset();

				ThreadPool.QueueUserWorkItem(new WaitCallback(ReceiveThreadEntryPoint));
				ThreadPool.QueueUserWorkItem(new WaitCallback(SendThreadEntryPoint));

				// return this unique id
				return MyId;
			}
			else
			{
				return -1;
			}
		}

		public bool IsConnected
		{
			get { return (_Socket == null ? false : _Socket.Connected); }
		}

		public void Disconnect()
		{
			if (IsConnected)
			{


                if (OnDisconnect != null)
                {
                    OnDisconnect(this);
                }

                
                // nullify all event handlers
                OnConnect = null;
                OnSend = null;
                OnReceive = null;
                OnDisconnect = null;


				// signal the threads to end
				StopEvent.Set();

				// now kill the socket
				if (_Socket != null)
				{
					_Socket.Close();
				}

			}
		}

		public void Send(string message)
		{
			// queue it...
			TransmitLock.AcquireWriterLock(-1);
			try
			{
				TransmitQueue.Enqueue(message);
			}
            catch (Exception e)
            {
                LogWriter.info(e.ToString());
            }
			finally { TransmitLock.ReleaseWriterLock(); }

			// signal that data was sent
			DataReady.Set();
		}

        public void clearMsgQueue()
        {
            // queue it...
            TransmitLock.AcquireWriterLock(-1);
            try
            {
                TransmitQueue.Clear();
            }
            catch (Exception e)
            {
                LogWriter.info(e.ToString());
            }
            finally { TransmitLock.ReleaseWriterLock(); }
        }

		public void ReceiveThreadEntryPoint(object state)
		{
			try
			{
				// loop...
				while( true )
				{
					WaitHandle[] handles = new WaitHandle[1];
					handles[0] = StopEvent;

                    if (WaitHandle.WaitAny(handles, 10,false) == 0)
                    {
                        break;
                    }else if ( _Socket != null && _Socket.Connected )
					{
						// not disconnected
						try
						{
							// start the recieve operation
							System.IAsyncResult iar;
							SocketStateObject so = new SocketStateObject(_Socket);
							_Socket.BeginReceive(so.buffer, 0, SocketStateObject.BUFFER_SIZE, 0, new AsyncCallback(AsynchReadCallback), so);

						}
                        catch (Exception e)
                        {
                            LogWriter.debug("AsynchSocketMgr.ReceiveThreadEntryPoint: Exception happens. Details: " + e);
                        }
					}

				}
			}
			catch {}

            LogWriter.debug("AsynchSocketMgr.ReceiveThreadEntryPoint:Receive Thread is ended");
		}

		public void SendThreadEntryPoint(object state)
		{
			try
			{
				Queue workQueue = new Queue();

				// loop...
				while( true )
				{
					WaitHandle[] handles = new WaitHandle[2];
					handles[0] = StopEvent;
					handles[1] = DataReady;

					if( WaitHandle.WaitAny(handles, 10, false) == 0 )
					{
						break;
					}
					else if (_Socket != null && _Socket.Connected)
					{
						// not disconnected
						// go through the queue...
						TransmitLock.AcquireWriterLock(-1);
						try
						{
							workQueue.Clear();
							foreach( string message in TransmitQueue)
							{
								workQueue.Enqueue(message);
							}
							TransmitQueue.Clear();
						}
						catch {}
						finally 
						{
							TransmitLock.ReleaseWriterLock();
						}

						// loop the outbound messages...
						foreach( string message in workQueue )
						{
							SocketStateObject so2 = new SocketStateObject(_Socket);
                            so2.buffer = Encoding.ASCII.GetBytes(message);
          
							// send it...
                            if (OnSend != null)
                            {
                                OnSend(this, message);
                            }
							_Socket.Send(so2.buffer, so2.buffer.Length,SocketFlags.None);

						}

					}
				}
			}
            catch 
            {
            }

            LogWriter.debug("AsynchSocketMgr.SendThreadEntryPoint:Send Thread is ended");
		}

        //public void AsynchSendCallback(System.IAsyncResult ar)
        //{
        //    SocketStateObject so = (SocketStateObject)ar.AsyncState;
        //    Socket s = so.WorkSocket;

        //    try
        //    {

        //        // sanity check
        //        if (s == null || !s.Connected) return;
        //        int send = s.EndSend(ar);

        //    }
        //    catch (Exception e){
        //        LogWriter.debug("AsynchSendCallback: " + e.Message);
        //        ConnectionGuard.Instance.startPingpong();
        //    }
        //}


		private void AsynchReadCallback(System.IAsyncResult ar)
		{
			SocketStateObject so = (SocketStateObject)ar.AsyncState;
			Socket s = so.WorkSocket;

            try
            {
                // sanity check
                if (s == null || !s.Connected) return;
                int read = s.EndReceive(ar);
                if (read > 0)
                {
                    string msg = Encoding.ASCII.GetString(so.buffer, 0, read);

                    if (msg.IndexOf("\n") < 0 || msg.LastIndexOf("\n") == msg.IndexOf("\n"))
                    {
                        if (!msg.Contains(" db on "))
                            LogWriter.error("AsynchSocketMgr.AsynchReadCallback: The received message is incomplete." + msg);
                    }

                    if (OnReceive != null)
                    {
                        OnReceive(this, msg);
                    }

                    // sanity check
                    if (s == null || !s.Connected) return;
                    // and start recieving more
                    //s.BeginReceive(so.buffer, 0, SocketStateObject.BUFFER_SIZE, 0, new AsyncCallback(AsynchReadCallback), so);
                }
            }
            catch (Exception e)
            {
                LogWriter.debug("AsynchReadCallBack:" + e.Message);
                ConnectionGuard.Instance.startPingpong();
            }
		}
	}
}
