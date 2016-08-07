using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using CodeBureau;
using System.Data.SqlClient;

namespace vrc
{
    static class HelperTools
    {
        
        public static void logRespMsg(string msg, out string rep_category, out string rep_content, Communication comm)
        {
            StreamReader sr = new StreamReader(str2stream(msg));

            // use blocking method for receiving answer
            string rep_head = sr.ReadLine();
            rep_content = sr.ReadLine();

            // extract the substring of category from reply head
            Regex exp = new Regex(@"^cw\sdata\s#0\s(#\w+)\s+.*");
            Match matchRepCategory = exp.Match(rep_head);
            if (matchRepCategory.Success)
            {
                Group g = matchRepCategory.Groups[1];
                CaptureCollection cc = g.Captures;
                Capture repCategory = cc[0];
                rep_category = repCategory.ToString();

                LogWriter.debug(comm.GetType().Name + ".receiveMessage" + "(" + comm.hostname + ":" + comm.port + ") response=" + rep_head + " (" + StringEnum.Parse(typeof(LDCCmd), rep_category) + ")" + rep_content);
            }
            else
            {
                rep_category = "";
                if (! msg.Contains(" db on")){
                    LogWriter.error(comm.GetType().Name + ".receiveMessage:" + "(" + comm.hostname + ":" + comm.port + ") Unknown response=" + msg);
                }
            }
        }

        public static void logReqMsg(string msg, out string req_category, out string req_content, Communication comm)
        {
            StreamReader sr = new StreamReader(str2stream(msg));


            // use blocking method for receiving answer
            string req_head = sr.ReadLine();
            req_content = sr.ReadLine();

            // extract the substring of category from reply head
            Regex exp = new Regex(@"^cw\sdata\s#0\s(#\w+)\s+.*");
            Match matchReqCategory = exp.Match(req_head);
            if (matchReqCategory.Success)
            {
                Group g = matchReqCategory.Groups[1];
                CaptureCollection cc = g.Captures;
                Capture reqCategory = cc[0];
                req_category = reqCategory.ToString();

                LogWriter.debug(comm.GetType().Name + ".sendMessage" + "(" + comm.hostname + ":" + comm.port + ") request=" + req_head + " (" + StringEnum.Parse(typeof(LDCCmd), req_category) + ")" + req_content);

            }
            else
            {
                req_category = "";
                LogWriter.error(comm.GetType().Name + ".sendMessage:" + "(" + comm.hostname + ":" + comm.port + ") Unknown request=" + msg);
            }

        }


        /// <summary>
        /// convert the ASCII encoded string into a memory stream  
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Stream str2stream(string str)
        {
            MemoryStream memStream = new MemoryStream();
            byte[] data = Encoding.ASCII.GetBytes(str);
            memStream.Write(data, 0, data.Length);
            memStream.Position = 0;

            return memStream;
        }

        /// <summary>
        /// parse the repsonse msg and extract the station id
        /// </summary>
        /// <param name="rep_content"></param>
        /// <returns></returns>
        internal static int parseStationId(string rep_head, string rep_content)
        {
            LDCCmd ldcCmd = parseCategory(rep_head);
            int stationId = -1;
            try
            {
                if (ldcCmd == LDCCmd.ASSIGN_AGENT_TO_STATION_REPLY || ldcCmd == LDCCmd.ASSIGN_AGENT_TO_STATION_DENIED)
                {
                    string sub = "station_id=";
                    int startIndex = rep_content.IndexOf(sub) + sub.Length;
                    stationId = int.Parse(rep_content.Substring(startIndex));

                }
                else if (ldcCmd == LDCCmd.RECORDING_CONTROL_REPLY || ldcCmd == LDCCmd.RECORDING_CONTROL_DENIED )
                {
                    Regex exp = new Regex(@"proc_id=([0-9]+)");
                    Match matchStationId = exp.Match(rep_content);
                    if (matchStationId.Success)
                    {
                        Group g = matchStationId.Groups[1];
                        CaptureCollection cc = g.Captures;
                        Capture cap = cc[0];
                        stationId = int.Parse(cap.ToString());
                    }

                }
                else if (ldcCmd == LDCCmd.MONITOR_LDCC_INFO_INDICATION)
                {
                    Regex exp = new Regex(@"station_id=([0-9]+)");
                    Match matchStationId = exp.Match(rep_content);
                    if (matchStationId.Success)
                    {
                        Group g = matchStationId.Groups[1];
                        CaptureCollection cc = g.Captures;
                        Capture cap = cc[0];
                        stationId = int.Parse(cap.ToString());
                    }
                }
            }
            catch(Exception e)
            {
                LogWriter.error("HelperTools.parseStationId: error parsing the station_id " + e.Message);
            }


            return stationId;
        }


