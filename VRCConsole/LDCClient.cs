using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Data.OleDb;
using vrc.Properties;


namespace vrc
{
    /// <summary>
    /// The LDCClient represents a specific connection between the client and the LDC-Server
    /// </summary>
    public class LDCClient : PoolObject, IDisposable
    {

        #region members

        private TransactionSaver _transactionSaver; // for log of transaction
        private volatile CommandObject _cmdObject; // hold the passed in command object
        private ServerEntry curAllowedServer = null; // the current server for processing the next command
        private bool bTranEnd = false; // mark the end of transaction
        private bool disposed = false; // mark whether this instance is once disposed
        private String voiceFileId; // hold the voiceFileId shortly (used for get GetVoiceFileID)
        private AutoResetEvent posRepEvent = new AutoResetEvent(false);// indicate the acknowledge reply is received
        private AutoResetEvent negRepEvent = new AutoResetEvent(false);// indicate the deny reply is received
        private Object sync = new Object(); // for sychronisation of the Dispose method
        private DateTime mLastTrafficTime; // the time when the last indication message for the traffic arrives 


        #endregion

        #region Property


        /// <summary>
        /// the actual traffic id in the transaction saver
        /// </summary>
        public string TrafficId
        {
            get { return _transactionSaver.getVoiceFileID(); }
            set
            {
                mLastTrafficTime = DateTime.Now; //update the traffic Time              
                _transactionSaver.setVoiceFileID(value);
            }
        }

        /// <summary>
        /// the time when the last indication message of the current traffic arrives
        /// </summary>
        public DateTime LastTrafficTime
        {
            get { return mLastTrafficTime; }
            set { mLastTrafficTime = value; }
        }


        /// <summary>
        /// indication of the bRecordingAvailable flag in the transaction saver
        /// </summary>
        public bool RecordingAvailable
        {
            get { return _transactionSaver.isRecordingAvailable(); }
            set { _transactionSaver.setRecordingAvailable(value); }
        }

        /// <summary>
        /// hold the reference to the current session object bounded on the LDCClient
        /// </summary>
        private Session currentSession;

        public Session CurrentSession
        {
            get { return currentSession; }
            set { currentSession = value; }
        }

        /// <summary>
        /// the ip address of the currently connected ldc client
        /// </summary>
        private String ldcClientIPAddress;
        public String LdcClientIPAddress
        {
            get { return ldcClientIPAddress; }
            set { ldcClientIPAddress = value; }
        }


        private int mPort;
        public int Port
        {
            get { return mPort; }
            set { mPort = value; }
        }


        private String mCrmSystem;

        public String CrmSystem
        {
            get { return mCrmSystem; }
            set { mCrmSystem = value; }
        }
        private String mVrSystem;

        public String VrSystem
        {
            get { return mVrSystem; }
            set { mVrSystem = value; }
        }
        private String mLdcServer;

        public String LdcServer
        {
            get { return mLdcServer; }
            set { mLdcServer = value; }
        }
        private int mStationID;

        public int StationID
        {
            get { return mStationID; }
            set { mStationID = value; }
        }
        private int mAgentID;

        public int AgentID
        {
            get { return mAgentID; }
            set { mAgentID = value; }
        }
        private String mWorkingGroup;


        public String WorkingGroup
        {
            get { return mWorkingGroup; }
            set { mWorkingGroup = value; }
        }


        #endregion

        #region methods

        public LDCClient()
        {
            // new UID for identification
            this.mUniqueID = System.Guid.NewGuid();

            // create new transactionSaver
            this._transactionSaver = new TransactionSaver();
        }

        /// <summary>
        /// assign the current allowed server which could handle the next command
        /// </summary>
        /// <param name="_curAllowedServer"></param>
        public void setCurAllowedLDCServer(ServerEntry _curAllowedServer)
        {
            curAllowedServer = _curAllowedServer;
        }

        public ServerEntry getCurAllowedLDCServer()
        {
            return curAllowedServer;
        }


        /// <summary>
        /// examine whether the transaction is ended
        /// </summary>
        public bool isTranEnd()
        {
            return bTranEnd;
        }


