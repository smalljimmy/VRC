using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections;
using System.Net;
using vrc.Properties;
using System.IO;
using System.Text;
using CodeBureau;

namespace vrc
{

    /// <summary>
    /// Run as Thread to check connection availability with LDC-Server in Ping-Pong way
    /// Let LDCClient(s) (un)register themselves as observer(s) so that they can get notified
    /// when the connection is lost/reestablished
    /// </summary>
    public class ConnectionGuard : _Thread, IConfigurationListener
    {
        private int ldcServerPort = new Settings().ldcServerPort;

        delegate void Del(ServerEntry se);


        #region embedded classes

        public class _PingThread : _Thread
        {
            volatile bool isRunning = false;
            Communication comm;

            AutoResetEvent reachLDCEvent = new AutoResetEvent(false); // event,set when receiving pong
            AutoResetEvent _restartPing = new AutoResetEvent(false); // Ping will start when the event is set


            public void restart()
            {
                _restartPing.Set();
            }
            
            protected void handlePong(AsynchSocketManager socketMgr, string msg)
            {
                if (!isRunning)
                {
                    return;
                }

                string rep_head;
                string rep_content;
                HelperTools.logRespMsg(msg, out rep_head, out rep_content, comm);


                if (!rep_head.Contains(StringEnum.GetStringValue(LDCCmd.PONG)))
                {
                    LogWriter.error("ConnectionGuard._PingThread: Receive unexpected message (not Pong) : " + rep_head + '\n' + rep_content + '\n');
                }

                reachLDCEvent.Set();

            }

            // ping thread
            protected override void ThreadProc()
            {



                // sleep for a random time between 10- 20 seconds before first run 
                Thread.Sleep(new Random().Next(10, 20) * 1000);

                Boolean loop = true;

                // ping the LDC-Servers with given interval
                while ( loop)
                {
                    try
                    {
                        Monitor.Enter(sync);

                        if (m_ldcserverList.Count > offlineLdcserverList.Count) // only start when there're online servers
                        {

                            LogWriter.debug("ConnectionGuard._PingThread: Ping begins to run");
                            isRunning = true;

                            // ping the ldc server in the list one by one
                            foreach (LDCServerComm ldcServerComm in m_ldcserverList)
                            {
                                ServerEntry ldcServer = new ServerEntry(ldcServerComm.ldcServer, ldcServerComm.port);

                                // don't ping offline servers
                                if (offlineLdcserverList.Contains(ldcServerComm))
                                {
                                    continue;
                                }

                                comm = ldcServerComm.pingpongComm;
                                comm.OnReceive = new AsynchSocketManager.OnReceiveDelegate(handlePong);
                                comm.clearMsgQueue();
                                comm.sendMessage(LDCCmd.PING, "");

                                int index1 = WaitHandle.WaitAny(new WaitHandle[] { reachLDCEvent }, Configuration.ldcTimout, false);
                                if (index1 == WaitHandle.WaitTimeout)
                                {

                                    // ldcServer is unreachable
                                    LogWriter.error("ConnectionGuard._PingThread: LDC-Server " + ldcServerComm.ldcServer + " is unreachable");

                                    // put the server into Black-List
                                    offlineLdcserverList.Add(ldcServerComm);



                                    // notify the registered oberservers on this server
                                    List<LDCClient> oberserverList = getLDCClientListOfServer(ldcServer);
                                    foreach (LDCClient ldcc in oberserverList)
                                    {
                                        LogWriter.info("ConnectionGuard._PingThread: {0} is disconnected ", ldcServer);
                                        // use delegation to call the method asynchronously
                                        new Del(ldcc.lostConnectionEvent).Invoke(ldcServer);
                                    }

                                    // disconnect all the communication objects
                                    comm.Disconnect();
                                    MonCommunication monComm = ldcServerComm.monitoringComm;
                                    monComm.Disconnect();
                                    CmdCommunication cmdComm = ldcServerComm.cmdComm;
                                    cmdComm.Disconnect();
                                }
                                else
                                {
                                    // ldcServer is running;  everything's fine
                                    // ping the next ldc server
                                }



                            }

                            //wake up reconnection
                            isRunning = false;
                            LogWriter.debug("ConnectionGuard._PingThread: Ping ends");
                        } // end if

                        Monitor.Exit(sync);

                         // run next round at a predefined interval or the _restartPing is signaled. Break when _stopper is set
                        int index2 = WaitHandle.WaitAny(new WaitHandle[] { _stopper, _restartPing }, Configuration.pingInterval, false);
                        if (index2 == 0)
                        {
                            //stopper is set
                            loop = false;
                        }
                        else if (index2 == 1 || index2 == WaitHandle.WaitTimeout)
                        {
                            // run next round
                        }

                    } // end try
                    catch (Exception e)
                    {
                        // trigger eskalation
                        MailSender.Instance.send("VRC Error: The ConnectionGuard meets an error. Details: " + e);
                        LogWriter.error("ConnectionGuard._PingThread: The ConnectionGuard meets an error. Details: " + e);

       

                    }
                    finally
                    {
                        try
                        {
                            // Release the lock.
                            Monitor.Exit(sync);
                        }
                        catch { }


                    }

                } // end while


                LogWriter.debug("ConnectionGuard._PingThread: the _PingThread is stopped");
            }
        }

