using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace vrc
{
    public class LDCServerComm : ICloneable
    {
        public String ldcServer; // the host name of the ldcServer
        public int port; // the port of the ldcServer
        public Communication pingpongComm; // the communication object for pingpong
        public CmdCommunication cmdComm; // the communication object for sending/receiving commands
        public MonCommunication monitoringComm; // the communication object for monitoring task

        #region ICloneable Members

        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion
    }
}