        /// <summary>
        /// clear/set the end of transaction
        /// </summary>
        public void setTranEnd(bool flag)
        {
            bTranEnd = flag;
        }


        /// <summary>
        /// Process the command by communicating with LDC-Server
        /// </summary>
        /// <param name="obj">Object to be added</param>
        /// <returns>True if success, false otherwise</returns>
        public String processCommand(CommandObject cmdObj)
        {

            _cmdObject = cmdObj;
            VRCPCmd cmd = _cmdObject.Cmd;

            try
            {
                // despatch the command 
                if (cmd == VRCPCmd.USER_LOGIN)
                {
                    // intialize data members with login parameters
                    LoginCmdPars loginPars = (LoginCmdPars)_cmdObject.Pars;
                    mVrSystem = loginPars.vrSys;
                    mCrmSystem = loginPars.crmSys;
                    mLdcServer = loginPars.ldcSvr;
                    mAgentID = loginPars.agentId;
                    mStationID = loginPars.stationId;
                    mWorkingGroup = loginPars.workingGrp;

                    login();
                }
                else if (cmd == VRCPCmd.USER_LOGOUT)
                {
                    logout();

                }
                else if (cmd == VRCPCmd.SET_ONE_WAY)
                {
                    setOneWay();
                }
                else if (cmd == VRCPCmd.SET_TWO_WAY)
                {
                    setTwoWay();
                }

                else if (cmd == VRCPCmd.START_NEW_RECORDING)
                {
                    startNewRecording();
                }
                else if (cmd == VRCPCmd.PAUSE_RECORDING)
                {
                    pauseRecording();
                }
                else if (cmd == VRCPCmd.CONTINUE_RECORDING)
                {
                    continueRecording();
                }
                else if (cmd == VRCPCmd.SAVE_RECORDING_REFERENCE)
                {
                    saveRecordingReference();
                }
                else if (cmd == VRCPCmd.SAVE_RECORDING_DATA)
                {
                    saveRecordingData();
                }
                else if (cmd == VRCPCmd.GET_VOICEFILE_ID)
                {
                    getVoiceFileID();
                }
                else if (cmd == VRCPCmd.SET_INFO_FLAG)
                {
                    setInfoFlag();
                }

                return reportSuccess(cmd);
            }
            catch (VRCApplicationException)
            {
                return reportFail(cmd);
            }
            catch (Exception e)
            {
                // trigger escalation
                MailSender.Instance.send("VRC ERROR: Exception happens by processing cmd " + cmd + ",Details: " + e);
                LogWriter.error(ldcClientIPAddress + " LDCClient.processCommand: Exception happens by processing cmd " + cmd + ",Details: " + e);
                return reportFail(cmd);
            }
        }


        /// <summary>
        /// Start to login on LDC-Servers
        /// </summary>
        private void login()
        {
            LogWriter.info(ldcClientIPAddress + " LDCClient.login:" +
             ";StationID=" + this.mStationID +
             ";AgentID=" + this.mAgentID +
             ";WorkingGroup=" + this.mWorkingGroup);


            List<ServerEntry> ldcServers = ConnectionGuard.Instance.getOnlineLDCServersList();
            bool loginSucceed = false;

            foreach (ServerEntry ldcServer in ldcServers)
            {
                if (loginOneServer(ldcServer))
                {
                    loginSucceed = true;
                    ConnectionGuard.Instance.registerObserver(this, ldcServer);
                }
   
            }


            if (!loginSucceed)
            {
                ERROR("The LDCClient can not login to any of the LDC-Servers.");
            }

        }