        /// <summary>
        /// Helper class - Thread for reconnecting the offline servers 
        /// </summary>
        public class _ReconnectThread : _Thread
        {

            AutoResetEvent _restartReconnect = new AutoResetEvent(false);

            public void restart(){
                _restartReconnect.Set();
            }

            protected override void ThreadProc()
            {
                Boolean loop = true;
                // Try to reconnect all the servers which are currently offline
                while (loop)
                {
                    try
                    {
                        Monitor.Enter(sync);

                        if (offlineLdcserverList.Count > 0)
                        {// only start when there're offline servers

                            // Try to acquire the lock.

                            LogWriter.debug("ConnectionGuard._ReconnectThread: Reconnection begins to run");

                            // save the ldcservers which go online again (temporarily)
                            List<LDCServerComm> goOnlineLDCserverList = new List<LDCServerComm>();

                            // go through all offline servers , try to make reconnection
                            foreach (LDCServerComm ldcServerComm in offlineLdcserverList)
                            {
                                if( ldcServerComm.pingpongComm == null)  ldcServerComm.pingpongComm = new Communication(ldcServerComm.ldcServer, ldcServerComm.port);
                                if (ldcServerComm.monitoringComm == null) ldcServerComm.monitoringComm = new MonCommunication(ldcServerComm.ldcServer, ldcServerComm.port);
                                if (ldcServerComm.cmdComm == null) ldcServerComm.cmdComm = new CmdCommunication(ldcServerComm.ldcServer, ldcServerComm.port);

                                Communication comm = ldcServerComm.pingpongComm;
                                ServerEntry ldcServer = new ServerEntry(ldcServerComm.ldcServer, ldcServerComm.port);

                                if (!comm.initCommunication())
                                {
                                    LogWriter.error("ConnectionGuard._ReconnectThread: can't establish connection with the LDC-Server: " + new ServerEntry(ldcServerComm.ldcServer, ldcServerComm.port));

                                    // check whether LDC-Server has been offline( >= maximal downtime)
                                    if (comm.isDown(Configuration.downTime))
                                    {
                                        // trigger Escalation
                                        MailSender.Instance.send("VRC ERROR: can't establish connection with the LDC-Server: " + new ServerEntry(ldcServerComm.ldcServer, ldcServerComm.port));
                                    }
                                    
                                    continue;
                                }
                                else
                                {

                                    // initialize the communications for other two processes
                                    // pass the userId which is already available
                                    ldcServerComm.cmdComm.userID = ldcServerComm.pingpongComm.userID;
                                    ldcServerComm.monitoringComm.userID = ldcServerComm.pingpongComm.userID;


                                    if (ldcServerComm.cmdComm.initCommunication() &&
                                         ldcServerComm.monitoringComm.initCommunication())
                                    {

                                        // mark the ldcServer as online
                                        goOnlineLDCserverList.Add(ldcServerComm);


                                        // notify the registered oberservers on this server
                                        List<LDCClient> oberserverList = getLDCClientListOfServer(ldcServer);
                                        foreach (LDCClient ldcc in oberserverList)
                                        {
                                            LogWriter.info("ConnectionGuard._Reconnect: {0} is online again", ldcServer);
                                            // use delegater to call the method asychrously
                                            new Del(ldcc.reestablishConnection).BeginInvoke(ldcServer,null,null);

                                        }

                                    }
                                    else
                                    {
                                        LogWriter.error("ConnectionGuard._ReconnectThread: can't establish connection with the LDC-Server: " + new ServerEntry(ldcServerComm.ldcServer, ldcServerComm.port));
                                        continue;
                                    }
                                }
                            } // end foreach

                            // move all the reconnectable servers out of "Black-List"
                            if (goOnlineLDCserverList.Count > 0)
                            {
                                offlineLdcserverList.RemoveAll(delegate(LDCServerComm ldcServComm)
                                {
                                    return goOnlineLDCserverList.Contains(ldcServComm);
                                });
                            }


                            LogWriter.debug("ConnectionGuard._ReconnectThread: Reconnection ends");

                        } // end if

                        Monitor.Exit(sync);


                        // run next round at a predefined interval or the _restartReconnect is signaled. Break when _stopper is set
                        int index = WaitHandle.WaitAny(new WaitHandle[] { _stopper, _restartReconnect }, Configuration.loginInterval, false);
                        if (index == 0)
                        {
                            //stopper is set
                            loop = false;
                        }
                        else if (index == 1 || index == WaitHandle.WaitTimeout)
                        {
                            // run next round
                        }

                    }
                    catch (Exception e)
                    {
                        // notify _PingThread that error happens by setting global variable member 
                        MailSender.Instance.send("VRC Error: The ConnectionGuard meets an error. Details: " + e);
                        LogWriter.error("ConnectionGuard._ReconnectThread: The ConnectionGuard meets an error. Details: " + e);
                    }
                    finally
                    {
                        try
                        {
                            // Release the lock.
                            Monitor.Exit(sync);
                        }
                        catch { }

                    }

                } // end while


                LogWriter.info("ConnectionGuard._ReconnectThread: the _ReconnectThread is stopped");
            }

