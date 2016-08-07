using System;
//using System.Net;
//using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using vrc.Properties;
//using System.Text;


namespace vrc
{

    # region command object
    /// <summary>
    /// super class to represent the parameters from client command
    /// </summary>
    public abstract class Pars
    {
    }

    /// <summary>
    /// class to represent the parameters sent with the USER_LOGIN request
    /// </summary>
    public class LoginCmdPars : Pars
    {
        public String crmSys;
        public String vrSys;
        public String ldcSvr;
        public int stationId;
        public int agentId;
        public String workingGrp;
    }

    /// <summary>
    /// class to represent the parameters sent with the SAVE_RECORDING_REFERENCE request
    /// </summary>
    public class SRRCmdPars : Pars
    {
        public String project;
        public String campaign;
        public String DBServer;
        public String recordID;
    }


    /// <summary>
    /// class to represent the parameters sent with the SAVE_RECORDING_DATA request
    /// </summary>
    public class SRDCmdPars : Pars
    {
        public String project;
        public String campaign;
        public String data1;
        public String data2;
        public String data3;
        public String data4;
        public String data5;
    }


    /// <summary>
    /// class to represent the parameters sent with the SET_INFO_FLAG request
    /// </summary>
    public class SIFCmdPars : Pars
    {
        public int value;
    }

    public class CommandObject
    {
        Guid uid; // the unique id associated with the command

        public Guid Uid
        {
            get { return uid; }
            set { uid = value; }
        }

        VRCPCmd cmd; // the VRCP command sent by the client

        public VRCPCmd Cmd
        {
            get { return cmd; }
            set { cmd = value; }
        }


        private Pars pars; // the parameters list of the command

        public Pars Pars
        {
            get { return pars; }
            set { pars = value; }
        }

        private String reqString; // the whole request string. For logging purpose

        public String ReqString
        {
            get { return reqString; }
            set { reqString = value; }
        }
    }
    #endregion

    /// <summary>
    /// The Session represents a connection with the client
    /// </summary>
    public class Session
    {

        // each session is associated with one ldcClient
        LDCClient ldcClient;

        // the socket connection with the client
        Socket mClientSocket;

        // the IP address of the client
        String mRemoteClientIP;

        // mark whether this instance is once disposed
        volatile Boolean disposed;

        // for sychronisation of the Dispose method
        private static Object sync = new Object();

        /// <summary>
        /// Constructor - create a new session for a client's connection
        /// </summary>
        /// <param name="clientSocket">socket connection for the client</param>
        public Session(Socket clientSocket)
        {
            mClientSocket = clientSocket;

            try
            {
                mRemoteClientIP = clientSocket.RemoteEndPoint.ToString();
            }
            catch (Exception e)
            {
                LogWriter.error("Session: unable to get client ip, Details " + e);
            }

            // notify Monitoring class that a new client is connected
            Monitoring.notifyClientConnected();

        }

