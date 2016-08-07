using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using GlobalObjects;

namespace RemotingContract
{
   public interface IRemoteConfig
    {
       void UpdateConfiguration(DSVRCConfig ds);
    }
}
