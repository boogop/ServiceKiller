using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Management;
using System.Diagnostics;

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

        internal static void stopService(List<string> t, double timeout, bool continueOnFail)
        {
            for (int i = 0; i < t.Count; i++)
            {
                ServiceController service = new ServiceController(t[i]);

                // some services are non-null but trying to read a status throws an
                // invalid operation exception
                bool canProcess;
                try
                {
                    canProcess = service.Status == ServiceControllerStatus.Running;
                }
                catch (System.InvalidOperationException)
                {
                    canProcess = false;
                }
                catch (Exception)
                {
                    throw;
                }

                if (canProcess)
                {
                    if (continueOnFail)
                    {
                        try
                        {
                            stopService(t[i], timeout);

                        }
                        catch { }
                    }
                    else
                    {
                        stopService(t[i], timeout);
                    }
                }
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
            // slow, doesn't work most of the time for wtf reason
            try
            {
                ServiceController service = new ServiceController(svc);
                string foo = service.ServiceName;

                System.Diagnostics.PerformanceCounter pc = new System.Diagnostics.PerformanceCounter();
                pc.CategoryName = "Process";
                pc.CounterName = "Working Set - Private";
                pc.InstanceName = foo;
                double memsize = (double)(pc.NextValue() / 1024 / 1024); // mb

                //Process[] localByName = Process.GetProcessesByName(svc);
                //foreach (Process p in localByName)
                //{
                //    //Console.WriteLine("Private memory size64: {0}", (p.PrivateMemorySize64 / f).ToString("#,##0"));
                //    memsize += p.WorkingSet64 / 1024/ 1024;
                //    //Console.WriteLine("Peak virtual memory size64: {0}", (p.PeakVirtualMemorySize64 / f).ToString("#,##0"));
                //    //Console.WriteLine("Peak paged memory size64: {0}", (p.PeakPagedMemorySize64 / f).ToString("#,##0"));
                //    //Console.WriteLine("Paged system memory size64: {0}", (p.PagedSystemMemorySize64 / f).ToString("#,##0"));
                //    //Console.WriteLine("Paged memory size64: {0}", (p.PagedMemorySize64 / f).ToString("#,##0"));
                //    //Console.WriteLine("Nonpaged system memory size64: {0}", (p.NonpagedSystemMemorySize64 / f).ToString("#,##0"));
                //}

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