        /// <summary>
        /// Parse the incoming string and get the command together with uid
        /// delegate the command to the LDC-Client by adding the command into 
        /// the LDC-Client's command queue
        /// </summary>
        /// <param name="cmdStr">the string sent by the client</param>
        public String delegateCmd(String cmdStr) //, out Boolean fCloseConnection )
        {
            // set not to close connection as default
            //fCloseConnection = false;

            // trim the '\n' on end of the request string
            cmdStr = cmdStr.TrimEnd('\n');

            // get the command by parsing the request string    
            CommandObject cmdObj = parseCmd(cmdStr);

            if (cmdObj != null) // parse succeed
            {
                // check whether the command is  defined
                if (cmdObj.Cmd == VRCPCmd.UNKNOWN)
                {
                    //fCloseConnection = true;
                    return "Unknown command";
                }

                // get command together with uid
                Guid ldccID = cmdObj.Uid;
                VRCPCmd cmd = cmdObj.Cmd;


                // log in
                if (cmd.Equals(VRCPCmd.USER_LOGIN) && ldccID == Guid.Empty)
                {

                    // Check whether another LDCClient has already logged in 

                    ldcClient = findExistingClient(cmdObj);

                    if (ldcClient != null && ldcClient.isBounded())
                    {

                        // dispose the old ldcclient
                        ldcClient.logout();

                    }


                    // generate a new LDC-Client instance 
                    //(the mLastActivity member is updated by constructor)
                    ldcClient = new LDCClient();

                    // pass the ip address of the remote client
                    ldcClient.LdcClientIPAddress = mRemoteClientIP;

                    // pass this session instance to the ldcClient
                    ldcClient.setCurrentSession(this);

                    // update the last activity time
                    ldcClient.mLastActivity = DateTime.Now;

                    // mark whether login succeed
                    Boolean fLoginSuccess = false;

                    // add the new LDCClient instance into pool
                    PoolManager.Instance.AddObject(ldcClient);

                    // delegate the login command
                    String reply = ldcClient.processLogin(cmdObj, out fLoginSuccess);

                    if (!fLoginSuccess)
                    {
                        // login failed
                        ldcClient.Dispose(false);

                    }

                    // return message to client
                    return reply;
                }

                // other commands
                else if (ldccID != Guid.Empty)
                {
                    // search for the exisiting LDC-Client from Pool
                    ldcClient = (LDCClient)PoolManager.Instance.ReleaseObject(ldccID);

                    if (ldcClient != null)
                    {
                        // update the last activity time
                        ldcClient.mLastActivity = DateTime.Now;

                        // pass the ip address of the remote client
                        ldcClient.LdcClientIPAddress = mRemoteClientIP;

                        // pass this session instance to the ldcClient
                        ldcClient.setCurrentSession(this);

                        return ldcClient.processCommand(cmdObj);
                    }
                    else
                    {
                        // can't retrieve the client from pool (uid not exists)
                        LogWriter.error(this.mRemoteClientIP + " Session: LDC-Client does not exist (invalid uid) " + cmdStr);
                        return InvalidUid(cmd);
                    }

                }

                // no uid provided within the command
                else
                {
                    // no uid provided with the command
                    LogWriter.error(this.mRemoteClientIP + " Session: No uid available in the request " + cmdStr);
                    //fCloseConnection = true;
                    return "UID wrong \n";
                }
            } // end if (cmdObject != null)

            //fCloseConnection = true;
            return "Syntax error within the command \n";  // parse failed (cmdObject == null)

        }


        /// <summary>
        /// Construct the error reply message when an old client is already logged in 
        /// </summary>
        /// <param name="vRCPCmd">VRCPCmd.USER_LOGIN</param>
        /// <param name="guid">unique_id of the old client</param>
        /// <returns>reply message indicating the error</returns>
        private string RepeatedLoginErr(VRCPCmd vRCPCmd, Guid guid)
        {
            // return failure message and the guid of the current logged in client (help to notify client about the error)
            String reply = vRCPCmd.ToString("G") + "_FAILURE" + ";" + guid.ToString();

            // append '\n' (according to VRCP) 
            return reply + '\n';
        }



        /// <summary>
        /// Used to find the LDCClient instance in the Object Pool which 
        /// has already done the the login(same login parameters as cmdObj )
        /// </summary>
        /// <param name="cmdObj">the cmdObj representing USER_LOGIN</param>
        /// <returns>the LDCClient instance if found one; 
        /// otherwise, null </returns>
        private LDCClient findExistingClient(CommandObject cmdObj)
        {
            // Only allowed to be called when the current cmd is VRCPCmd.USER_LOGIN
            if (!cmdObj.Cmd.Equals(VRCPCmd.USER_LOGIN))
            {
                throw new ArgumentException("The cmdObj is not USER_LOGIN");
            }

            // Get login parameters from current cmdObj (agentID, stationID, workingGroup)
            LoginCmdPars loginPars = (LoginCmdPars)cmdObj.Pars;
            int stationID = loginPars.stationId;
            //int agentID = loginPars.agentId;
            //String workingGroup = loginPars.workingGrp;
            ////String vrSystem = loginPars.vrSys;
            ////String crmSystem = loginPars.crmSys;
            ////String ldcServer = loginPars.ldcSvr;

            //// Iterate through the object pool to find the LDCClient instance 
            //// with same login paramters
            //Hashtable objPool = PoolManager.Instance.ObjPool;
            //foreach (LDCClient ldc in objPool.Values)
            //{
            //    if (ldc.AgentID == agentID &&
            //        ldc.StationID == stationID &&
            //        ldc.WorkingGroup.Equals(workingGroup))
            //    {
            //        // find one existing instance
            //        return ldc;
            //    }
            //}

            //// not found
            //return null;

            return PoolManager.Instance.findLDCClientWithStationId(stationID);

        }



