using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace vrc
{

    /// <summary>
    /// class representing all the available LDCC messages
    /// </summary>
    static class LDCCMsg
    {
        public enum MsgType
        {
            PBX_INFORMATION,
            CALL_INITIATED,
            CALL_ALERTING,
            CALL_INFORMATION,
            CALL_CONNECTED,
            CALL_DISCONNECTED,
            CALL_RELEASED,
            UNKNOWN, // for error handling

        }

        private static string[] msgs = new string[]{
            "PBX_INFORMATION",
            "CALL_INITIATED",
            "CALL_ALERTING",
            "CALL_INFORMATION",
            "CALL_CONNECTED",
            "CALL_DISCONNECTED",
            "CALL_RELEASED",
        };


        public static MsgType parseMsgType(string msg_content)
        {
           for (int i=0; i < msgs.Length; i++){
            if ( msg_content.Contains(msgs[i])){
                return (MsgType)Enum.Parse(typeof(MsgType), msgs[i]);         
            }
           }

            // no msg type can be found
           LogWriter.error("LDCCMsg.parseMsgType: unknown msg type:" + msg_content);
           return MsgType.UNKNOWN;
        }
    }
}
