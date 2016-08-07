using System;
using System.Threading;

namespace vrc
{
	/// <summary>
	/// Base class for a thread
	/// </summary>
	public abstract class _Thread
	{

		protected Thread thread;

        /// <summary>
        /// Entry point for the thread
        /// </summary>
		abstract protected void ThreadProc();

        /// <summary>
        ///  signal event for stopping the thread
        /// </summary>
        protected AutoResetEvent _stopper = new AutoResetEvent(false);

		public _Thread(){}


		#region methods

		/// <summary>
		/// Start the thread
		/// </summary>
		public void run(){
            if (thread != null)
            {
                throw new ApplicationException("Can't restart thread when it's not stopped yet");
            }

            thread = new Thread(new ThreadStart(ThreadProc));
            thread.Start();

		}


        /// <summary>
        /// Stop the thread.
        /// Blocking call. Return when the thread is stopped
        /// </summary>
        public virtual void stop()
        {
            if (thread == null)
            {
                return;
            }

            // let thread terminate
            _stopper.Set();

            thread.Join();

            thread = null;

        }

		#endregion
	}
}