            override public void stop()
            {
                // close all sockets
                foreach (LDCServerComm ldcServerComm in ConnectionGuard.Instance.getOnlineLDCServerCommList())
                {
                    MonCommunication monComm = ldcServerComm.monitoringComm;
                    monComm.Disconnect();

                    CmdCommunication cmdComm = ldcServerComm.cmdComm;
                    cmdComm.Disconnect();

                    Communication pingpongComm = ldcServerComm.pingpongComm;
                    pingpongComm.Disconnect();
                }

                base.stop();

            }
        }

        #endregion

        #region members

        // Ping thread
        _PingThread pingThread = null;

        // Reconnect thread
        _ReconnectThread reconnectThread = null;


        // List to hold the ldc servers together with their communication objects 
        private static List<LDCServerComm> m_ldcserverList =
            new List<LDCServerComm>();

        // List to hold the offline ldc servers currently  
        private static List<LDCServerComm> offlineLdcserverList =
            new List<LDCServerComm>();

        // Map from a specific ldcServer to its observers ( LDCClients )
        private static Dictionary<ServerEntry, List<LDCClient>> dicServer2Clients = new Dictionary<ServerEntry, List<LDCClient>>();

        // Return the singleton instance of ConnectionGuard
        private static volatile ConnectionGuard instance = null;

        // object for synchronisation
        private static Object sync = new Object();


        #endregion

        #region constructor

