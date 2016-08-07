using System;
using System.Collections.Generic;
using System.Text;

namespace vrc
{

    /// <summary>
    /// enumerate all the VRCP commands (Request / Reply)
    /// </summary>
    public enum VRCPCmd
    {
                    USER_LOGIN,
                    USER_LOGIN_SUCCESS,
                    USER_LOGIN_FAILURE,
                    USER_LOGOUT,
                    USER_LOGOUT_SUCCESS,
                    USER_LOGOUT_FAILURE,
                    START_NEW_RECORDING,
                    START_NEW_RECORDING_SUCCESS,
                    START_NEW_RECORDING_FAILURE,
                    PAUSE_RECORDING,
                    PAUSE_RECORDING_SUCCESS,
                    PAUSE_RECORDING_FAILURE,
                    CONTINUE_RECORDING,
                    CONTINUE_RECORDING_SUCCESS,
                    CONTINUE_RECORDING_FAILURE,
                    SET_ONE_WAY,
                    SET_ONE_WAY_SUCCESS,
                    SET_ONE_WAY_FAILURE,
                    SET_TWO_WAY,
                    SET_TWO_WAY_SUCCESS,
                    SET_TWO_WAY_FAILURE,
                    SAVE_RECORDING_REFERENCE,
                    SAVE_RECORDING_REFERENCE_SUCCESS,
                    SAVE_RECORDING_REFERENCE_FAILURE,
                    SAVE_RECORDING_DATA,
                    SAVE_RECORDING_DATA_SUCCESS,
                    SAVE_RECORDING_DATA_FAILURE,
                    GET_VOICEFILE_ID,
                    GET_VOICEFILE_ID_SUCCESS,
                    GET_VOICEFILE_ID_FAILURE, 
                    SET_INFO_FLAG,

                    UNKNOWN

        }
}
