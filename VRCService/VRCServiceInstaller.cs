using System;
using System.ServiceProcess;
using System.Configuration.Install;
using System.ComponentModel;
using System.Collections;

namespace vrc
{
    [RunInstaller(true)]
    public class VRCServiceInstaller : Installer
    {

        public VRCServiceInstaller()
        {
            ServiceProcessInstaller serviceProcessInstaller =
                               new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new ServiceInstaller();

            //# Service Account Information
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;

            //# Service Information
            serviceInstaller.DisplayName = "Voice Recording Controller Service";
            serviceInstaller.StartType = ServiceStartMode.Manual;

            //# This must be identical to the WindowsService.ServiceBase name
            //# set in the constructor of VRControllerService.cs
            serviceInstaller.ServiceName = "VRCService";

            this.Installers.Add(serviceProcessInstaller);
            this.Installers.Add(serviceInstaller);
        }

    }
}
