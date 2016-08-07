using System;
using System.Collections.Generic;
using System.Text;
using CodeBureau;

namespace vrc
{
	/// <summary>
	/// All the available category code (REQUEST/RESPONSE) of LDC-Server 
    /// from onsoft
    /// use user-defined Attribute (see StringEnum.cs) to support String-valued Emnumeration
	/// </summary>
    public enum LDCCmd
	{
        [StringValue("")]
        UNKNOWN, // use for error handling

        [StringValue("#1000")]
        LOGOUT,

        [StringValue("#1001")]
        CAT_LDC_FILE_GET_REQUEST,

        [StringValue("#1004")]
        DB_STRING_REQUEST,
        [StringValue("#5005")]
        DB_STRING_REPLY,

        [StringValue("#1005")]
        USER_LOGIN_REQUEST,
        [StringValue("#5006")]
        USER_LOGIN_REPLY,

        [StringValue("#1006")]
        PING,
        [StringValue("#5007")]
        PONG,

        [StringValue("#1511")]
        TRANSACTION_INFO_FLAG_SET_REQUEST,
        [StringValue("#5522")]
        TRANSACTION_INFO_FLAG_SET_REPLY,
        [StringValue("#5523")]
        TRANSACTION_INFO_FLAG_SET_DENIED,

        [StringValue("#2013")]
        RECORDING_CONTROL_REQUEST,
        [StringValue("#6026")]
        RECORDING_CONTROL_REPLY,
        [StringValue("#6027")]
        RECORDING_CONTROL_DENIED,

        [StringValue("#2014")]
        ASSIGN_WORKING_GROUP_TO_AGENT,
        [StringValue("#6028")]
        ASSIGN_WORKING_GROUP_TO_AGENT_REPLY,
        [StringValue("#6029")]
        ASSGIN_WORKING_GROUP_TO_AGENT_DENIED,

        [StringValue("#2015")]
        ASSIGN_AGENT_TO_STATION_REQUEST,
        [StringValue("#6030")]
        ASSIGN_AGENT_TO_STATION_REPLY,
        [StringValue("#6031")]
        ASSIGN_AGENT_TO_STATION_DENIED,

        [StringValue("#2000")]
        CAT_LDC_DISCRETE_LISTENING_START_REQUEST,
        [StringValue("#6000")]
        CAT_LDC_DISCRETE_LISTENING_START_REPLY,
        [StringValue("#6001")]
        CAT_LDC_DISCRETE_LISTENING_START_DENIED,

        [StringValue("#2001")]
        CAT_LDC_DISCRETE_LISTENING_STOP_REQUEST,
        [StringValue("#6002")]
        CAT_LDC_DISCRETE_LISTENING_STOP_REPLY,

        [StringValue("#2002")]
        CAT_LDC_PAUSED_START_REQUEST,
        [StringValue("#6003")]
        CAT_LDC_PAUSE_START_REPLY,
        [StringValue("#6004")]
        CAT_LDC_PAUSED_START_DENIED,

        [StringValue("#2003")]
        CAT_LDC_PAUSED_STOP_REQUEST,
        [StringValue("#6005")]
        CAT_LDC_PAUSED_STOP_REPLY,
        [StringValue("#6006")]
        CAT_LDC_PAUSED_STOP_DENIED,

        [StringValue("#2004")]
        CAT_LDC_RECORDING_CREATE_REQUEST,
        [StringValue("#6007")]
        CAT_LDC_RECORDING_CREATE_REPLY,
        [StringValue("#6008")]
        CAT_LDC_RECORDING_CREATE_DENIED,

        [StringValue("#2005")]
        CAT_LDC_RECORDING_REMOVE_REQUEST,
        [StringValue("#6009")]
        CAT_LDC_RECORDING_REMOVE_REPLY,
        [StringValue("#6010")]
        CAT_LDC_RECORDING_REMOVE_DENIED,


        [StringValue("#4350")]
        MONITOR_LDCC_INFO_REQUEST,
        [StringValue("#8600")]
        MONITOR_LDCC_INFO_REPLY,
        [StringValue("#8700")]
        MONITOR_LDCC_INFO_INDICATION,

        [StringValue("#4366")]
        MONITOR_LDCC_SET_SCOPE_REQUEST,
        [StringValue("#8712")]
        MONITOR_LDCC_SET_SCOPE_REPLY,
        [StringValue("#8713")]
        MONITOR_LDCC_SET_SCOPE_DENIED,

        [StringValue("#4369")]
        MONITOR_FILE_AVAILABLE_REQUEST,
        [StringValue("#8716")]
        MONITOR_FILE_AVAILABLE_INFO_REPLY,

        [StringValue("#5002")]
        CAT_LDC_FILE_END,
        [StringValue("#5032")]
        CAT_LDC_RECORDING_INSERTED,
        
        [StringValue("#5030")]
        DUMMY_UNKNOWN,

        [StringValue("#5511")]
        CAT_LDC_DATA_TRANSPARENT_MERGED

	}
}
