using System;
using System.Threading;
using System.Data.SqlClient;
using System.Data;
using System.Reflection;
using System.IO;

namespace vrc
{
    /// <summary>
    /// Summary description for TransactionSaver.
    /// </summary>
    public class TransactionSaver
    {

        #region members

        private Object sync = new Object(); // for sychronised access to voice file id

        private String mData1 = "";
        private String mData2 = "";
        private String mData3 = "";
        private String mData4 = "";
        private String mData5 = "";


        private String mCRMSystem = "";
        private String mVRSystem = "";


        private String mDBServerOfCRMSystem = "";
        private String mDBServerOfVRSystem = "";
        private String mProject = "";
        private String mCampaign = "";
        private volatile String mRecordID = "";
        private volatile String mVoiceFileID = "";
        private volatile bool bRecordingAvailable = false;
        private volatile bool bSaved = false; 

        #region Properties

        public String Data1
        {
            get { return mData1; }
            set { mData1 = value; }
        }

        public String Data2
        {
            get { return mData2; }
            set { mData2 = value; }
        }

        public String Data3
        {
            get { return mData3; }
            set { mData3 = value; }
        }

        public String Data4
        {
            get { return mData4; }
            set { mData4 = value; }
        }

        public String Data5
        {
            get { return mData5; }
            set { mData5 = value; }
        }
        public String CRMSystem
        {
            get { return mCRMSystem; }
            set { mCRMSystem = value; }
        }
        public String VRSystem
        {
            get { return mVRSystem; }
            set { mVRSystem = value; }
        }

        public String DBServerOfVRSystem
        {
            get { return mDBServerOfVRSystem; }
            set { mDBServerOfVRSystem = value; }
        }

        public String DBServerOfCRMSystem
        {
            get { return mDBServerOfCRMSystem; }
            set { mDBServerOfCRMSystem = value; }
        }

        public String Project
        {
            get { return mProject; }
            set { mProject = value; }
        }

        public String Campaign
        {
            get { return mCampaign; }
            set { mCampaign = value; }
        }

        public String RecordID
        {
            get { return mRecordID; }
            set { mRecordID = value; }
        }

        public String VoiceFileID
        {
            get { return mVoiceFileID; }
            set { mVoiceFileID = value; }
        }

        public bool RecordingAvailable
        {
            get { return bRecordingAvailable; }
            set { bRecordingAvailable = value; }
        }

        #endregion


        /**
         * Paramerters for connecting to the DB (save transaction data)
         * */
        private String mSQLServer;
        private String mSQLDBName;
        private String mSQLDBUserID;
        private String mSQLDBPassword;
        private String mDSN;

        #endregion
        
        public TransactionSaver()
        {

            // load parameters for connecting VRC database from Configuration
            mSQLServer = Configuration.vrcdbServer;
            mSQLDBName = Configuration.dbName;
            mSQLDBUserID = Configuration.dbUser;
            mSQLDBPassword = Configuration.dbPasswd;
            mDSN = Configuration.dbName;

        }

        #region methods

        /// <summary>
        /// Name of the CRM-System.
        /// Corresponds to the field CRM_SYSTEM in the MAPPING table
        /// </summary>
        /// <param name="pCRMSystem"></param>
        public void setCRMSystem(String pCRMSystem)
        {
            mCRMSystem = pCRMSystem;
        }

        /// <summary>
        /// Name of the VR-System.
        /// Corresponds to the field VR-SYSTEM in the MAPPING table
        /// </summary>
        /// <param name="pVRSystem"></param>
        public void setVRSystem(String pVRSystem)
        {
            mVRSystem = pVRSystem;
        }


        /// <summary>
        /// Name of the database of the CRM-System. 
        /// Corresponds to the field CRM_DBSERVER in the RECORDING table
        /// </summary>
        /// <param name="pDBServerOfCRMSystem"></param>
        public void setDBServerOfCRMSystem(String pDBServerOfCRMSystem)
        {
            DBServerOfCRMSystem = pDBServerOfCRMSystem;
        }

        /// <summary>
        /// Name of the database of VR-System.
        /// Corresponds to the field VR_DBSERVER of the RECORDING table
        /// </summary>
        /// <param name="pDBServerOfVRSystem"></param>
        public void setDBServerOfVRSystem(String pDBServerOfVRSystem)
        {
            mDBServerOfVRSystem = pDBServerOfVRSystem;
        }


        /// <summary>
        /// Name of the project
        /// Corresponds to the field PROJECT of the RECORDING table
        /// </summary>
        /// <param name="pProject"></param>
        public void setProject(String pProject)
        {
            Project = pProject;
        }