        /// <summary>
        /// login on one LDC-Server
        /// </summary>
        /// <param name="ldcServer"></param>
        /// <returns></returns>
        private bool loginOneServer(ServerEntry ldcServer)
        {
            bool bResult;

            // set curAllowedServer, used for OnReceive()
            ServerEntry tmpServer = curAllowedServer;
            this.curAllowedServer = ldcServer;

            lock (sync)
            {

                // assign agent to station               
                ConnectionGuard.Instance.addToCmdQueue(LDCCmd.ASSIGN_AGENT_TO_STATION_REQUEST,
                                    "agent_id=" + this.AgentID + (char)5
                                  + "station_id=" + this.StationID, ldcServer);

                int index = WaitHandle.WaitAny(new WaitHandle[] { this.posRepEvent, this.negRepEvent }, Configuration.ldcTimout * 3, false);

                if (index == WaitHandle.WaitTimeout)
                {
                    LogWriter.info("ConnectionGuard.tryLogin: timeout by logining in to " + ldcServer);
                    bResult = false;
                }
                else if (index == 0)
                {
                    //ldccLogin succeed
                    LogWriter.info("ConnectionGuard.tryLogin: login succeeds on " + ldcServer);
                    bResult = true;

                }
                else
                {
                    //ldccLogin failed
                    LogWriter.info("ConnectionGuard.tryLogin: login is denied on " + ldcServer);
                    bResult = false;
                }
            }

            curAllowedServer = tmpServer;
            return bResult;

        }


        /// <summary>
        /// Ends the connection to LDC-Server
        /// Removed from Pool
        /// </summary>
        public void logout()
        {

            LogWriter.info(ldcClientIPAddress + " LDCClient.logout: Host=" + this.mLdcServer +
                ";StationID=" + this.mStationID +
                ";AgentID=" + this.mAgentID +
                ";WorkingGroup=" + this.mWorkingGroup);

            this.Dispose(false);

        }

        /// <summary>
        /// Start a new record explicitly. For every call, only one record is allowed
        /// </summary>
        private void startNewRecording()
        {
            try
            {
                sendCommand(LDCCmd.RECORDING_CONTROL_REQUEST, "proc_sel_mode=2" + (char)5
                            + "proc_id=" + mStationID + (char)5
                            + "rec_control_mode=5");

            }
            catch (TooManyRetryTimesException)
            {
                // trigger eskalation 
                MailSender.Instance.send("VRC ERROR", "One error happens by startNewRecording");
                ERROR("LDCClient.startNewRecording: The operation is given up while it has failed for too many times");
            }
            catch (NoTransactionException)
            {
                ERROR("LDCClient.startNewRecording: there's no transaction available");
            }
            catch (ReceiveDenyException)
            {
                ERROR("LDCClient.startNewRecording: the command is denied by LDC-Server");
            }
        }


        /// <summary>
        /// Pause a recording
        /// </summary>
        private void pauseRecording()
        {
            try
            {
                sendCommand(LDCCmd.RECORDING_CONTROL_REQUEST, "proc_sel_mode=2" + (char)5
                        + "proc_id=" + mStationID + (char)5
                        + "rec_control_mode=3");

            }
            catch (TooManyRetryTimesException)
            {
                // trigger eskalation 
                MailSender.Instance.send("VRC ERROR", "One error happens by pauseRecording");
                ERROR("LDCClient.pauseRecording: The operation is given up while it has failed for too many times");

            }
            catch (NoTransactionException)
            {
                ERROR("LDCClient.pauseRecording: there's no transaction available");
            }
            catch (ReceiveDenyException)
            {
                ERROR("LDCClient.pauseRecording: the command is denied by LDC-Server");
            }
        }


        /// <summary>
        /// Continue a recording
        /// </summary>
        private void continueRecording()
        {
            try
            {
                sendCommand(LDCCmd.RECORDING_CONTROL_REQUEST, "proc_sel_mode=2" + (char)5
                        + "proc_id=" + mStationID + (char)5
                        + "rec_control_mode=4");

            }
            catch (TooManyRetryTimesException)
            {
                // trigger eskalation 
                MailSender.Instance.send("VRC ERROR", "One error happens by continueRecording");
                ERROR("LDCClient.continueRecording: The operation is given up while it has failed for too many times");

            }
            catch (NoTransactionException)
            {
                ERROR("LDCClient.continueRecording: there's no transaction available");
            }
            catch (ReceiveDenyException)
            {
                ERROR("LDCClient.continueRecording: the command is denied by LDC-Server");
            }

        }


