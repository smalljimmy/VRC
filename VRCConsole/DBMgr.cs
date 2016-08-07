using System;
using System.Data.SqlClient;

namespace vrc
{
    /// <summary>
    /// the DB manager for VRC DB
    /// </summary>
    class DBMgr
    {
        private DBMgr()
        {
        }

        private static DBMgr instance = null;

        public static DBMgr Instance
        {
            get { if (instance == null) { instance = new DBMgr(); } return instance; }
        }

        private SqlConnection vrcDBConn;

        internal SqlConnection getVRCDBConn()
        {
            if (vrcDBConn != null)
            {
                return vrcDBConn;
            }
            
            String vrcdbConnStr = String.Empty;

            if (!String.IsNullOrEmpty(Configuration.dsn))
            {

                // when dsn is set, take it as connection string
                vrcdbConnStr = Configuration.dsn;

            }
            else
            {
                // check individual parameters
                if (String.IsNullOrEmpty(Configuration.vrcdbServer) ||
                    String.IsNullOrEmpty(Configuration.dbName))
                {
                    MailSender.Instance.send("VRC ERROR: The information about VRC's database is incomplete");
                    LogWriter.error("HelpterTool.getVRCDBConn: The information about VRC's database is incomplete ");
                    return null;
                }

                // otherwiese, comprise connection string with connection builder 
                SqlConnectionStringBuilder sqlConnBuilder = new SqlConnectionStringBuilder();
                sqlConnBuilder.DataSource = Configuration.vrcdbServer;
                sqlConnBuilder.InitialCatalog = Configuration.dbName;

                // examine whether the username and password is set
                if (String.IsNullOrEmpty(Configuration.dbUser) ||
                    String.IsNullOrEmpty(Configuration.dbPasswd))
                {
                    // no username/password is given, use integratedSecurity then
                    sqlConnBuilder.IntegratedSecurity = true;
                }
                else
                {
                    // set username/parssword explicitly
                    sqlConnBuilder.UserID = Configuration.dbUser;
                    sqlConnBuilder.Password = Configuration.dbPasswd;
                }

                vrcdbConnStr = sqlConnBuilder.ToString();

            }

            SqlConnection sqlConnection = new SqlConnection(vrcdbConnStr);

            try
            {
                // open the connection to DB Server
                sqlConnection.Open();
            }
            catch (Exception e)
            {
                // fail by connecting to DB Server
                // trigger escalation
                MailSender.Instance.send("VRC ERROR: Fail to connect to VRC DB ( " + vrcdbConnStr + " ) Details: " + e);
                LogWriter.error("HelperTools.getVRCDBConn: Fail to connect to VRC DB ( " + vrcdbConnStr + " ) Details: " + e);
                return null;

            }

            // keep the connection for reuse
            vrcDBConn = sqlConnection;
            return sqlConnection;
        }


        internal void closeVRCDBConn()
        {
            if (vrcDBConn != null)
            {
                vrcDBConn.Close();
                vrcDBConn = null;
            }

        }


    }
}
