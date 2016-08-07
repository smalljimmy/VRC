using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace vrc
{
    class ConfigurationTest
    {

        [STAThread]
        static void Main_disable(string[] args)
        {
            PropertyInfo[] myPropertyInfo  = typeof(Configuration).GetProperties(BindingFlags.Public|BindingFlags.Static);

            // Display information for all properties.
            for (int i = 0; i < myPropertyInfo.Length; i++)
            {
                PropertyInfo myPropInfo = (PropertyInfo)myPropertyInfo[i];
                Console.WriteLine( myPropInfo.Name + " = " + myPropInfo.GetValue(null, null));
            }

            Console.Read();

        }
    }
}
