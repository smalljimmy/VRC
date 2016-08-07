using System.ServiceProcess;
using System.Configuration.Install;
using System;
using System.Collections;

namespace vrc
{
    /// <summary>
    /// Startpoint of VRC service.
    /// </summary>
    class VRCService : ServiceBase
    {


        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args"></param>
        static void Main()
        {

            #if (!DEBUG)

                System.ServiceProcess.ServiceBase[] ServicesToRun;
                ServicesToRun = new System.ServiceProcess.ServiceBase[] { new VRCService() };
                System.ServiceProcess.ServiceBase.Run(ServicesToRun);
            #else
                VRCService vs = new VRCService();
                vs.OnStart(null);
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);

            #endif


        }

        private static _Thread newVRCService;

        protected override void OnStart(string[] args)
        {

            newVRCService = new VRCController();
            newVRCService.run();

        }
        protected override void OnStop()
        {
            newVRCService.stop();
            
        }

        private void InitializeComponent()
        {
            // 
            // VRControllerService
            // 
            this.ServiceName = "VRCService II";

        }
    }
}