        /// <summary>
        /// Private constructor to prevent instantiation
        /// </summary>
        private ConnectionGuard()
        {
            // load LDC server list from configuration 
            loadServerList();

            // register as observer of Configuration in order to be informed
            // when LDC-Server list is updated
            Configuration.registerObserver(this);
        }


        public void startPingpong()
        {
            pingThread.restart();
        }

        public void startReconnect()
        {
            reconnectThread.restart();
        }


        /// <summary>
        /// Load the ldc-servers from Configuration and
        /// initialize the m_ldcServerList by generating
        ///  "ldcServer,communication" pairs
        /// </summary>
        private void loadServerList()
        {
            // clear containers 
            m_ldcserverList.Clear();
            offlineLdcserverList.Clear();
            dicServer2Clients.Clear();


            char[] sep = { ';' };
            String[] ldcServers = Configuration.ldcServerList.Split(sep);

            for (int i = 0; i < ldcServers.Length; i++)
            {
                string serverPort = ldcServers[i].Trim();
                if (serverPort.Length == 0)
                {
                    continue;
                }

                LDCServerComm ldcServerComm = new LDCServerComm();
                ServerEntry ldcServerEntry = new ServerEntry(serverPort);

                // create three communication objects(not initialized) for each ldcServer

                ldcServerComm.ldcServer = ldcServerEntry.servername;
                ldcServerComm.port = ldcServerEntry.port;

                m_ldcserverList.Add(ldcServerComm);

                // make sure to mark all servers as offline first
                // the reason is that we need to initialize the communictation object 
                // for each LDC-Server within _ReconnectThread
                offlineLdcserverList.Add(ldcServerComm);

                // also initialize the dicServer2Clients with no observers at moment
                dicServer2Clients.Add(ldcServerEntry, new List<LDCClient>());

            }
        }


        /// <summary>
        /// Static method to retrieve instance of ConnectionGuard
        /// </summary>
        /// <param name="pCommunication">Communication object</param>
        /// <param name="pTransaction">Transaction object</param>
        /// <returns></returns>
        public static ConnectionGuard Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (typeof(ConnectionGuard))
                    {
                        if (instance == null)
                        {
                            instance = new ConnectionGuard();
                        }
                    }

                }

