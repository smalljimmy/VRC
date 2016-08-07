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
    /// Class for communication with LDC-Server for monitoring
    /// </summary>
    public class MonCommunication : Communication
    {

        #region members
        public new enum STAGE { START, GET_DBSTRING, LOGIN, MON_LDCC_INFO, MON_FILE_AVAILABLE, MON_MONITORING, END }; // possible stages by communicating with LDC-Server    
        public new STAGE nextStage;
        #endregion


        #region methods

        public MonCommunication()
        {
        }

        public MonCommunication(string _hostName, int _port)
            : base(_hostName, _port)
        {

        }

        public override bool initCommunication()
        {
            if (base.initCommunication())
            {
                this.OnReceive = new OnReceiveDelegate(this.handleMonReply);
                this.OnDisconnect = new OnDisconnectDelegate(this.stopMonitoring);

                this.nextStage = MonCommunication.STAGE.MON_LDCC_INFO;
                this.clearMsgQueue();
                this.sendMessage(LDCCmd.MONITOR_LDCC_INFO_REQUEST, "LineID=" + (char)5 + "start=1");

                // wait till indicaiton and file monitoring are comepletely started
                int index = WaitHandle.WaitAny(new WaitHandle[] { requestFailedEvent, requestSucceedEvent }, Configuration.ldcTimout * 6, false);
                if (index == 0 || index == WaitHandle.WaitTimeout)
                {
                    this.Disconnect();
                    OnReceive = null;
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }


        protected void stopMonitoring(AsynchSocketManager socketMgr)
        {
            LogWriter.debug("{0}.stopMonitoring: try to stop monitoring on {1}", this.GetType().Name, new ServerEntry(socketMgr.hostname, socketMgr.port));

            // stop monitoring processes on LDCServer
            MonCommunication comm = (MonCommunication)socketMgr;
            comm.sendMessage(LDCCmd.MONITOR_LDCC_INFO_REQUEST, "LineID=" + (char)5 + "start=0");


            // wait till indicaiton and file monitoring are comepletely stoped
            int index = WaitHandle.WaitAny(new WaitHandle[] { requestFailedEvent, requestSucceedEvent }, Configuration.ldcTimout * 6, false);
            if (index == 0 || index == WaitHandle.WaitTimeout)
            {
                LogWriter.debug("{0}.stopMonitoring: fail to stop monitoring on {1}", this.GetType().Name, new ServerEntry(socketMgr.hostname, socketMgr.port));

            }
            else
            {
                LogWriter.debug("{0}.stopMonitoring: succeed to stop monitoring on {1}", this.GetType().Name, new ServerEntry(socketMgr.hostname, socketMgr.port));

            }
        }


        protected void handleMonReply(AsynchSocketManager socketMgr, string msg)
        {
            // avoid procesing when the socket is disconnected
            if (!IsConnected)
            {
                return;
            }

            MonCommunication comm = (MonCommunication)socketMgr;
            string rep_head;
            string rep_content;
            HelperTools.logRespMsg(msg, out rep_head, out rep_content, comm);

            try
            {

                if (HelperTools.parseCategory(rep_head) == LDCCmd.MONITOR_LDCC_INFO_REPLY)
                {

                    //receive reply from ldcserver;
                    if (rep_content.Contains("start=1"))
                    {
                        // indication is succesfully STARTED
                        LogWriter.info("Monitoring: succeed to start MONITOR_LDCC_INFO monitoring  on LDC-Server {0}", new ServerEntry(hostname, port));

                        // start FILE_AVAILABLE_INFO
                        comm.sendMessage(LDCCmd.MONITOR_FILE_AVAILABLE_REQUEST, "LineID=" + (char)5 + "start=1");

                    }
                    else if (rep_content.Contains("start=0"))
                    {
                        // indication is succesfully STOPED
                        LogWriter.info("Monitoring: succeed to stop MONITOR_LDCC_INFO monitoring  on LDC-Server {0}", new ServerEntry(hostname, port));

                        // stop FILE_AVAILABLE_INFO
                        comm.sendMessage(LDCCmd.MONITOR_FILE_AVAILABLE_REQUEST, "LineID=" + (char)5 + "start=0");
                    }


                }
                else if (HelperTools.parseCategory(rep_head) == LDCCmd.MONITOR_FILE_AVAILABLE_INFO_REPLY)
                {
                    //receive reply from ldcserver;
                    if (rep_content.Contains("start=1"))
                    {
                        // file monitoring is succesfully STARTED
                        LogWriter.info("Monitoring: succeed to start FILE_AVAILABLE_INFO monitoring  on LDC-Server {0}", new ServerEntry(hostname, port));
                    }
                    else if (rep_content.Contains("start=0"))
                    {
                        // file monitoring is succesfully STOPED
                        LogWriter.info("Monitoring: succeed to stop FILE_AVAILABLE_INFO monitoring  on LDC-Server {0}", new ServerEntry(hostname, port));
                    }


                    requestSucceedEvent.Set();
                }
                else if (HelperTools.parseCategory(rep_head) == LDCCmd.MONITOR_LDCC_INFO_INDICATION)
                {
                    // make choice depending on the message type
                    LDCCMsg.MsgType msgType = LDCCMsg.parseMsgType(rep_content);
                    ServerEntry ldcServerEntry = new ServerEntry(comm.hostname, comm.port);

                    switch (msgType)
                    {
                        case LDCCMsg.MsgType.PBX_INFORMATION:
                            // notify Monitoring that a new call is established
                            Monitoring.notifyCallConnected(ldcServerEntry);
                            goto case LDCCMsg.MsgType.CALL_INITIATED;
                        case LDCCMsg.MsgType.CALL_INITIATED:
                        case LDCCMsg.MsgType.CALL_CONNECTED:
                        case LDCCMsg.MsgType.CALL_INFORMATION:
                        case LDCCMsg.MsgType.CALL_ALERTING:
                            // update the current allowed server for associated ldcc
                            markTranBegin(comm, rep_head, rep_content);
                            break;

                        case LDCCMsg.MsgType.CALL_DISCONNECTED:

                            // notify Monitoring the call is ended
                            Monitoring.notifyCallDisconnected(ldcServerEntry);
                            goto case LDCCMsg.MsgType.CALL_RELEASED;
                        case LDCCMsg.MsgType.CALL_RELEASED:

                            // nullify the current alloed server for associated ldcc
                            markTranEnd(comm, rep_head, rep_content);
                            break;

                        default:
                            LogWriter.error("Monitoring: the indication message type is unknown: " + rep_content);
                            break;
                    }



                }
                else if (HelperTools.parseCategory(rep_head) == LDCCmd.CAT_LDC_RECORDING_INSERTED)
                {
                    string trafficId = HelperTools.parseTrafficId(rep_head, rep_content);
                    LDCClient ldcc = PoolManager.Instance.findLDCClientWithTrafficId(trafficId);


                    if (ldcc != null)
                    {
                        // set the RecordingAvailable flag
                        ldcc.RecordingAvailable = true;
                    }
                    else
                    {
                        LogWriter.warn("ConnectionGuard.CAT_LDC_RECORDING_INSERTED: there's no ldcclient associated with the transaction any more (trafficId = " + trafficId + ")");
                    }

                    runUpdateProc(trafficId);

                }
                else
                {
                    LogWriter.error("ConnectionGuard.MON_MONITORING: receive wrong message on LDC-Server" +  new ServerEntry(hostname, port) + "(monitoring message expected) " + rep_head + "\n" + rep_content + "\n. The Monitoing Process will be restarted");
                    MailSender.Instance.send("VRC Error: receive wrong message on LDC-Server" +  new ServerEntry(hostname, port) + "(monitoring message is expected)" + rep_head + "\n" + rep_content + "\n. The Monitoing Process will be restarted");

                    // restart Monitoring
                    LogWriter.info("ConnectionGuard.MON_MONITORING: restart monitoring on LDCServer now");
                    this.Disconnect();
                    if (this.initCommunication())
                    {
                        LogWriter.info("ConnectionGuard.MON_MONITORING: monitoring on LDC-Server {0} is restarted successfully", new ServerEntry(hostname, port));
                    }
                    else
                    {
                        LogWriter.error("ConnectionGuard.MON_MONITORING: fail to restart monitoring on LDC-Server {0}", new ServerEntry(hostname, port));
                    };

                    return;

                }
            }
            catch (Exception e)
            {
                LogWriter.error("ConnectionGuard.MON_MONITORING: Exception happens during Monitoring on LDC-Server {0}: {1} The Monitoring process will be restarted", new ServerEntry(hostname, port), e);

                // restart Monitoring
                LogWriter.info("ConnectionGuard.MON_MONITORING: restart monitoring on LDC-Server {0} now", new ServerEntry(hostname,port));

                this.Disconnect();
                if (this.initCommunication())
                {
                    LogWriter.info("ConnectionGuard.MON_MONITORING: monitoring on LDC-Server {0} is restarted successfully", new ServerEntry(hostname,port));
                }
                else
                {
                    LogWriter.error("ConnectionGuard.MON_MONITORING: fail to restart monitoring on LDC-Server {0}", new ServerEntry(hostname,port));
                };
            }


        }


        /// <summary>
        /// mark begin of a new transaction
        /// 1.set new traffic id
        /// 2.update the current allowed ldcserver for the correspondent ldcclient 
        /// </summary>
        private void markTranBegin(MonCommunication comm, string rep_head, string rep_content)
        {

            // get ldcclient from station id
            int stationId = HelperTools.parseStationId(rep_head, rep_content);
            string trafficId = HelperTools.parseTrafficId(rep_head, rep_content);

            if (stationId != -1)
            {
                LDCClient ldcc = PoolManager.Instance.findLDCClientWithStationId(stationId);

                if (ldcc != null)
                {
                    // set the msg source as ldcc's current allowed server
                    ServerEntry curLdcServer = new ServerEntry(comm.hostname, comm.port);

                    if (ldcc.isTranEnd()) // first indication message for a new traffic
                    {

                        ldcc.setCurAllowedLDCServer(curLdcServer);

                        // clear tran-end flag
                        ldcc.setTranEnd(false);

                        // set the traffic id
                        ldcc.TrafficId = trafficId;

                        // update the last traffic time
                        ldcc.LastTrafficTime = DateTime.Now;

                        LogWriter.debug("ConnectionGuard.MON_MONITORING: a new transaction {0} is started for station {1} on {2}", trafficId, ldcc.StationID, curLdcServer);

                    }
                    else if (!ldcc.TrafficId.Equals(trafficId)) // indication message for a concurrent traffic
                    {
                        // concurrent call exists
                        if (DateTime.Now.Subtract(ldcc.LastTrafficTime).TotalMilliseconds > Configuration.trafficDuration) // check whether time out happens
                        {
                            LogWriter.warn("ConnectionGuard.MON_MONITORING: the old transaction {0} is replaced with the new one {1} while it has been idle too long for station {2} on {3}", ldcc.TrafficId, trafficId, ldcc.StationID, curLdcServer);

                            // replace the  old traffic
                            ldcc.setCurAllowedLDCServer(curLdcServer);
                            ldcc.TrafficId = trafficId;

                            // update the last traffic time
                            ldcc.LastTrafficTime = DateTime.Now;

                        }
                        else
                        {
                            // ignore the msg
                            LogWriter.warn("ConnectionGuard.MON_MONITORING: the new transaction {0} begins while a concurrent one {1} has not been ended yet for station {2} on {3}", trafficId, ldcc.TrafficId, ldcc.StationID, curLdcServer);

                        }


                    }
                    else // new indication message for the current traffic
                    {
                        // update the last traffic time
                        ldcc.LastTrafficTime = DateTime.Now;
                    }

                }

            }
            else
            {
                // no ldcc corresponds to the station-id
                LogWriter.warn("ConnectionGuard.MON_MONITORING: there's no ldcclient associated with the transaction {0} ", trafficId);

            }
        }


        /// <summary>
        /// mark end of the transaction
        /// </summary>
        private void markTranEnd(MonCommunication comm, string rep_head, string rep_content)
        {
            string trafficId = HelperTools.parseTrafficId(rep_head, rep_content);
            LDCClient ldcc = PoolManager.Instance.findLDCClientWithTrafficId(trafficId);

            if (ldcc != null && !ldcc.isTranEnd())
            {
                ServerEntry ldcServer = new ServerEntry(comm.hostname, comm.port);
                if (ldcServer.Equals(ldcc.getCurAllowedLDCServer()))
                {
                    // set flag for transaction end
                    ldcc.setTranEnd(true);
                    LogWriter.debug("ConnectionGuard.MON_MONITORING: the transaction {0}  for station {1} is ended on {2}", trafficId, ldcc.StationID, ldcServer);
                }
                else
                {
                    LogWriter.error("ConnectionGuard.MON_MONITORING: the current allowed server has been changed before End Call on station {0} ", ldcc.StationID);
                }
            }
            else
            {
                if (ldcc == null)
                {
                    LogWriter.warn("ConnectionGuard.MON_MONITORING: there's no ldcclient associated with the transaction {0} ", trafficId);
                }
                else
                {
                    LogWriter.warn("ConnectionGuard.MON_MONITORING: the transaction  {0} for station {1} has already been ended", trafficId, ldcc.StationID);
                }
            }
        }


        /// <summary>
        /// set the recording_inserted flag for the record with given VOICEFILE_ID in VRDb
        /// </summary>
        /// <param name="trafficId"></param>
        private void runUpdateProc(string trafficId)
        {
            SqlConnection conn = DBMgr.Instance.getVRCDBConn();
            if (conn != null)
            {
                try
                {
                    SqlCommand cmd = new SqlCommand(
                        "PROC_RECORDING_INSERTED", conn);

                    cmd.Parameters.Add(
                    new SqlParameter("@VOICEFILE_ID", trafficId));
                    cmd.CommandType = CommandType.StoredProcedure;

                    int affectedRow = cmd.ExecuteNonQuery();

                    if (affectedRow > 0)
                    {
                        LogWriter.debug("Update voice file {0}: the RECORDING_INSERTED flag is set", trafficId);
                    }
                }
                catch (Exception e)
                {
                    MailSender.Instance.send("VRC ERROR: Exception happens by updating VRDB. Details: " + e);
                    LogWriter.error("MonCommunication.runUpdateProc: Exception happens by updating VRDB. Details: " + e);
                    DBMgr.Instance.closeVRCDBConn();
                }


            }
        }


        #endregion

    }
}
