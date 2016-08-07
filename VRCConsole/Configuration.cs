using System;
using System.Collections;
using System.Collections.Generic;
using GlobalObjects;
using vrc.Properties;
using System.Diagnostics;

namespace vrc
{
	/// <summary>
	/// Reads and sets the configuration.
	/// (Static class)
	/// </summary>
    /// 
    [Serializable()]
	public static class Configuration
	{
		#region members
        

       
        private static String _ldcServerList = "127.0.0.1";		// serverConnections - list of LDC server
        private static int _ldcTimout = 3;			// serverConnections - LDC communication timeout in seconds

        private static int _vrcpPort = 20011;				// clientConnections - port number for VRCP communication
        
        private static int _pingInterval = 10;			// connectionGuard - time interval for LDC server ping
        private static int _loginInterval = 3;			// connectionGuard - time interval for reconnection trial to the LDC server
        private static int _downTime = 60;              // connectionGuard - maximal down time for ldc server before escalation takes place

        private static String _dsn;				    // transactionSaver - connection string for VRC data base
        private static String _vrcdbServer;		    // transactionSaver - name of VRC DB server
        private static String _dbUser;				// transactionSaver - DB user name
        private static String _dbPasswd;			// transactionSaver - DB users password
        private static String _dbName;				// transactionSaver -  name of the VRC DB
        private static String _exportPathFileName;  // transactionSaver - full path and filename for export file
 
        private static int _pollingInterval = 30;		// poolOptimizer - time intervall (in seconds) for poolObject check
        private static int _objectLifeTime = 200;		// poolOptimizer - life time of pool objects without action in seconds
        
        private static int _commandIteration = 2;		// ldcClient - number of retrials for an  LDC server connection
        private static int _holdingTime = 1;			// ldcClient - time period between command repetition
        private static int _trafficDuration = 60;       // ldcClient - maximal duration for a transaction

        private static String _logDirectory = "c:/temp" ;	 // LogWriter - full path for logs
        private static String _logLevel = "_ALL";		 // LogWriter - default logging level (_OFF, _ERROR, _DEBUG, _INFO, _ALL )

        private static String _smtpServer;			// mailSender - smtp Server
        private static String _mailRecipient;		// mailSender - recipient's mail address
        private static int _smtpServerPort = 25;    // mailSender - SMTP Server port


        private static DSVRCConfig  configDS;        // hold the values from remote configuration service
        private static String filepath = new Settings().LocalConfigFilePath; // the path for saving local configuration file 
                                                                             // (read from application setting, the reason is 
                                                                             // that the value is only used when remote configuration
                                                                             // does "NOT" work, so that it can not be read from configuration file
                                                                             // itself)   


        private static List<IConfigurationListener> observerList = new List<IConfigurationListener>(); //List for holding the registered observers

		#endregion

		#region properties 
        
        /// <summary>
        ///  LogWriter - full path for logs
        /// </summary>
        public static String logDirectory
        {
            get {      
                return _logDirectory;
            }
            set { _logDirectory = value; }
        }


        /// <summary>
        /// LogWriter - default logging level (_OFF, _ERROR, _DEBUG, _INFO, _ALL )
        /// </summary>
        public static String logLevel
        {
            get
            {

                return _logLevel;
            }
            set { _logLevel = value; }
        }


        /// <summary>
        /// mailSender - for mail notification
        /// </summary>
		public static String smtpServer {
			get{
                return _smtpServer; 
            }
			set{ _smtpServer = value; }
		}


        /// <summary>
        /// mailSender - smtp Server
        /// </summary>
        public static int smtpServerPort
        {
            get
            {
                return _smtpServerPort;
            }
            set { _smtpServerPort = value; }
        }


        /// <summary>
        /// mailSender - recipient's mail address
        /// </summary>
        public static String mailRecipient
        {
            get
            {
                return _mailRecipient;
            }
			set{ _mailRecipient = value; }
		}


        /// <summary>
        /// clientConnections - port number for VRCP communication
        /// </summary>
		public static int vrcpPort {
            get
            {
                return _vrcpPort;
            }
			set{ _vrcpPort = value; }
		}


        /// <summary>
        /// serverConnections - list of LDC server
        /// </summary>
        public static String ldcServerList
        {
            get
            {
                return _ldcServerList;
            }
			set{ _ldcServerList = value; }
		}


        /// <summary>
        /// serverConnections - LDC communication timeout in seconds
        /// </summary>
        public static int ldcTimout
        {
            get
            {
                return _ldcTimout;
            }
			set{ _ldcTimout = value; }
		}


