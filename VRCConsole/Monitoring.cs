using System;
using System.Collections.Generic;
using System.Text;

namespace vrc
{
    /// <summary>
    /// The Monitoring class is for 
    /// Save the event and status of the application
    /// Calculate and provides status information
    /// </summary>
    static class Monitoring
    {
        public static volatile int ClientsConnected = 0; // total connections with clients
        private static volatile Dictionary<ServerEntry, int> LDCServerActiveCalls; // hold the ldcserver names together with the number of active calls
        private static Object sync = new Object(); // used for sychronisation


        static Monitoring()
        {
            LDCServerActiveCalls = new Dictionary<ServerEntry, int>();
        }

        /// <summary>
        /// return the online ldcservers (subset of LDCServerConnects) with their active calls
        /// </summary>
        /// <returns></returns>
        public static Dictionary<ServerEntry, int> GetLDCServersWithActiveCalls()
        {
            Dictionary<ServerEntry,int> onlineLDCServerConnects = new 
                Dictionary<ServerEntry,int>();

            foreach (ServerEntry se in ConnectionGuard.Instance.getOnlineLDCServersList())
            {
                int cons = 0;
                LDCServerActiveCalls.TryGetValue(se, out cons);
                onlineLDCServerConnects.Add(se, cons);
            }

            return onlineLDCServerConnects;
        }


        /// <summary>
        /// Invoked when a new call is started on the LDC Server
        /// </summary>
        /// <param name="ldcServer">the ldcServer name</param>
        public static void notifyCallConnected(ServerEntry ldcServer)
        {
            LogWriter.debug("Monitoring.notifyCallConnected: a new call is started on " + ldcServer);

            if (ldcServer == null)
            {
                throw new NullReferenceException("The ldcServer is null");
            }

            lock (sync)
            {
                if (LDCServerActiveCalls.Keys.Count == 0 || !LDCServerActiveCalls.ContainsKey(ldcServer))
                {
                    LDCServerActiveCalls.Add(ldcServer, 1);
                }
                else
                {
                    
                    LDCServerActiveCalls[ldcServer]++;
                }
                
                
                // added
                printContents(LDCServerActiveCalls);

            }

            //printContents(LDCServerConnects);
            
        }

        private static void printContents(Dictionary<ServerEntry, int> LDCServerConnects)
        {
            foreach (ServerEntry servername in LDCServerConnects.Keys){
                LogWriter.debug(servername + "(" + LDCServerConnects[servername] + ")");
            }
        }


        /// <summary>
        /// Invoked when a call is disconnected on the LDCServer
        /// </summary>
        /// <param name="ldcServer">the LDCServer name</param>
        public static void notifyCallDisconnected(ServerEntry ldcServer)
        {
            LogWriter.debug("Monitoring.notifyCallDisconnected:" + ldcServer);

            if (ldcServer == null)
            {
                throw new NullReferenceException("The ldcServer is null");
            }


            lock (sync)
            {
                if (LDCServerActiveCalls.Keys.Count == 0 || !LDCServerActiveCalls.ContainsKey(ldcServer))
                {
                    return;
                }

                if (LDCServerActiveCalls[ldcServer] > 0)
                {
                    LDCServerActiveCalls[ldcServer]--;
                }
                else
                {
                    LDCServerActiveCalls.Remove(ldcServer);
                }

                printContents(LDCServerActiveCalls);
            }
        }


        /// <summary>
        /// Increase the number of connected clients by 1.
        /// </summary>
        public static void notifyClientConnected()
        {
            LogWriter.debug("Monitoring.notifyClientConnected");

            ClientsConnected++;

            LogWriter.debug("ClientsConnected : " + ClientsConnected);
        }

        /// <summary>
        /// Descrease the number of connected clients by 1.
        /// </summary>
        public static void notifyClientDisconnected()
        {

            LogWriter.debug("Monitoring.notifyClientDisconnected " );


            if (ClientsConnected > 0)
            {
                ClientsConnected--;
            }
            else
            {
                ClientsConnected = 0;
            }

            LogWriter.debug("ClientsConnected : " + ClientsConnected);
    
        }

    }
}
