using System;

namespace vrc
{
	/// <summary>
	/// Maps LDCClient and TransactionSaver reference with an Uid and stores the date of its last use
	/// </summary>
	public abstract class PoolObject : IDisposable{

		public Guid mUniqueID;
        public DateTime mLastActivity = DateTime.Now;

        public virtual void Dispose(){}

    }
}

