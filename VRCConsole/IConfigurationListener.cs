using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace vrc
{
    /// <summary>
    /// All the classes who care the change of configuration 
    /// should implement this interface
    /// </summary>
    public interface IConfigurationListener
    {
        /// <summary>
        /// method which is called when configuration is changed
        /// </summary>
        void notifyConfigurationChanged();
    }
}