        /// <summary>
        /// This method returns a special failure message
        /// when the uid from client does not exist in the
        /// object pool.  
        /// </summary>
        /// <param name="cmd">the vrcp command</param>
        /// <returns>failure message with nullified uid</returns>
        private String InvalidUid(VRCPCmd cmd)
        {
            // return failure message with nullified guid (help to notify client about the error)
            String reply = cmd.ToString("G") + "_FAILURE" + ";" + Guid.Empty.ToString();

            // append '\n' (according to VRCP) 
            return reply + '\n';
        }


        /// <summary>
        /// Parse the request string from client (read from socket) according to VRCP protocal
        /// extract the relavate information (command, uid, etc) and combine them into the command object
        /// </summary>
        /// <param name="request">the request string sent from Client</param>
        /// <returns>
        ///  null: wrong syntax in the request, command parsing failed
        ///  CommandObject: command together with its parameters
        ///  (The Cmd member is set to VRCPCmd.Unknown if the command is unknown)
        /// </returns>
        private CommandObject parseCmd(String request)
        {

            CommandObject requestCmdObj = new CommandObject();

            requestCmdObj.ReqString = request;
            VRCPCmd reqCmd = getCmd(request);

            //unknown command
            if (reqCmd == VRCPCmd.UNKNOWN)
            {

                LogWriter.error(this.mRemoteClientIP + " Session: error, unknown command " + request);
                requestCmdObj.Cmd = reqCmd;

            }

            //log in
            else if (reqCmd == VRCPCmd.USER_LOGIN)
            {

                // receive USER_LOGIN command
                requestCmdObj.Cmd = reqCmd;

                // extract parameters from Request String:
                // USER_LOGIN; CRM System ; VR System; LDC-Server; station ID; agent ID; working group <10>
                char[] sep = { ';' };
                String[] pars = request.Split(sep);

                if (pars == null || pars.Length != 7)
                {
                    return null;
                }


                try
                {
                    LoginCmdPars par = new LoginCmdPars();
                    par.crmSys = pars[1]; // get CRM System
                    par.vrSys = pars[2];  // get VR System
                    par.ldcSvr = pars[3]; // get LDC Server
                    par.stationId = int.Parse(pars[4]); // get Station ID
                    par.agentId = int.Parse(pars[5]);  // get Agent ID
                    par.workingGrp = pars[6]; // get Working Group

                    requestCmdObj.Pars = par;
                }
                catch
                {
                    LogWriter.error(this.mRemoteClientIP + " Session: command has unknown format " + request);
                    return null;
                }


            }

            // save recording reference 
            else if (reqCmd == VRCPCmd.SAVE_RECORDING_REFERENCE)
            {

                // save command
                requestCmdObj.Cmd = reqCmd;

                // set uid
                Guid uid = getUid(request);
                requestCmdObj.Uid = uid;

                // extract parameters from Request String:
                // SAVE_RECORDING_REFERENCE ; unique ID ; project ; campaign ; DB Server ; record ID <10>
                char[] sep = { ';' };
                String[] pars = request.Split(sep);

                if (pars == null || pars.Length != 6)
                {
                    return null;
                }


                SRRCmdPars par = new SRRCmdPars();
                par.project = pars[2]; // get project
                par.campaign = pars[3];  // get campaign
                par.DBServer = pars[4]; // get DB Server
                par.recordID = pars[5]; // get record ID

                //set parameters
                requestCmdObj.Pars = par;

            }

            // save recording data 
            else if (reqCmd == VRCPCmd.SAVE_RECORDING_DATA)
            {

                // save command
                requestCmdObj.Cmd = reqCmd;

                // set uid
                Guid uid = getUid(request);
                requestCmdObj.Uid = uid;

                // extract parameters from Request String:
                // SAVE_RECORDING_DATA ; unique ID ; project ; campaign ; data1 ; data2 ; data3 ; data4 ; data5 <10>
                char[] sep = { ';' };
                String[] pars = request.Split(sep);

                if (pars == null || pars.Length != 9)
                {
                    return null;
                }


                try
                {
                    SRDCmdPars par = new SRDCmdPars();
                    par.project = pars[2];   // get project
                    par.campaign = pars[3];  // get campaign
                    par.data1 = pars[4];     // get data1
                    par.data2 = pars[5];     // get data2
                    par.data3 = pars[6];     // get data3
                    par.data4 = pars[7];     // get data4
                    par.data5 = pars[8];     // get data5

                    //set parameters
                    requestCmdObj.Pars = par;
                }
                catch
                {
                    LogWriter.error(this.mRemoteClientIP + " Session: command has unknown format " + request);
                    return null;
                }

            }

            // set info flag 
            else if (reqCmd == VRCPCmd.SET_INFO_FLAG)
            {

                // save command
                requestCmdObj.Cmd = reqCmd;

                // set uid
                Guid uid = getUid(request);
                requestCmdObj.Uid = uid;

                // extract parameters from Request String:
                // SET_INFO_FLAG;unique ID;value<10>
                char[] sep = { ';' };
                String[] pars = request.Split(sep);

                if (pars == null || pars.Length != 3)
                {
                    return null;
                }


                try
                {
                    SIFCmdPars par = new SIFCmdPars();
                    par.value = int.Parse(pars[2]);   // get integer value

                    //set parameters
                    requestCmdObj.Pars = par;
                }
                catch
                {
                    LogWriter.error(this.mRemoteClientIP + " Session: command has unknown format " + request);
                    return null;
                }


            }

            // control command
            else
            {
                //save command
                requestCmdObj.Cmd = reqCmd;

                //set uid
                Guid uid = getUid(request);
                requestCmdObj.Uid = uid;
            }

            return requestCmdObj;
        }