        /// <summary>
        /// Name of the Campaign
        /// Corresponds to the field CAMPAIGN of the RECORDING table
        /// </summary>
        /// <param name="pCampaign"></param>
        public void setCampaign(String pCampaign)
        {
            mCampaign = pCampaign;

        }


        /// <summary>
        /// ID of the customer record in the db of CRM-System
        /// Corresponds to the field CRM_RECORD_ID of the RECORDING table
        /// </summary>
        /// <param name="pRecordID"></param>
        public void setRecordIDinCRMSystem(String pRecordID)
        {
            RecordID = pRecordID;
        }


        /// <summary>
        /// Set the value of the dynamic data fields
        /// Corresponds to the DATAx field of the RECORDING table
        /// </summary>
        /// <param name="value"></param>
        public void setData1(String value)
        {
            mData1 = value;
        }


        /// <summary>
        /// Set the value of the dynamic data fields
        /// Corresponds to the DATAx field of the RECORDING table
        /// </summary>
        /// <param name="value"></param>
        public void setData2(String value)
        {
            mData2 = value;
        }


        /// <summary>
        /// Set the value of the dynamic data fields
        /// Corresponds to the DATAx field of the RECORDING table
        /// </summary>
        /// <param name="value"></param>
        public void setData3(String value)
        {
            mData3 = value;
        }


        /// <summary>
        /// Set the value of the dynamic data fields
        /// Corresponds to the DATAx field of the RECORDING table
        /// </summary>
        /// <param name="value"></param>
        public void setData4(String value)
        {
            mData4 = value;
        }


        /// <summary>
        /// Set the value of the dynamic data fields
        /// Corresponds to the DATAx field of the RECORDING table
        /// </summary>
        /// <param name="value"></param>
        public void setData5(String value)
        {
            mData5 = value;
        }


        /// <summary>
        /// set the voice file id (synchronised)
        /// </summary>
        /// <param name="pVoiceFileID">voice file id</param>
        public void setVoiceFileID(String pVoiceFileID)
        {
            lock (sync)
            {
                // 2008-06-18: when a new ID is transmitted, clear transaction data
                // (new requirement by laszlo mester)
                if (pVoiceFileID != VoiceFileID)
                    clearData();

                VoiceFileID = pVoiceFileID;
            }
        }


        /// <summary>
        /// get the voice file id (synchronised)
        /// </summary>
        /// <returns>
        /// voice file id if available
        /// else: ""</returns>
        public String getVoiceFileID()
        {
            lock (sync)
            {
                return VoiceFileID;
            }
        }


        /// <summary>
        /// set the recording available flag
        /// </summary>
        /// <param name="value"></param>
        public void setRecordingAvailable(bool value)
        {
            lock (sync)
            {
                bRecordingAvailable = value;
            }
        }

        /// <summary>
        /// indicate whether the recording available flag is set
        /// </summary>
        /// <returns></returns>
        public bool isRecordingAvailable()
        {
            return bRecordingAvailable;
        }


