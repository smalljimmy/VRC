using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using CodeBureau;
using System.Text.RegularExpressions;
using vrc.Properties;
using System.Data.OleDb;
using System.Threading;

namespace vrc
{
    /// <summary>
    /// Class for communication with LDC-Server and its Database
    /// used for Login purpose
    /// </summary>
    public class Communication : AsynchSocketManager, IDisposable
    {

        #region members

        private String dbOfVRServer = "";  // save the database server of VR system
        private String USERID_QUERY = new Settings().userid_queryStr; // get the string to query for userid in LDC-Server's datebase 

        private static int initFailures = 0; // keep the number of successive init failures
        
        public enum ERROR { NONE, RECEIVE_WRONG_MESSAGE, SERVER_UNAVAILABLE, RECEIVE_EMPTY_MESSAGE}; // list of error types for last error message
        private ERROR lastError = ERROR.NONE; // save the last error message happened in sendMessage
        public String userID = null; // the userId used for logging onto LDC-Server

        /// <summary>
        /// events used for examine whether the request is successful on ldcserver
        /// </summary>
        public AutoResetEvent requestSucceedEvent = new AutoResetEvent(false);
        public AutoResetEvent requestFailedEvent = new AutoResetEvent(false);

        #endregion


        #region methods

        public Communication()
        {
        }

        public Communication(string _hostName, int _port) : base(_hostName, _port)
        {
            
        }

        /// <summary>
        /// Initialize the communication with LDC-Server 
        /// (make sending commando && Ping-Pong available):
        ///  1. create the socket connection with LDC-Server
        ///  2. logon the LDC-Server with user-id
        /// </summary>
        /// <returns>succeed/fail</returns>
        public virtual bool initCommunication()
        {
           
            OnConnect = new OnConnectDelegate(HandleInitiateConnection);
            OnSend = new OnSendDelegate(HandleRequest); ;
            OnReceive = new OnReceiveDelegate(HandleLogin);
            OnDisconnect = new OnDisconnectDelegate(HandleEndConnection);
 
             
            this.Connect();

            int index = WaitHandle.WaitAny(new WaitHandle[] { requestFailedEvent, requestSucceedEvent }, Configuration.ldcTimout * 6, false);
            if (index == 0 || index == WaitHandle.WaitTimeout)
            {
                if (index == WaitHandle.WaitTimeout)
                {
                    LogWriter.debug(this.GetType().Name + ".initCommunication: Time out occurs for login");
                }

                this.Disconnect();
                initFailures++;
                return false;
            }
            else
            {
                initFailures = 0;
                LogWriter.info(this.GetType().Name + ".initCommunication:  Succeed to login to {0}", new ServerEntry(this.hostname, this.port));
                return true;
            }


        }

        
        protected void HandleInitiateConnection(AsynchSocketManager socketMgr, bool bSuccess)
        {
            if (!bSuccess)
            {
                LogWriter.error("Communication.OnConnect: Fail to establish Socket with {0}", new ServerEntry(this.hostname, this.port));
                requestFailedEvent.Set();
 
            }
            else
            {
                clearMsgQueue();
 
            }
        }


        protected void HandleRequest(AsynchSocketManager socketMgr, string msg)
        {
            string temp1;
            string temp2;
            HelperTools.logReqMsg(msg, out temp1, out temp2, this);
        }