        /// <summary>
        /// Find out the VRCP command in request string by enumerating
        /// all the commands in VRCPCmd 
        /// </summary>
        /// <param name="request">request string</param>
        /// <returns>VRCP command</returns>
        private VRCPCmd getCmd(string request)
        {
            String[] cmds = Enum.GetNames(typeof(VRCPCmd));

            foreach (String cmd in cmds)
            {
                if (request.Contains(cmd))
                {
                    return (VRCPCmd)Enum.Parse(typeof(VRCPCmd), cmd);
                }
            }

            return VRCPCmd.UNKNOWN;
        }



        /// <summary>
        /// Help method to extract Unique ID from request string
        /// </summary>
        /// <param name="request">request string</param>
        /// <returns>
        /// Unique ID
        /// Guid.Empty when fail to extract unique ID 
        /// </returns>
        private Guid getUid(String request)
        {
            // extract UID  from Request String:
            // COMMAND ; unique ID ... <10>
            char sep = ';';
            String[] pars = request.Split(sep);

            if (pars == null || pars.Length < 2)
            {
                LogWriter.error(mRemoteClientIP + " Session: Fail to get user id from request" + request);
                return Guid.Empty;
            }

            Guid uid;
            try
            {
                uid = new Guid(pars[1]);

            }
            catch
            {
                uid = Guid.Empty;
            }
            return uid;
        }


        /// <summary>
        /// This method is called when session ends (client disconnected):
        /// 1) The socket with client will be closed 
        /// 2) The data member "currentSession" of the bounded LDCClient is reset to null (so that the PoolOptimizer 
        /// is allowed to remove the LDCClient)
        /// 3) The Monitoring class will be notified to count down the number of connected clients
        /// </summary>
        internal void Dispose()
        {

            LogWriter.debug("Session.Dispose: the session is disposed now");

            lock (sync)
            {
                // each Session needs/can be disposed only once
                if (!disposed)
                {

                    if (ldcClient != null)
                    {
                        // nullify the current session for ldcClient  
                        ldcClient.setCurrentSession(null);

                    }

                    // disconnect with client
                    if (mClientSocket != null)
                    {
                        try
                        {
                            mClientSocket.Close();
                        }
                        catch
                        {
                        }
                        finally
                        {
                            mClientSocket = null;
                        }

                    }

                    // notify Monitoring to count down the client connection
                    LogWriter.debug("Session.Dispose: VRC closed the connection with client " + mRemoteClientIP);
                    Monitoring.notifyClientDisconnected();

                    disposed = true;
                }
                else
                {
                    LogWriter.debug("Session.Dispose: unable to dispose a session object twice");

                }
            }

        }

        // unbound the ldcClient
        public void unBound()
        {
            ldcClient = null;
        }
    }
}