        /// <summary>
        /// connectionGuard - time interval for LDC server ping
        /// </summary>
		public static int pingInterval {
            get
            {
                return _pingInterval;
            }
			set{ _pingInterval = value; }
		}

		/// <summary>
		/// time interval for reconnection trial to the LDC server
		/// </summary>
		public static int loginInterval {
            get
            {
                return _loginInterval;
            }
			set{ _loginInterval = value; }
		}


        /// <summary>
        /// maximal down time for ldcserver before escalation triggers
        /// </summary>
        public static int downTime
        {
            get
            {
                return _downTime;
            }
            set { _downTime = value; }
        }


		/// <summary>
		/// time intervall for poolObject check
		/// </summary>
		public static int pollingInterval {
            get
            {
                return _pollingInterval;
            }
			set{ _pollingInterval = value; }
		}


        /// <summary>
        /// poolOptimizer - life time of pool objects without action 
        /// </summary>
        public static int objectLifeTime
        {
            get
            {
                return _objectLifeTime;
            }
			set{ _objectLifeTime = value; }
		}


        /// <summary>
        /// ldcClient - number of retrials for an  LDC server connection
        /// </summary>
        public static int commandIteration
        {
            get
            {
                return _commandIteration;
            }
			set{ _commandIteration = value; }
		}


        /// <summary>
        /// ldcClient - time period between command repetition
        /// </summary>
        public static int holdingTime
        {
            get
            {
                return _holdingTime;
            }
			set{ _holdingTime = value; }
		}


        
        /// <summary>
        /// ldcClient - maximal traffic duration
        /// </summary>
        public static int trafficDuration
        {
            get
            {
                return _trafficDuration;
            }
            set { _trafficDuration = value; }
		}



        /// <summary>
        /// transactionSaver - connection string for VRC data base
        /// </summary>
        public static String dsn
        {
            get
            {
                return _dsn;
            }
			set{ _dsn = value; }
		}


        /// <summary>
        /// transactionSaver - name of VRC DB server
        /// </summary>
        public static String vrcdbServer
        {
            get
            {
                return _vrcdbServer;
            }
			set{ _vrcdbServer = value; }
		}


        /// <summary>
        /// transactionSaver - DB user name
        /// </summary>
        public static String dbUser
        {
            get
            {
                return _dbUser;
            }
			set{ _dbUser = value; }
		}


        /// <summary>
        /// transactionSaver - DB users password
        /// </summary>
        public static String dbPasswd
        {
            get
            {
                return _dbPasswd;
            }
			set{ _dbPasswd = value; }
		}


        /// <summary>
        /// transactionSaver -  name of the VRC DB
        /// </summary>
        public static String dbName
        {
            get
            {
                return _dbName;
            }
			set{ _dbName = value; }
		}


        /// <summary>
        /// transactionSaver - full path and filename for export file
        /// </summary>
        public static String exportPathFileName
        {
            get
            {
                return _exportPathFileName;
            }
            set { _exportPathFileName = value; }
        }

		#endregion


		#region methods


        /// <summary>
        /// Register a IConfigurationListener
        /// </summary>
        /// <param name="listner">the listener object who implements the IConfigurationListener interface</param>
         public static void registerObserver(IConfigurationListener listener)
        {
            // add the new listener object into the oberserver List (sychronised)
            lock (((IList)observerList).SyncRoot)
            {
                observerList.Add(listener);

            }
        }


        /// <summary>
        /// Unregister an IConfigurationListener
        /// </summary>
        /// <param name="ldcClient">the listener object who implements the IConfigurationListener interface</param>
        public static void removeObserver(IConfigurationListener listener)
        {
            // remove the listener object from the oberserver List (sychronised)
            lock (((IList)observerList).SyncRoot)
            {
                observerList.Remove(listener);

            }
        }


        /// <summary>
        /// Notify all the observers of the configuration change
        /// </summary>
         private static void notifyOberservers()
         {
             // iterate through all listeners and call the listerner method (sychronised)
             lock (((IList)observerList).SyncRoot)
             {
                 foreach (IConfigurationListener listener in observerList)
                 {
                     listener.notifyConfigurationChanged();
                 }

             }
         }
        
        
        /// <summary>
        /// Load the configuration values passed by vcrds
        /// If the vcrds is null
        /// then loads the local configuration instead, which is saved as
        /// the most recent remote configuration loaded successfully
        /// </summary>
        /// <returns>succeed/fail</returns>
         public static Boolean loadConfig(DSVRCConfig vcrds)
        {
            if (vcrds == null){
                    // initialize with local config
                return loadLocalConfig();
            
            }

            configDS = vcrds;
            initialize();

            try
            {
                // save the Dataset loaded from remote into local configuration
                // for use in the next time (when remote config doesnt work)
                vcrds.WriteXml(filepath);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Fail to save local configuration. Details: " + e);
                LogWriter.logAsEvent("Fail to save local configuration. Details: " + e, EventLogEntryType.Error);

            }

            return true;
        }


