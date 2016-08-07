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
    /// Class for communication with LDC-Server for sending and receiving commands
    /// </summary>
    public class CmdCommunication : Communication
    {


        #region methods

        public CmdCommunication()
        {
        }

        public CmdCommunication(string _hostName, int _port)
            : base(_hostName, _port)
        {

        }

        public override bool initCommunication()
        {
            if (base.initCommunication())
            {
                this.OnReceive = new OnReceiveDelegate(this.handleCmdReply);
                return true;
            }

            return false;
        }




        /// <summary>
        /// while we use asynchronous socket, 
        /// this method is called each time when the reply message from LDC-Server arrives
        /// (only for command-socket)
        /// </summary>
        /// <param name="socketMgr"></param>
        /// <param name="msg"></param>
        public void handleCmdReply(AsynchSocketManager socketMgr, string msg)
        {
            CmdCommunication comm = (CmdCommunication)socketMgr;
            string rep_head;
            string rep_content;
            int stationId;
            int agentId;
            string trafficId;

            LDCClient ldcc;
            HelperTools.logRespMsg(msg, out rep_head, out rep_content, comm);

            LDCCmd cmdReply = HelperTools.parseCategory(rep_head);

            switch (cmdReply)
            {
                case LDCCmd.ASSIGN_AGENT_TO_STATION_REPLY:
                case LDCCmd.ASSIGN_AGENT_TO_STATION_DENIED:
                case LDCCmd.RECORDING_CONTROL_REPLY:
                case LDCCmd.RECORDING_CONTROL_DENIED:
                    stationId = HelperTools.parseStationId(rep_head, rep_content);
                    if (stationId != -1)
                    {
                        ldcc = PoolManager.Instance.findLDCClientWithStationId(stationId);

                        if (ldcc != null)
                        {
                            ldcc.onReceive(rep_head, rep_content,this);
                        }
                        else
                        {
                            LogWriter.debug("ConnectionGuard: the ldcclient instance does not exist any more (stationId = " + stationId + ")");
                        }

                    }
                    else
                    {
                        LogWriter.error("ConnectionGuard: the reply is invalid, missing station-id :" + msg);
                    }

                    break;

                case LDCCmd.TRANSACTION_INFO_FLAG_SET_REPLY:
                case LDCCmd.TRANSACTION_INFO_FLAG_SET_DENIED:
                    trafficId = HelperTools.parseTrafficId(rep_head, rep_content);
                    ldcc = PoolManager.Instance.findLDCClientWithTrafficId(trafficId);

                    if (ldcc != null)
                    {
                        ldcc.onReceive(rep_head, rep_content,this);
                    }
                    else
                    {
                        LogWriter.error("ConnectionGuard: there's no ldcclient associated with the transaction any more (trafficId = " + trafficId + ")");
                    }
                    break;

                case LDCCmd.ASSIGN_WORKING_GROUP_TO_AGENT_REPLY:
                case LDCCmd.ASSGIN_WORKING_GROUP_TO_AGENT_DENIED:
                    // use agent id to identify ldcclient
                    agentId = HelperTools.parseAgentId(rep_head, rep_content);

                    if (agentId != -1)
                    {
                        ldcc = PoolManager.Instance.findLDCClientWithAgentId(agentId);

                        if (ldcc != null)
                        {
                            ldcc.onReceive(rep_head, rep_content,this);
                        }
                        else
                        {
                            LogWriter.error("ConnectionGuard: the ldcclient instance does not exist any more (agent_id = " + agentId + ")");
                        }

                    }

                    break;

                default:
                    LogWriter.error("ConnectionGuard: unknown cmd");
                    break;


            }

        }

        #endregion

    }
}