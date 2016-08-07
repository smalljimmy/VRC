using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlobalObjects;
using System.Data;


namespace vrc
{

    /// <summary>
    /// The only class in VRC-Server for remoting communication
    /// Expose the important methods for remote configruation and monitoring.
    /// </summary>
    class RemotingService : MarshalByRefObject, IRemoteConfig,IRemoteMonitor 
    {
        /// <summary>
        /// Called by the web-service when it takes a new settings from client
        /// In consequence, the method will retrieve the new settings from web-service 
        /// </summary>
        #region IRemoteConfig Members

        public void UpdateConfiguration(DSVRCConfig ds)
        {
          Configuration.reloadConfiguration(ds);       
        }

        public DSVRCConfig GetConfiguration()
        {
            return Configuration.getConfiguration();

        }

        #endregion



        /// <summary>
        /// Called by web-service continuely with given interval 
        /// to retrieve online monitored values
        /// </summary>
        /// <returns></returns>
        #region IRemoteMonitor Members

        public DSVRCMonitoring GetMonVals()
        {
            // declare a dataset to hold the monitored values
            var monds = new DSVRCMonitoring();

            // fill the dataset with the variables in Monitoring instance  
            
            // add number of active calls for each LDC-Server
            var MonValDic = Monitoring.GetLDCServersWithActiveCalls();
            var retlist = new List<string>();
            foreach (var actvalkey in MonValDic.Keys)
            {
                var ldcsrow = monds.LDCServerConnections.NewLDCServerConnectionsRow();
                ldcsrow.LDCServerName = actvalkey.ToString();
                ldcsrow.NumberOfActiveCalls = MonValDic[actvalkey];
                monds.LDCServerConnections.AddLDCServerConnectionsRow(ldcsrow);

            }

            // add total number of connections with Client
            var clientsrow = monds.ClientConnections.NewClientConnectionsRow();
            clientsrow.NumberOfClientsConnected = Monitoring.ClientsConnected;
            monds.ClientConnections.AddClientConnectionsRow(clientsrow);
            return monds;

        }

        #endregion
    }
}