        protected void HandleLogin(AsynchSocketManager socketMgr, string msg)
        {
            string rep_head;
            string rep_content;
            HelperTools.logRespMsg(msg, out rep_head, out rep_content, this);
            String identifier = " db on ";

            if (msg.Contains(identifier))
            {
                int position = msg.IndexOf(identifier) + identifier.Length;
                this.dbOfVRServer = msg.Substring(position).Trim();
                LogWriter.debug(this.GetType().Name + ".initCommunition: db of VR Server: " + dbOfVRServer);


                if (userID == null)
                {
                    sendMessage(LDCCmd.DB_STRING_REQUEST, "");
                }
                else
                {
                    // use the User-ID to login 
                    sendMessage(LDCCmd.USER_LOGIN_REQUEST, "UserID={" + userID + "}" + (char)5 + "AppID=18");
                }

            }
            else if (rep_head.Contains(StringEnum.GetStringValue(LDCCmd.DB_STRING_REPLY)))
            {

                userID = getUserID(rep_content);
                if (userID == null)
                {
                    LogWriter.error(this.GetType().Name + ": fail to get User-ID from LDC-Server's database");
                    requestFailedEvent.Set();
                    return;
                }

                // use the User-ID to login 
                sendMessage(LDCCmd.USER_LOGIN_REQUEST, "UserID={" + userID + "}" + (char)5 + "AppID=18");
            }
            else if (rep_head.Contains(StringEnum.GetStringValue(LDCCmd.USER_LOGIN_REPLY)))
            {
                // Login succeed
                requestSucceedEvent.Set();
            }
            else
            {
                requestFailedEvent.Set();
            }
        }


        protected void HandleEndConnection(AsynchSocketManager socketMgr)
        {
            LogWriter.info("{0}.OnDisconnect: the communication with {1} is disconnected.", this.GetType().Name, new ServerEntry(socketMgr.hostname, socketMgr.port));
        }


        /// <summary>
        /// get the Database of VR Server (obtained during initCommunication())
        /// </summary>
        /// <returns>Database Server</returns>
        public String getDBOfVRServer()
        {
            if (dbOfVRServer.Length == 0)
            {
                LogWriter.error(this.GetType().Name + ".getDBOfVRServer: The db server is unknown.");
            }

            return dbOfVRServer;

        }


        
        /// <summary>
        /// Send a message to the LDC-Server and retrieve the reply (asychronous) 
        /// </summary>
        /// <param name="req_categorieNumber"> category number of the message</param>
        /// <param name="req_Message">message content</param>
        /// <returns> succeed / fail </returns>
        public void sendMessage(LDCCmd req_categorieNumber, string req_Message)
        {
            // send message to LDC-Server when request is set
            if (req_categorieNumber != LDCCmd.UNKNOWN && req_Message != null)
            {
                string request = HelperTools.makeCmdStr(req_categorieNumber, req_Message);
                this.Send(request);

            }

        }


        /// <summary>
        /// Get the User-ID from the database of the LDC-Server (OLEDB)
        /// </summary>
        /// <returns>User-ID</returns>
        private String getUserID(String connStr)
        {
            if (connStr == null)
            {
                return null;
            }

            // Purge the prefix
            if (connStr.StartsWith("DBString="))
            {
                connStr = connStr.Substring("DBString=".Length);
            }

            String userID = null;

            // the query string for getting the user id
            String queryString = USERID_QUERY;

            using (OleDbConnection connection = new OleDbConnection(connStr))
            {
                try
                {
                    // prepare command 
                    OleDbCommand command = new OleDbCommand(queryString, connection);

                    // open connection
                    connection.Open();

                    // execute query
                    userID = ((Guid)command.ExecuteScalar()).ToString();
                }
                catch (Exception e)
                {
                    LogWriter.error(this.GetType().Name + ".getUserID: Faile to get user id from DB. Details: " + e);
                }
            }

            return userID;
        }

        /// <summary>
        /// return the last error string 
        /// </summary>
        /// <returns>last error in sendMessage</returns>
        public ERROR getLastError()
        {
            ERROR ret = this.lastError;
            
            // reset the error message
            lastError = ERROR.NONE;

            return ret;
        }


        /// <summary>
        /// examine whether the ldcserver has gone down for milliumSeconds (calculated from initFailure)
        /// </summary>
        /// <returns></returns>
        public bool isDown(int milliumSeconds)
        {
            // compute the failTimes based on milliumSeconds
            int failTimes = milliumSeconds / Configuration.loginInterval;

            if (initFailures >= failTimes)
            {
                // reset initFailures
                initFailures = 0;
                return true;
            }

            return false;
        }


        /// <summary>
        /// realse the socket resource
        /// </summary>
        public void Dispose()
        {
            this.Disconnect();
        }



        #endregion

    }
}