        /// <summary>
        /// Switch to One-Way mode
        /// </summary>
        private void setOneWay()
        {

            try
            {
                sendCommand(LDCCmd.RECORDING_CONTROL_REQUEST, "proc_sel_mode=2" + (char)5
                    + "proc_id=" + mStationID + (char)5
                    + "rec_control_mode=1");

            }
            catch (TooManyRetryTimesException)
            {
                // trigger eskalation 
                MailSender.Instance.send("VRC ERROR", "One error happens by setOneWay");
                ERROR("LDCClient.setOneWay: The operation is given up while it has failed for too many times");
            }
            catch (NoTransactionException)
            {
                ERROR("LDCClient.setOneWay: there's no transaction available");
            }
            catch (ReceiveDenyException)
            {
                ERROR("LDCClient.setOneWay: the command is denied by LDC-Server");
            }

        }


        /// <summary>
        /// Switch in Two-Way mode
        /// </summary>
        private void setTwoWay()
        {
            try
            {
                sendCommand(LDCCmd.RECORDING_CONTROL_REQUEST, "proc_sel_mode=2" + (char)5
                        + "proc_id=" + mStationID + (char)5
                        + "rec_control_mode=0");

            }
            catch (TooManyRetryTimesException)
            {
                // trigger eskalation 
                MailSender.Instance.send("VRC ERROR", "One error happens by setTwoWay");
                ERROR("LDCClient.setTwoWay: The operation is given up while it has failed for too many times");
            }
            catch (NoTransactionException)
            {
                // trigger eskalation 
                MailSender.Instance.send("VRC ERROR", "One error happens by setTwoWay");
                ERROR("LDCClient.setTwoWay: there's no transaction available");
            }
            catch (ReceiveDenyException)
            {
                ERROR("LDCClient.setTwoWay: the command is denied by LDC-Server");
            }

        }


        /// <summary>
        /// Set the info flag for a transaction
        /// </summary>
        private void setInfoFlag()
        {
            // check whether voice file id is set
            if (_transactionSaver.getVoiceFileID().Length == 0)
            {
                //  voice file is is not set yet
                //  trigger escalation
                MailSender.Instance.send("VRC ERROR: unable to set infoFlag; VoiceFileID is not set yet: " + _cmdObject.ReqString);
                ERROR("LDCClient.setInfoFlag: unable to set infoFlag; VoiceFileID is not set yet :" + _cmdObject.ReqString);
            }

            try
            {
                // get parameters from command object
                SIFCmdPars sifPars = (SIFCmdPars)_cmdObject.Pars;

                sendCommand(LDCCmd.TRANSACTION_INFO_FLAG_SET_REQUEST, "transaction_id=" + TrafficId + (char)5
                        + "info_flag=" + sifPars.value);

            }
            catch (TooManyRetryTimesException)
            {
                // trigger eskalation 
                MailSender.Instance.send("VRC ERROR", "One error happens by setInfoFlag");
                ERROR("LDCClient.setInfoFlag: The operation is given up while it has failed for too many times");
            }
            catch (NoTransactionException)
            {
                ERROR("LDCClient.setInfoFlag: there's no transaction available");
            }
            catch (ReceiveDenyException)
            {
                ERROR("LDCClient.setInfoFlag: the command is denied by LDC-Server");
            }

        }



        /// <summary>
        /// Query the VoiceFile ID
        /// </summary>
        private void getVoiceFileID()
        {
            // check whether the voice file ID is already set
            if (_transactionSaver.getVoiceFileID().Length == 0)
            {
                //  trigger escalation
                MailSender.Instance.send("VRC ERROR: Fail to get voice file id: " + _cmdObject.ReqString);
                ERROR("LDCClient.saveRecordingData: Fail to get voice file id :" + _cmdObject.ReqString);
            }


            // keep the voice File Id 
            voiceFileId = _transactionSaver.getVoiceFileID();

            // the voice file id is obtained from _transactionSaver
            // code stays in reportSuccess(vrc.VRCPCmd);
        }

