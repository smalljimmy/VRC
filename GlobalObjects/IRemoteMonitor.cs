using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace GlobalObjects
{
    public interface IRemoteMonitor
    {
        DSVRCMonitoring GetMonVals();

    }
}