        /// <summary>
        /// Load the configuration from local xml file
        /// </summary>
        /// <returns>succeed/fail</returns>
		public static Boolean loadLocalConfig(){

            if (String.IsNullOrEmpty(filepath))
            {
                throw new Exception("no local config file path set in the application setting");
            }

            configDS = new DSVRCConfig();
            
            try
            {
                configDS.ReadXml(filepath);
                
                // initialize members from the typed DataSet
                initialize();
                return true;
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Configuration.loadLocalConfig: Fail to load local configuration file. Details: " + e);
                LogWriter.logAsEvent("Configuration.loadLocalConfig: Fail to load local configuration file. Details: " + e, EventLogEntryType.Error);
            }

            return false;

		}

    
        /// <summary>
        /// Help method - initialize the data members with a typed dataset
        /// </summary>
        /// <param name="configDS">the typed Dataset</param>
        private static void initialize()
        {
            //Server Connections
            if (configDS.ServerConnections.Rows.Count > 0)
            {
                _ldcServerList = configDS.ServerConnections[0].ServerList;
                _ldcTimout = configDS.ServerConnections[0].Timeout * 1000;
            }


            //Client Connection
            if (configDS.ClientConnections.Rows.Count > 0)
            {
                _vrcpPort = configDS.ClientConnections[0].portNumber;
            }

            //Connection Guard
            if (configDS.ConnectionGuard.Rows.Count > 0)
            {
                _pingInterval = configDS.ConnectionGuard[0].PingInterval * 1000;
                _loginInterval = configDS.ConnectionGuard[0].LoginInterval * 1000;
                _downTime = configDS.ConnectionGuard[0].DownTime * 1000;
            }

            //Transaction Saver
            if (configDS.TransactionSaver.Rows.Count > 0)
            {
                _dsn = configDS.TransactionSaver[0].Dsn;
                _vrcdbServer = configDS.TransactionSaver[0].ServerName;
                _dbName = configDS.TransactionSaver[0].DataBaseName;
                _dbUser = configDS.TransactionSaver[0].UserID;
                _dbPasswd = configDS.TransactionSaver[0].Password;
                _exportPathFileName = configDS.TransactionSaver[0].ExportPathFileName;
            }

            //Pool Optimizer
            if (configDS.PoolOptimizer.Rows.Count > 0)
            {
                _pollingInterval = configDS.PoolOptimizer[0].PollingInterval * 1000;
                _objectLifeTime = configDS.PoolOptimizer[0].ObjectLifeTime * 1000;
            }

            //LDC Client
            if (configDS.LDCClient.Rows.Count > 0)
            {
                _commandIteration = configDS.LDCClient[0].CommandIteration;
                _holdingTime = configDS.LDCClient[0].HoldingTime * 1000;
                _trafficDuration = configDS.LDCClient[0].TrafficDuration * 1000;
            }

            //Log Writer
            if (configDS.LogWriter.Rows.Count > 0)
            {
                _logDirectory = configDS.LogWriter[0].PathFileName;
                _logLevel = configDS.LogWriter[0].LogLevel;
            }

            //Mail Sender
            if (configDS.MailSender.Rows.Count > 0)
            {
                _smtpServer = configDS.MailSender[0].ServerName;
                _smtpServerPort = configDS.MailSender[0].SmtpServerPort;
                _mailRecipient = configDS.MailSender[0].Recipient;
            }


        }


        /// <summary>
        /// listener method called when the remote configuration changes
        /// (per Remoting)
        /// </summary>
		public static Boolean reloadConfiguration(DSVRCConfig vcrds){
            
            //just try to reload the new value from service
            Boolean fSucceed = loadConfig(vcrds);

            // notify all the observers
            notifyOberservers();

            return fSucceed;
		}

        /// <summary>
        /// return the actual configuration used by VRC
        /// </summary>
        /// <returns></returns>
        public static DSVRCConfig getConfiguration() {
            return configDS;   
        }

 		#endregion

	}
}