                return instance;
            }

        }

        #endregion

        #region methods


        protected override void ThreadProc()
        {
            // creat && start reconnection thread
            if (reconnectThread == null)
            {
                reconnectThread = new _ReconnectThread();
                reconnectThread.run();
            }

            // start ping thread
            if (pingThread == null)
            {
                pingThread = new _PingThread();
                pingThread.run();
            }

            LogWriter.info("ConnectionGuard is started");
        }


        /// <summary>
        /// Stop the ConnectionGuard
        /// hide the inherited stop method
        /// We need to stop the two sub-threads explicitly
        /// </summary>
        new public void stop()
        {
            try
            {
                // stop reconnect thread
                reconnectThread.stop();
                reconnectThread = null;
            }
            catch { }

            try
            {
                // stop ping thread
                pingThread.stop();
                pingThread = null;
            }
            catch { }

            try
            {
                // Release the lock.
                Monitor.Exit(sync);
            }
            catch { }

            // also stop myself
            base.stop();

        }


        /// <summary>
        /// Unregister a ldcClient from the observer for one given ldcServer
        /// </summary>
        public void unRegisterObserver(LDCClient ldcClient, ServerEntry ldcServer)
        {
            if (ldcClient == null || ldcServer == null)
            {
                throw new ArgumentNullException();
            }

            try
            {
                List<LDCClient> observerList = getLDCClientListOfServerForChange(ldcServer);

                // add the new ldcClient into the oberserver List (sychronised)
                lock (((IList)observerList).SyncRoot)
                {
                    if (observerList.Remove(ldcClient))
                    {
                        LogWriter.error("ConnectionGuard.unRegisterObserver: the ldcClient " + ldcClient.LdcClientIPAddress +
                       " is UNregistered on {0}", ldcServer);
                    }
                }
            }
            catch (Exception e)
            {

                LogWriter.error("ConnectionGuard.registerObserver: Unregistration failed. Details: " + e);
            }
        }


        /// <summary>
        /// Register a ldcClient as the observer for one ldcServer(online) and for all 
        /// the offline ldcServers
        /// </summary>
        public void registerObserver(LDCClient ldcClient, ServerEntry ldcServer)
        {
            if (ldcClient == null || ldcServer == null)
            {
                throw new ArgumentNullException();
            }

            try
            {
                    List<LDCClient> observerList = getLDCClientListOfServerForChange(ldcServer);

                    // add the new ldcClient into the oberserver List (sychronised)
                    lock (((IList)observerList).SyncRoot)
                    {
                        if (observerList.Contains(ldcClient))
                        {
                             LogWriter.error("ConnectionGuard.registerObserver: the ldcClient " + ldcClient.LdcClientIPAddress +
                            " is tried to be registered on {0} for twice", ldcServer);
                        }
                        else
                        {
                            observerList.Add(ldcClient);
                            LogWriter.debug("ConnectionGuard.registerObserver: the ldcClient " + ldcClient.LdcClientIPAddress +
                        " is registered on " + ldcServer);
                        }

                    }

                    // also register the client on offline servers
                    foreach (ServerEntry offlienLdcServer in getOfflineLDCServersList ())
                    {
                        List<LDCClient> observerList2 = getLDCClientListOfServerForChange(offlienLdcServer);
                        lock (((IList)observerList2).SyncRoot)
                        {
                            if (observerList2.Contains(ldcClient))
                            {
                                LogWriter.error("ConnectionGuard.registerObserver: can not register the ldcClient " + ldcClient.LdcClientIPAddress +
                               " on {0} twice", offlienLdcServer);
                            }
                            else
                            {
                                observerList2.Add(ldcClient);
                                LogWriter.debug("ConnectionGuard.registerObserver: the ldcClient " + ldcClient.LdcClientIPAddress +
                            " is registered on {0} (offline)" , offlienLdcServer);
                            }

                        }

                    }
         
            }
            catch (Exception e)
            {

                LogWriter.error("ConnectionGuard.registerObserver: registration failed. Details: " + e);
            }

        }

        private List<LDCClient> getLDCClientListOfServerForChange(ServerEntry ldcServer)
        {
            //List for holding the registered observers
            List<LDCClient> observerList;

            dicServer2Clients.TryGetValue(ldcServer, out observerList);

            return observerList;
        }


        /// <summary>
        /// Remove an ldcClient from all the oberserverList in which it has registered
        /// </summary>
        /// <param name="ldcClient">ldcClient</param>
        public void removeObserver(LDCClient ldcClient)
        {
            if (ldcClient == null)
            {
                LogWriter.error("ConnectionGuard.removeObserver: the ldcclient does not exist anymore");
            }

            try
            {
                foreach (ServerEntry ldcServerEntry in dicServer2Clients.Keys)
                {
                    List<LDCClient> observerList = getLDCClientListOfServerForChange(ldcServerEntry);


                    // remove the ldcClient from the oberserver List (sychronised)
                    lock (((IList)observerList).SyncRoot)
                    {
                        if (observerList.Remove(ldcClient))
                        {
                            LogWriter.debug("ConnectionGuard.removeObserver: the ldcClient  " + ldcClient.LdcClientIPAddress +
                        " is removed from observer list of " + ldcServerEntry.servername);

                        }

                    }
                };

            }
            catch (Exception e)
            {
                LogWriter.error("ConnectionGuard.removeObserver: remove observer failed. Details: " + e);
            }

        }



        /// <summary>
        /// Get the List of LDCClients connected to the specific LDCServer
        /// </summary>
        /// <param name="ldcServer">the LDCServer</param>
        /// <returns>List of ldcclients (as Reference)</returns>
        private static List<LDCClient> getLDCClientListOfServer(ServerEntry ldcServer)
        {

            //List for holding the registered observers
            List<LDCClient> observerList;

            dicServer2Clients.TryGetValue(ldcServer, out observerList);

            // use copy
            return new List<LDCClient>( observerList.ToArray() );
        }


        /// <summary>
        /// return all the online LDC Servers
        /// </summary>
        /// <returns></returns>
        public List<ServerEntry> getOnlineLDCServersList()
        {
            List<LDCServerComm> ldcServerCommList = this.getOnlineLDCServerCommList();
            List<ServerEntry> onlineLDCServersList = new List<ServerEntry>();

            foreach (LDCServerComm ldcServerComm in ldcServerCommList)
            {
                ServerEntry nextLDCServer = new ServerEntry(ldcServerComm.ldcServer, ldcServerComm.port);
                onlineLDCServersList.Add(nextLDCServer);
            }

            return onlineLDCServersList;
        }


        /// <summary>
        /// return all the online LDCServerComms
        /// </summary>
        /// <returns></returns>
        public List<LDCServerComm> getOnlineLDCServerCommList()
        {
            List<LDCServerComm> ldcServerCommList = new List<LDCServerComm>((LDCServerComm[])m_ldcserverList.ToArray().Clone());
            List<LDCServerComm> offlineLdcServerCommList = getOfflineLDCServerCommList();

            ldcServerCommList.RemoveAll(delegate(LDCServerComm comm) { return offlineLdcServerCommList.Contains(comm); });

            return ldcServerCommList;
        }



        /// <summary>
        /// return all the offline LDC Servers
        /// </summary>
        /// <returns></returns>
        public List<ServerEntry> getOfflineLDCServersList()
        {
            List<LDCServerComm> ldcServerCommList = this.getOfflineLDCServerCommList();
            List<ServerEntry> offlineLDCServersList = new List<ServerEntry>();

            foreach (LDCServerComm ldcServerComm in ldcServerCommList)
            {
                ServerEntry nextLDCServer = new ServerEntry(ldcServerComm.ldcServer, ldcServerComm.port);
                offlineLDCServersList.Add(nextLDCServer);
            }

            return offlineLDCServersList;
        }


        /// <summary>
        /// return all the offline LDCServerComms
        /// </summary>
        /// <returns></returns>
        public List<LDCServerComm> getOfflineLDCServerCommList()
        {
            List<LDCServerComm> offlineLdcServerCommList = new List<LDCServerComm>((LDCServerComm[])offlineLdcserverList.ToArray().Clone());
            return offlineLdcServerCommList;
        }


        #endregion


        #region IConfigurationListener Members

        /// <summary>
        /// notify all the listeners about the configuration changed event
        /// </summary>
        public void notifyConfigurationChanged()
        {
            LogWriter.debug("ConnectionGuard.notifyConfigurationChanged: Configuration changed. LDC-Server list will be reloaded");

            // stop the running thread first
            this.stop();

            // reload the new ldcServer list 
            this.loadServerList();

            LogWriter.debug("ConnectionGuard.notifyConfigurationChanged: LDC-Server list is updated. ConnectionGuard will be restarted");

            // restart the thread
            this.run();

        }

        #endregion

        internal void addToCmdQueue(LDCCmd req_category, string req_msg, ServerEntry ldcServer)
        {
            if (ldcServer == null)
            {
                LogWriter.error("ConnectionGuard.addToCmdQueue: the parameter ldcServer is null");
                return;
            }

            // get comm object
            LDCServerComm ldcServerComm = m_ldcserverList.Find(delegate(LDCServerComm comm)
            { return comm.ldcServer.Equals(ldcServer.servername) && comm.port == ldcServer.port; });

            if (ldcServerComm == null)
            {
                LogWriter.error("ConnectionGuard.addToCmdQueue: {0} is unavailable now ", ldcServer);
                return;
            }

            // send the cmd
            ldcServerComm.cmdComm.sendMessage(req_category, req_msg);


        }

    }
}
