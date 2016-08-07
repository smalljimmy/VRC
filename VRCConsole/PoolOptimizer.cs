using System;
using System.Threading;
using System.Collections;

namespace vrc
{
    /// <summary>
    /// PoolOptimizer purges the idle PoolObject (not active for longer than 
    /// predefined threshold(see Configuration.objectLifeTime)) from the object pool.
    /// Run periodically (see Configuration.PollingInterval)
    /// </summary>
    public class PoolOptimizer : _Thread
	{


		#region members

		private static PoolOptimizer instance = null;
		private static PoolManager pm = PoolManager.Instance;

		#endregion

		#region constructor

		/// <summary>
		/// Private constructor to prevent instantiation
		/// </summary>
		public PoolOptimizer() {
		}
		/// <summary>
		/// Static constructor that gets called only once during the application's lifetime.
		/// </summary>
		static PoolOptimizer() {
			instance = new PoolOptimizer();
		}
 
		/// <summary>
		/// Static property to retrieve the instance of the Pool Manager
		/// </summary>
		public static PoolOptimizer Instance {
			get {
				if(instance != null) {
					return instance;
				}
				return null;
			}
		}

		#endregion

		#region methods


		/// <summary>
		/// pool optimizer logic
		/// </summary>
        protected override void ThreadProc()
        {
            LogWriter.debug("PoolOptimizer: PoolOptimizer is started");

            // sleep for a random time between 10 - 30 seconds before first run 
            Thread.Sleep(new Random().Next(10,30) * 1000);

			LDCClient ldcc = null;
			TimeSpan tdiff = new TimeSpan();

            Boolean loop = true;

			// endless polling loop 
			while(loop) {

               Hashtable ht = pm.ObjPool; // get the object pool

               if (ht.Keys.Count > 0) // only start when the pool is not empty
                {
                    LogWriter.debug("PoolOptimizer.run: Optimization begins to run");

                    Hashtable dht = new Hashtable(); // hold objects which will be removed

                    // check last activity for every object in pool.
                    foreach (Guid key in ht.Keys)
                    {
                        ldcc = (LDCClient)ht[key];

                        tdiff = DateTime.Now - ldcc.mLastActivity;

                        if (tdiff.TotalMilliseconds > Convert.ToInt32(Configuration.objectLifeTime))
                        {
                            // register the objects which will be removed from pool
                            dht.Add(key, ht[key]);
                        }
                    }


                    //remove the not used objects in one time
                    foreach (Guid delkey in dht.Keys)
                    {
                        ldcc = (LDCClient)dht[delkey];
                        LogWriter.debug("The LDCClient " + delkey + " is removed while it's idle too long ");

                        // disconnect client if the LDCClient is bound with session
                        if (ldcc.isBounded())
                        {
                            LogWriter.debug("PoolOptimizer.run: the associated session with the LDCClient will also be disposed ");
                         }

                        // release resource occupied for the obj
                        ldcc.Dispose(false);

                    }


                    LogWriter.debug("PoolOptimizer.run: Optimization ends");
                }

               // wait some time before next run. Break if _stopper is set.
               if (_stopper.WaitOne(Convert.ToInt32(Configuration.pollingInterval), false))
               {
                   loop = false;
               }
			}
		}

		#endregion
    }
}
