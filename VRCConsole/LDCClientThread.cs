using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
/**
namespace vrc
{
    /// <summary>
    /// The LDCClientThread is started as a thread
    /// to process the command asychronisely. That means,
    /// the command request from client will be buffered in the 
    /// command queue and be processed in order in the background.
    /// </summary>
    class LDCClientThread : ILDCClient
    {
        // used for sychronisation 
        private Queue<CommandObject> mCommandQueue;
        private vrc.LDCClient.SyncEvents mSyncEvents;

        // used for process command 
        private volatile CommandObject cmdObj;
        
        private Communication mCommunication; // communication with LDC-Server
        private TransactionSaver mTransactionSaver; // save transaction info


        public LDCClientThread(Queue<CommandObject> commandQueue, vrc.LDCClient.SyncEvents syncEvents, Communication communication, TransactionSaver transactionSaver)
        {
            this.mCommandQueue = commandQueue;
            this.mSyncEvents = syncEvents;
            this.mCommunication = communication;
            this.mTransactionSaver = transactionSaver;
        }


        /// <summary>
        /// central part
        /// retrieve and process a command from the command queue once upon a time
        /// </summary>
        public void run()
        {

            while (WaitHandle.WaitAny(mSyncEvents.EventArray) != 1)
            {
                
                // query for the next command in queue
                lock (((ICollection)mCommandQueue).SyncRoot)
                {
                    cmdObj = mCommandQueue.Dequeue();
                }
                Console.WriteLine("Consumer Thread: consumed {0} items", cmdObj.ReqString);

                Command cmd = cmdObj.Cmd;
                
                // despatch the command 
                if (cmd == Command.USER_LOGIN)
                {
                    login();
                }
                else if (cmd == Command.USER_LOGOUT)
                {
                    logout();
                }
                else if (cmd == Command.SET_ONE_WAY)
                {
                    setOneWay();
                }
                else if (cmd == Command.SET_TWO_WAY)
                {
                    setTwoWay();
                }

                else if (cmd == Command.START_NEW_RECORDING)
                {
                    startNewRecording();
                }
                else if (cmd == Command.PAUSE_RECORDING)
                {
                    pauseRecording();
                }
                else if (cmd == Command.CONTINUE_RECORDING)
                {
                    continueRecording();
                }

            }
        }


        #region ILDCClient Members

        /// <summary>
        /// Start Login
        /// generate a communication instance for connection with LDC-Server
        /// reply the client with unique id if login succeed
        /// else reply the client with login fail message
        /// </summary>
        public void login()
        {
            // get parameters 
            LoginCmd loginPars = (LoginCmd)cmdObj.Pars;
            String ldcSvr = loginPars.ldcSvr;
            int ldcSvrPort = 20001; // predefined port

            this.mCommunication = new Communication(ldcSvr, ldcSvrPort);
            if (mCommunication.initiateCommunication())
            {
                // send Login_Success Msg together with new UID
                

            }
            else
            {   // send Login_Fail Msg 
            }
        }

        public void logout()
        {
            throw new NotImplementedException();
        }

        public void lostConnectionEvent()
        {
            throw new NotImplementedException();
        }

        public void pauseRecording()
        {
            throw new NotImplementedException();
        }

        public void continueRecording()
        {
            throw new NotImplementedException();
        }

        public void reestablishConnection()
        {
            throw new NotImplementedException();
        }

        public void setOneWay()
        {
            throw new NotImplementedException();
        }

        public void setTwoWay()
        {
            throw new NotImplementedException();
        }

        public void startNewRecording()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
*/