using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using vrc.Properties;
using System.Net.Sockets;

namespace vrc
{

    /// <summary>
    /// struct to hold server name and port
    /// </summary>
    public class ServerEntry
    {
        public string servername;
        public int port;

        public ServerEntry(string _hostname, int _port)
        {

            servername = _hostname;
            port = _port;
        }

        public ServerEntry(string fullName)
        {
            char[] sep2 = { ':' };
            String[] serverComponents = fullName.Split(sep2);

            try
            {
                IPHostEntry serverHostEntry = Dns.GetHostEntry(serverComponents[0].Trim());
                servername = serverComponents[0].Trim();
            }
            catch (SocketException se)
            {
                try
                {
                    // DNS unavailable, use IP address instead
                    IPAddress ipAddress = IPAddress.Parse(serverComponents[0].Trim());
                    servername = serverComponents[0].Trim();
                }
                catch (FormatException)
                {
                    // not IP Adress
                    LogWriter.error("ServerEntry: Can't parse the server :" + fullName);
                    servername = null;
                    return;
                }

            }

            if (serverComponents.Length == 2)
            {
                //servername = Dns.GetHostEntry(serverComponents[0].Trim()).HostName.ToLower().Trim();
                port = int.Parse(serverComponents[1].Trim());
            }

            else
            {
                port = new Settings().ldcServerPort;
            }
        }

        public override int GetHashCode()
        { return servername.GetHashCode() + port.GetHashCode(); }


        public override bool Equals(object other)
        {
            if (other == null)
            {
                return false;
            }

            return this.servername.Equals(((ServerEntry)other).servername) &&
                this.port == ((ServerEntry)other).port;
        }


        public override string ToString()
        {
            return servername + ":" + port;
        }
    }

}
