using System;
using System.ComponentModel;
using System.Collections;
using System.Threading;
using System.Data.SqlClient; 

namespace vrc
{
	/// <summary>
	/// Pool of LDCClient and TransactionSaver objects.
	/// </summary>
	public sealed class PoolManager {
        // used for sychronised access to objPool
        private static Object sync = new Object();

        private Hashtable objPool = new Hashtable();
		private const int POOL_SIZE = 1000;
		private static PoolManager poolInstance = null;

        /// <summary>
		/// Private constructor to prevent instantiation
		/// </summary>
		private PoolManager() {
 
		}
 
		/// <summary>
		/// Static constructor that gets
		/// called only once during the application's lifetime.
		/// </summary>
		static PoolManager() {
			poolInstance = new PoolManager();
		}
 
		/// <summary>
		/// Static property to retrieve the instance of the Pool Manager
		/// </summary>
		public static PoolManager Instance {
			get {
				if(poolInstance != null) {
					return poolInstance;
				}
				return null;
			}
		}
 
		/// <summary>
		/// Adds an object to the pool
		/// </summary>
		/// <param name="obj">Object to be added</param>
		/// <returns>True if success, false otherwise</returns>
		public Boolean AddObject(LDCClient obj) {
            if (objPool.Count >= POOL_SIZE)
				return false;
 
			lock(sync) {
				//objPool.Add(obj.GetHashCode(),obj);
				objPool.Add(obj.mUniqueID,obj);
			}
 
			return true;
		}
 
		/// <summary>
		/// Removes an PoolObject from the pool
		/// </summary>
		/// <param name="uid">Uid if object to remove from the pool</param>
		/// <returns>The object if success, null otherwise</returns>
		public void RemoveObject(LDCClient ldcc) {
 
			lock(sync) {
				objPool.Remove(ldcc.mUniqueID);
			}

		}


		/// <summary>
		/// Returns an PoolObject from the pool
		/// </summary>
		/// <param name="obj">Object to get from the pool</param>
		/// <returns>The object if success, null otherwise</returns>
		public LDCClient ReleaseObject(Guid uid) {
            return (LDCClient)objPool[uid];
		}




		/// <summary>
		/// just für debugging (delete it)
		/// </summary>
		public void showAll() {
            lock (sync)
            {
                foreach (Guid key in objPool.Keys)
                {
                    LDCClient obj = (LDCClient)objPool[key];
                    Console.WriteLine(key.ToString() + ": " + objPool[key].GetHashCode() + ": " + obj.ToString());
                }
            }
		}

        public LDCClient findLDCClientWithStationId(int stationId)
        {
            lock (sync)
            {
                foreach (LDCClient ldc in objPool.Values)
                {
                    if (ldc.StationID == stationId)
                    {
                        // find one existing instance
                        return ldc;
                    }
                }
            }

            // not found
            return null;
        }


        public LDCClient findLDCClientWithAgentId(int agentId)
        {
            lock (sync)
            {
                foreach (LDCClient ldc in objPool.Values)
                {
                    if (ldc.AgentID == agentId)
                    {
                        // find one existing instance
                        return ldc;
                    }
                }
            }

            // not found
            return null;
        }


		#region properties
        
        /// <summary>
        /// Returns the object pool
        /// </summary>
        public Hashtable ObjPool
        {
            get { return objPool; }
        }

		/// <summary>
		/// Property that represents the current number of objects in the pool
		/// </summary>
		public int CurrentObjectsInPool {
			get {
				return objPool.Count;
			}
		}
 
		#endregion





        internal LDCClient findLDCClientWithTrafficId(string trafficId)
        {
            lock (sync)
            {
                foreach (LDCClient ldc in objPool.Values)
                {
                    if (ldc.TrafficId.Equals(trafficId))
                    {
                        // find one existing instance
                        return ldc;
                    }
                }

            }
            // not found
            return null;
        }

    }
}