        /// <summary>
        /// Save the user's data
        /// </summary>
        private void saveRecordingData()
        {
            // check whether voice file id is set
            if (_transactionSaver.getVoiceFileID().Length == 0)
            {
                //  voice file is is not set yet
                //  trigger escalation
                MailSender.Instance.send("VRC ERROR: unable to save recording data; VoiceFileID is not set yet: " + _cmdObject.ReqString);
                ERROR("LDCClient.saveRecordingData: unable to save recording data; VoiceFileID is not set yet :" + _cmdObject.ReqString);
            }

            // get parameters from command object
            SRDCmdPars srdPars = (SRDCmdPars)_cmdObject.Pars;

            // set VR system
            _transactionSaver.setVRSystem(this.VrSystem);
            // set CRM system
            _transactionSaver.setCRMSystem(this.CrmSystem);
            // set project
            _transactionSaver.setProject(srdPars.project);
            // set campaign
            _transactionSaver.setCampaign(srdPars.campaign);
            // set data1 - data5
            _transactionSaver.setData1(srdPars.data1);
            _transactionSaver.setData2(srdPars.data2);
            _transactionSaver.setData3(srdPars.data3);
            _transactionSaver.setData4(srdPars.data4);
            _transactionSaver.setData5(srdPars.data5);

            // save the data
            if (!_transactionSaver.saveIntoVRDatabase())
            {
                ERROR("LDCClient.saveRecordingData: Saving into VR DB failed");
            };
        }


        /// <summary>
        /// Save the reference of user's data in the CRM-System
        /// </summary>
        private void saveRecordingReference()
        {
            // check whether voice file id is set
            if (_transactionSaver.getVoiceFileID().Length == 0)
            {
                //  voice file is is not set yet
                //  trigger escalation
                MailSender.Instance.send("VRC ERROR: unable to save recording reference; VoiceFileID is not set yet: " + _cmdObject.ReqString);
                ERROR("LDCClient.saveRecordingReference: unable to save recording reference; VoiceFileID is not set yet :" + _cmdObject.ReqString);
            }

            // get parameters from command object
            SRRCmdPars srdPars = (SRRCmdPars)_cmdObject.Pars;
            // set VR system
            _transactionSaver.setVRSystem(this.VrSystem);
            // set CRM system
            _transactionSaver.setCRMSystem(this.CrmSystem);
            // set project
            _transactionSaver.setProject(srdPars.project);
            // set campaign
            _transactionSaver.setCampaign(srdPars.campaign);
            // set reference data of CRM System
            _transactionSaver.setDBServerOfCRMSystem(srdPars.DBServer);
            _transactionSaver.setRecordIDinCRMSystem(srdPars.recordID);

            // save the data
            if (!_transactionSaver.saveIntoVRDatabase())
            {
                ERROR("LDCClient.saveRecordingReference: Saving into VR DB failed");
            };

        }



        /// <summary>
        /// Called by the ConnectionGuard when the connection to LDC-Server is lost
        /// </summary>
        public void lostConnectionEvent(ServerEntry ldcServer)
        {
            LogWriter.info(ldcClientIPAddress + " LDCClient.lostConnectionEvent(): the connection to the server is lost");

            if (ldcServer == null)
            {
                LogWriter.error("LDCClient.lostConnectionEvent: the parameter 'ldcServer' can't be null");

            }

            // set the curAllowedServer as null, so that the command needn't be sent to it
            if (ldcServer.Equals(curAllowedServer))
            {
                curAllowedServer = null;
            }

        }

        /// <summary>
        /// Reconnect to the LDC-Server
        /// </summary>
        public void reestablishConnection(ServerEntry ldcServer)
        {
            // login on server 
            if (loginOneServer(ldcServer))
            {
                LogWriter.info(" LDCClient.reestablishConnection(): {0} has (re)logged in to {1} ", this.mStationID, ldcServer);
            }
            else
            {
                ConnectionGuard.Instance.unRegisterObserver(this, ldcServer);
            }
        }

        #region help methods