        /// <summary>
        /// parse the repsonse msg and extract the station id
        /// </summary>
        /// <param name="rep_content"></param>
        /// <returns></returns>
        internal static int parseAgentId(string rep_head, string rep_content)
        {
            LDCCmd ldcCmd = parseCategory(rep_head);
            int agentId = -1;
            try
            {
                if (ldcCmd == LDCCmd.ASSIGN_WORKING_GROUP_TO_AGENT_REPLY || ldcCmd == LDCCmd.ASSGIN_WORKING_GROUP_TO_AGENT_DENIED)
                {
                    Regex exp = new Regex(@"agent_id=([0-9]+)");
                    Match matchAgentId = exp.Match(rep_content);
                    if (matchAgentId.Success)
                    {
                        Group g = matchAgentId.Groups[1];
                        CaptureCollection cc = g.Captures;
                        Capture cap = cc[0];
                        agentId = int.Parse(cap.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                LogWriter.error("HelperTools.parseStationId: error parsing the agent_id" + e.Message);
            }

            return agentId;
        }


        /// <summary>
        /// parse the traffic id from the message
        /// </summary>
        /// <param name="rep_content"></param>
        /// <returns></returns>
        internal static string parseTrafficId(string rep_head,string rep_content)
        {
            LDCCmd ldcCmd = parseCategory(rep_head);
            string trafficId = "";

            if (ldcCmd == LDCCmd.MONITOR_LDCC_INFO_INDICATION)
            {
                string tid_keyword_from = "TransactionID={";
                string tid_keyword_to = "}";
               

                try
                {
                    int position1 = rep_content.IndexOf(tid_keyword_from) + tid_keyword_from.Length - 1;
                    int position2 = rep_content.IndexOf(tid_keyword_to, position1) + 1;
                    int length = position2 - position1;
                    trafficId = rep_content.Substring(position1, length);
                }
                catch (Exception e)
                {
                    LogWriter.error("HelperTools.parseTrafficId: error parsing the transaction-id" + e.Message);
                }
            }
            else if (ldcCmd == LDCCmd.TRANSACTION_INFO_FLAG_SET_REPLY ||
                     ldcCmd == LDCCmd.TRANSACTION_INFO_FLAG_SET_DENIED)
            {

                string tid_keyword_from = "transaction_id={";
                string tid_keyword_to = "}";


                try
                {
                    int position1 = rep_content.IndexOf(tid_keyword_from) + tid_keyword_from.Length - 1;
                    int position2 = rep_content.IndexOf(tid_keyword_to, position1) + 1;
                    int length = position2 - position1;
                    trafficId = rep_content.Substring(position1, length);
                }
                catch (Exception e)
                {
                    LogWriter.error("HelperTools.parseTrafficId: error parsing the transaction-id" + e.Message);
                }

            }
            else if (ldcCmd == LDCCmd.CAT_LDC_RECORDING_INSERTED)
            {
                trafficId = rep_content;
            }

            return trafficId;
        }


        /// <summary>
        /// get LDCCmd Category from its string representing 
        /// </summary>
        /// <param name="rep_head"></param>
        /// <returns></returns>
        internal static LDCCmd parseCategory(string rep_category)
        {
            LDCCmd ldcCmd = LDCCmd.UNKNOWN;

            if (rep_category.Length > 0)
            {

                try
                {
                    string cmdString = StringEnum.Parse(typeof(LDCCmd), rep_category).ToString();
                    ldcCmd = (LDCCmd)Enum.Parse(typeof(LDCCmd), cmdString);
                }
                catch (Exception e)
                {
                    LogWriter.error("HelperTools.parseCategory: error parsing the message category " + e.Message);
                }

            }

            return ldcCmd;
        }

        internal static string makeCmdStr(LDCCmd cat_nr, string msg)
        {
            return "cw data #0 " +  StringEnum.GetStringValue(cat_nr) + " " + msg.Length.ToString() + "\n" + msg + "\n";
        }

    }
}
