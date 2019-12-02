using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Management;

namespace ServiceKiller
{
    internal static class clsServices
    {

        internal static void getAllServices(out ServiceController[] foo)
        {
            foo = ServiceController.GetServices();

            // sort 'em
            Array.Sort(foo, delegate (ServiceController x, ServiceController y) { return x.DisplayName.CompareTo(y.DisplayName); });
        }

        internal static void startService(string svc, double timeout)
        {
            TimeSpan bail = TimeSpan.FromMilliseconds(chkNull.numNull(timeout));
            ServiceController service = new ServiceController(svc);
            service.Start();
            service.WaitForStatus(ServiceControllerStatus.Running, bail);
        }

        internal static void stopService(string svc, double timeout)
        {
            TimeSpan bail = TimeSpan.FromMilliseconds(chkNull.numNull(timeout));
            ServiceController service = new ServiceController(svc);
            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, bail);
        }

        internal static void stopService(List<string> t, double timeout)
        {
            for (int i = 0; i < t.Count; i++)
            {
                ServiceController service = new ServiceController(t[i]);
                if (service.Status == ServiceControllerStatus.Running)
                    stopService(t[i], timeout);
            }
        }

        internal static string getServiceDescription(string svc)
        {
            ManagementObject serviceObject = new ManagementObject(new ManagementPath(string.Format("Win32_Service.Name='{0}'", svc)));
            string desc = "Not found"; 
            // this isn't anappropriate use of a try-catch but service descriptions frequently 
            // aren't where you expect them to be. The only way to do it is catch when it fails.
            try
            {
                desc = chkNull.whenNull(serviceObject["Description"]);
                if (chkNull.isNull(desc))
                    desc = "Not found";
            }
            catch
            {
                desc = "Unable to read service description";
            }

           // test(svc);

            return desc;
        }

        internal static double getMemSize(string svc)
        {
            // slow
            try
            {
                ServiceController service = new ServiceController(svc);
                string foo = service.ServiceName;

                System.Diagnostics.PerformanceCounter pc = new System.Diagnostics.PerformanceCounter();
                pc.CategoryName = "Process";
                pc.CounterName = "Working Set - Private";
                pc.InstanceName = foo;
                double memsize = (double)(pc.NextValue() / 1024 / 1024); // mb

                return Math.Round(memsize,2);
            }
            catch 
            {
                return -1;
            }
        }

        internal static string getStatus(ServiceController svc)
        {
            ServiceControllerStatus status = svc.Status;
            return chkNull.whenNull(status);
        }

    }
}