        /// <summary>
        /// construct the fail message based on the VRCPCmd type
        /// </summary>
        /// <param name="cmd">VRCPCmd</param>
        /// <returns>fail message</returns>
        private String reportFail(VRCPCmd cmd)
        {

            String reply = cmd.ToString("G") + "_FAILURE";

            // append uid in the reply (except USER_LOGIN)
            if (cmd != VRCPCmd.USER_LOGIN)
            {
                reply += ";" + this.mUniqueID;
            }

            // append '\n' (according to VRCP) 
            return reply + '\n';
        }

        /// <summary>
        /// Construct the successful message based on the VRCPCmd type
        /// </summary>
        /// <param name="cmd">VRCPCmd</param>
        /// <returns>sucessful message</returns>
        private String reportSuccess(VRCPCmd cmd)
        {
            // construct the reply message
            String reply = cmd.ToString("G") + "_SUCCESS;" + this.mUniqueID;

            // append voice file id for the reply of GET_VOICEFILE_ID
            if (cmd == VRCPCmd.GET_VOICEFILE_ID)
            {
                reply += ";" + voiceFileId;
                voiceFileId = null;
            }

            // append '\n' (according to VRCP) 
            return reply + '\n';

        }

        /// <summary>
        /// Send a command to LDC-Server 
        /// If error happens (timeout), the process will be repeated for predefined times
        /// </summary>
        /// <param name="req_categorieNumber">the requested command category</param>
        /// <param name="req_message">the request's message</param>
        /// <exception cref="VRCException.StopIndicationFailException"> thrown when stop indication failed</exception>
        private void
            sendCommand(LDCCmd req_categorieNumber, string req_message)
        {

            // the command could only be processed during tansaction (except setInfoFlag command)
            if (req_categorieNumber != LDCCmd.TRANSACTION_INFO_FLAG_SET_REQUEST)
            {
                if (this.isTranEnd())
                {
                    throw new NoTransactionException();
                }
            }


            // copy the curAllowedServer, in fear it be changed during the method
            ServerEntry ldcServer = curAllowedServer;

            if (ldcServer == null)
            {
                // ldcServer is not set yet (no transaction has begun)
                LogWriter.debug("LDCClient.sendCommand: the curAllowedServer is null (no transaction has begun).");
                throw new NoTransactionException();
            }

            int tryTimes = 0;

            // repeat for given times when server is reachable 
            for (tryTimes = 0; tryTimes <= Configuration.commandIteration; tryTimes++)
            {
                lock (sync)
                {

                    ConnectionGuard.Instance.addToCmdQueue(req_categorieNumber, req_message, ldcServer);

                    int index = WaitHandle.WaitAny(new WaitHandle[] { posRepEvent, negRepEvent }, Configuration.ldcTimout, false);
                    if (index == WaitHandle.WaitTimeout)
                    {
                        LogWriter.info("LDCClient.sendCommand: Timeout for receiving reply message");
                    }
                    else if (index == 0)
                    { // posRepEvent is set
                        LogWriter.info("LDCClient.sendCommand: Receive  Acknowledgement");
                        break;
                    }
                    else
                    { // negRepEvent is set
                        LogWriter.error("LDCClient.sendCommand: Receive Deny");
                        throw new ReceiveDenyException();
                    }

                    // get negative answer from LDC Server
                    // wait some time before next try 
                    Thread.Sleep(Configuration.holdingTime);

                    LogWriter.debug(ldcClientIPAddress + " LDCClient.sendCommand: Try again...");


                }


                // try next time ; resend the command
            } //end for

            if (tryTimes > Configuration.commandIteration)
            {
                // Retry exceeds limits (keep receiving negative responses)
                throw new TooManyRetryTimesException();
            }

        }


        /// <summary>
        /// help method to throw the exeption of error while processing command
        /// </summary>
        /// <param name="errorMsg">if errorMsg exists (not null, Length > 0 ), write the errorMsg into log</param>
        private void ERROR(String errorMsg)
        {
            if (errorMsg != null && errorMsg.Length > 0)
            {
                LogWriter.error(errorMsg);
            }
            throw new VRCApplicationException(errorMsg);
        }