        /// <summary>
        /// save the transaction data in the VRC-Database 
        /// afterwards, all the members (middle value) will be cleared
        /// </summary>
        /// <returns>succeed/fail</returns>
        public Boolean saveIntoVRDatabase()
        {
            if (this.getVoiceFileID().Length == 0)
            {
                return false;
            }

            SqlConnection sqlConnection = DBMgr.Instance.getVRCDBConn();
            if (sqlConnection == null)
            {
                // fail by connecting to DB Server
                LogWriter.error("TransactionSaver.saveData: Fail to connect to VRC DB \n The transaction data will be written in the export data. Please check corresponding settings in the configruation ");

                // write into export file 
                writeToExportFile();
                return false;
            }


            try
            {
                // get mapping id from MAPPING table
                SqlCommand cmd = new SqlCommand();

                String queryMappingID = "SELECT MAPPING.MAPPING_ID FROM MAPPING WHERE MAPPING.VR_SYSTEM = '" + this.VRSystem +
                    "' AND MAPPING.CRM_SYSTEM = '" + this.CRMSystem + "'";

                cmd.CommandText = queryMappingID;
                cmd.CommandType = CommandType.Text;
                cmd.Connection = sqlConnection;

                Object mapId = cmd.ExecuteScalar();

                // check whether mapping id exists
                if (mapId == null)
                {
                    MailSender.Instance.send("VRC ERROR : There's no mapping defined in the DB for the combination: <CRM,VR> :" + this.CRMSystem + "," + this.VRSystem);
                    LogWriter.error("TransactionSaver.saveIntoVRdatabase: There's no mapping for the combination: <CRM,VR> :" + this.CRMSystem + "," + this.VRSystem);
                    return false;
                };


                LogWriter.debug("TransactionSaver.saveIntoVRdatabase: the mapping id for the combination <CRM,VR>:" + this.CRMSystem + "," + this.VRSystem + " is " + mapId);


                // setup SQL-Server to truncate over long string automatically by insertion
                cmd.CommandText = "set ansi_warnings off"; // set statement
                cmd.CommandType = CommandType.Text;
                cmd.Connection = sqlConnection;
                cmd.ExecuteNonQuery();



                cmd.CommandType = CommandType.Text;
                cmd.Connection = sqlConnection;


                if (!bSaved)
                {
                    // insert new recording data 
                    cmd.CommandText = buildSQLInsert((int)mapId);
                }
                else
                {
                    // update the recording data 
                    cmd.CommandText = buildSQLUpldate();
                }


                // save the data into db as a transaction
                int affectedRows = cmd.ExecuteNonQuery();

                if (affectedRows < 1)
                {
                    LogWriter.debug("TransactionSaver.saveData: no change has been made to VRC DB");
                }
                else
                {
                    bSaved = true;
                }

            }
            catch (SqlException se)
            {
                // trigger escalation
                MailSender.Instance.send("VRC ERROR: Fail to write transaction data into VRC DB. \n  Details: " + se.Message + " \n The current transaction data is written in the export file instead :" + this.ToString());
                LogWriter.error("TransactionSaver.saveData: Exception happens by saving data into db; Details: " + se.Message);

                // write into export file 
                writeToExportFile();
                return false;
            }
            finally
            {
                DBMgr.Instance.closeVRCDBConn();
            }


            // clear all members before next run
            // 2008-06-18: Data should be kept until a new voicefile-Id is transmitted (Laszlo Mester)
            // clearData();

            return true;
        }



        /// <summary>
        /// Help method to build the update statement to save transaction data
        /// </summary>
        /// <returns>SQL statement for update</returns>
        private string buildSQLUpldate()
        {
            String SQLQuery;

            // insert into table:
            SQLQuery = "UPDATE RECORDING";
            SQLQuery += " SET ";


            // fill fields with data members 
            if (this.Project.Length > 0)
            {
                SQLQuery += "Project=";
                SQLQuery += "'" + Project + "',";
            }
            if (this.Campaign.Length > 0)
            {
                SQLQuery += "Campaign=";
                SQLQuery += "'" + this.mCampaign + "',";
            }
            if (this.DBServerOfCRMSystem.Length > 0)
            {
                SQLQuery += "CRM_DBSERVER=";
                SQLQuery += "'" + this.DBServerOfCRMSystem + "',";
            }
            if (this.DBServerOfVRSystem.Length > 0)
            {
                SQLQuery += "VR_DBSERVER=";
                SQLQuery += "'" + this.mDBServerOfVRSystem + "',";
            }
            if (this.RecordID.Length > 0)
            {
                SQLQuery += "CRM_RECORD_ID=";
                SQLQuery += "'" + this.RecordID + "',";
            }
            if (this.Data1.Length > 0)
            {
                SQLQuery += "Data1=";
                SQLQuery += "'" + this.Data1 + "',";
            }
            if (this.Data2.Length > 0)
            {
                SQLQuery += "Data2=";
                SQLQuery += "'" + this.Data2 + "',";
            }
            if (this.Data3.Length > 0)
            {
                SQLQuery += "Data3=";
                SQLQuery += "'" + this.Data3 + "',";
            }
            if (this.Data4.Length > 0)
            {
                SQLQuery += "Data4=";
                SQLQuery += "'" + this.Data4 + "',";
            }
            if (this.Data5.Length > 0)
            {
                SQLQuery += "Data5=";
                SQLQuery += "'" + this.Data5 + "',";
            }


            SQLQuery += "RECORDING_INSERTED=";
            SQLQuery += bool2bit(this.RecordingAvailable) + ",";


            // let db generate the datetime
            SQLQuery += "TIMESTAMP=";
            SQLQuery += "GETDATE()";

            SQLQuery += " WHERE VOICEFILE_ID=";
            SQLQuery += "'" + this.VoiceFileID + "'";

            LogWriter.debug("update string : " + SQLQuery);

            return SQLQuery;
        }


        /// <summary>
        /// Write the transaction data into export file
        /// (bz. vrc db is unavailable) 
        /// </summary>
        private void writeToExportFile()
        {
            // get export file from Configuration
            String filePath = Configuration.exportPathFileName; 
            if (!File.Exists(filePath))
            {
                FileStream fs = File.Create(filePath);
                fs.Close();
            }

            StreamWriter sw = File.AppendText(filePath);
            
            sw.WriteLine(DateTime.Now.ToString());
            sw.Write(this.ToString());
            sw.WriteLine();
            sw.Flush();
            sw.Close();
        }