        /// <summary>
        /// Close Connection with LDC-Server 
        ///(Count down connection number when necessary)
        /// </summary>
        public void Dispose(Boolean disposeSession)
        {
            LogWriter.debug("LDCClient.Dispose: Ldcclient is disposed now");

            // first synchronize. avoid dispose during sending command
            lock (sync)
            {

                // each LDCClient instance needs/can be disposed only once
                if (!disposed)
                {

                    // nullify transactionsaver
                    _transactionSaver = null;

                    // disconnect session
                    if (this.isBounded())
                    {
                        CurrentSession.unBound();

                        if (disposeSession)
                        {
                            CurrentSession.Dispose();
                            CurrentSession = null;
                        }
                    }

                    // remove from PoolOptimizer
                    PoolManager.Instance.RemoveObject(this);

                    // remove from register list
                    ConnectionGuard.Instance.removeObserver(this);

                    disposed = true;
                }
            }
        }

        public bool isDisposed()
        {
            return disposed;
        }


        public override void Dispose()
        {
            Dispose(true);
        }


        /// <summary>
        /// This is a wrapper method which deals with Login only
        /// The purpose is to keep a flag to notify whether login succeeds.
        /// The connection to the Client will be closed when the fLoginSuccess
        ///  is set to false
        /// </summary>
        /// <param name="cmdObj">command object for login </param>
        /// <param name="fLoginSuccess">mark whether loginsucess</param>
        /// <returns>the reply string to client</returns>
        internal string processLogin(CommandObject cmdObj, out bool fLoginSuccess)
        {
            VRCPCmd cmd = cmdObj.Cmd;

            if (cmd != VRCPCmd.USER_LOGIN)
            {
                throw new Exception("LDCCLient.processLogin: the passed command is not login " + cmdObj);
            }

            String reply = processCommand(cmdObj);

            if (reply.Contains("SUCCESS"))
            {
                fLoginSuccess = true;
            }
            else
            {
                fLoginSuccess = false;
            }

            return reply;

        }


        /// <summary>
        /// determine whether the LDCClient is bound to a client
        ///
        /// </summary>
        public Boolean isBounded()
        {
            return (this.currentSession != null);
        }

        /// <summary>
        /// Add the reference to the  bounded session object
        /// It helps the Pool Optimizer to select idle LDCClient
        /// Only LDCClient with no bounded session(session == null)
        /// can be selected
        /// </summary>
        /// <param name="session">the session instance</param>
        internal void setCurrentSession(Session session)
        {
            this.currentSession = session;
        }



        internal void onReceive(string rep_head, string rep_content, CmdCommunication cmdComm)
        {
            LDCCmd cmdReply = HelperTools.parseCategory(rep_head);

            switch (cmdReply)
            {
                case LDCCmd.ASSIGN_AGENT_TO_STATION_REPLY:
                    ConnectionGuard.Instance.addToCmdQueue(LDCCmd.ASSIGN_WORKING_GROUP_TO_AGENT, "agent_id=" + this.AgentID + (char)5
                             + "group_name=" + this.WorkingGroup + (char)5 + "assign_to_group=1", new ServerEntry(cmdComm.hostname, cmdComm.port));
                    break;


                case LDCCmd.ASSIGN_WORKING_GROUP_TO_AGENT_REPLY:
                case LDCCmd.RECORDING_CONTROL_REPLY:
                case LDCCmd.TRANSACTION_INFO_FLAG_SET_REPLY:
                    posRepEvent.Set();
                    break;

                case LDCCmd.ASSIGN_AGENT_TO_STATION_DENIED:
                case LDCCmd.ASSGIN_WORKING_GROUP_TO_AGENT_DENIED:
                case LDCCmd.RECORDING_CONTROL_DENIED:
                case LDCCmd.TRANSACTION_INFO_FLAG_SET_DENIED:
                    negRepEvent.Set();
                    break;

                default:
                    LogWriter.error("LDCClient.onReceive: Receive unknown command from {0}: {1}", new ServerEntry(cmdComm.hostname, cmdComm.port).ToString(), rep_head + "\n" + rep_content);
                    break;


            }

        }

    }

        #endregion
        #endregion

}