        /// <summary>
        /// Help method to build the insert statement to save transaction data
        /// </summary>
        /// <param name="mapID">the foreign key to the MAPPING table</param>
        /// <returns>SQL statement for insertion</returns>
        private String buildSQLInsert(int mapID)
        {
            String SQLFields;
            String SQLValues;
            String SQLQuery;

            // insert into table:
            SQLFields = "INSERT INTO RECORDING (";
            SQLValues = " VALUES (";


            SQLFields += "MAPPING_ID,";
            SQLValues += mapID + ",";

            // fill fields with data members 
            if (this.Project.Length > 0)
            {
                SQLFields += "Project,";
                SQLValues += "'" + Project + "',";
            }
            if (this.Campaign.Length > 0)
            {
                SQLFields += "Campaign,";
                SQLValues += "'" + this.mCampaign + "',";
            }
            if (this.DBServerOfCRMSystem.Length > 0 )
            {
                SQLFields += "CRM_DBSERVER,";
                SQLValues += "'" + this.DBServerOfCRMSystem + "',";
            }
            if (this.DBServerOfVRSystem.Length > 0)
            {
                SQLFields += "VR_DBSERVER,";
                SQLValues += "'" + this.mDBServerOfVRSystem + "',";
            }
            if (this.RecordID.Length > 0)
            {
                SQLFields += "CRM_RECORD_ID,";
                SQLValues += "'" + this.RecordID + "',";
            }
            if (this.Data1.Length > 0)
            {
                SQLFields += "Data1,";
                SQLValues += "'" + this.Data1 + "',";
            }
            if (this.Data2.Length > 0)
            {
                SQLFields += "Data2,";
                SQLValues += "'" + this.Data2 + "',";
            }
            if (this.Data3.Length > 0)
            {
                SQLFields += "Data3,";
                SQLValues += "'" + this.Data3 + "',";
            }
            if (this.Data4.Length > 0)
            {
                SQLFields += "Data4,";
                SQLValues += "'" + this.Data4 + "',";
            }
            if (this.Data5.Length > 0)
            {
                SQLFields += "Data5,";
                SQLValues += "'" + this.Data5 + "',";
            }

            SQLFields += "VOICEFILE_ID,";
            SQLValues += "'" + this.VoiceFileID + "',";


            SQLFields += "RECORDING_INSERTED,";
            SQLValues += bool2bit(this.RecordingAvailable) + ",";


            // let db generate the datetime
            SQLFields += "TIMESTAMP,";
            SQLValues += "GETDATE(),"; 

            SQLQuery = SQLFields.Substring(0, SQLFields.Length - 1) + ")"
                        + SQLValues.Substring(0, SQLValues.Length - 1) + ")";

            LogWriter.debug("insert string : " + SQLQuery);

            return SQLQuery;
        }

        private int bool2bit(bool p)
        {
            if (p)
                return 1;
            else
                return 0;
        }


        /// <summary>
        /// reset the members after saving the record in db
        /// </summary>
        private void clearData()
        {
            // the values set by "SAVE_RFCORDING_DATA
            mData1 = "";
            mData2 = "";
            mData3 = "";
            mData4 = "";
            mData5 = "";

            // the values set by "SAVE_RFCORDING_REFERENCE
            mRecordID = "";
            mDBServerOfCRMSystem = "";


            // the value set by "ConnectionGuard.Monitoring"
            mVoiceFileID = "";

            // the values set by both 
            mProject = "";
            mCampaign = "";

            // reset flags
            bSaved = false;
            bRecordingAvailable = false;


            mCRMSystem = "";
            mVRSystem = "";

            // the values set during Login
            // mDBServerOfVRSystem = "";


        }

        /// <summary>
        /// Override the ToString() Method 
        /// used for emergency export 
        /// </summary>
        /// <returns>transaction data as string</returns>
        public override String ToString()
        {
            StringWriter sw = new StringWriter();

            // use reflect to list all members and its value
            PropertyInfo[] myPropertyInfos = this.GetType().GetProperties();

            // Display information for all properties.
            for (int i = 0; i < myPropertyInfos.Length; i++)
            {
                PropertyInfo myPropertyInfo = (PropertyInfo)myPropertyInfos[i];
                sw.WriteLine(myPropertyInfo.Name + " = " + myPropertyInfo.GetValue(this, null ));
            }

            return sw.ToString();
        }

        #endregion

    }
}
