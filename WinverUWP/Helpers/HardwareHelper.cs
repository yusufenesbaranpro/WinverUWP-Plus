using System;
using System.Runtime.InteropServices;

namespace WinverUWP.Helpers
{
    public static class HardwareHelper
    {
        public static string GetProcessorName()
        {
            var cpu = RegistryHelper.GetInfoString("ProcessorNameString", @"\Registry\Machine\HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            if (string.IsNullOrWhiteSpace(cpu))
                return "Unknown Processor";
                
            // Cleanup extra spaces often found in CPU names
            return System.Text.RegularExpressions.Regex.Replace(cpu, @"\s+", " ").Trim();
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx([In, Out] ref MEMORYSTATUSEX lpBuffer);

        public static string GetTotalMemory()
        {
            var memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            if (GlobalMemoryStatusEx(ref memStatus))
            {
                double gb = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                // Round to nearest decent number if close (e.g. 15.9 -> 16)
                // Actually showing 1 decimal is fine.
                // But typically RAM is 8, 16, 32. 
                // Let's format nicely.
                return $"{gb:F1} GB";
            }
            return "Unknown Memory";
        }

        public static string GetGPUName()
        {
            // Try 0000 (usually Integrated or Primary)
            var gpu = RegistryHelper.GetInfoString("DriverDesc", @"\Registry\Machine\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000");
            
            if (string.IsNullOrEmpty(gpu))
            {
                 // Try 0001 (Discrete sometimes)
                 gpu = RegistryHelper.GetInfoString("DriverDesc", @"\Registry\Machine\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0001");
            }
            
            return gpu ?? "Unknown Graphics";
        }

        [DllImport("kernel32")]
        public static extern ulong GetTickCount64();

        public static string GetUptime()
        {
            TimeSpan uptime = TimeSpan.FromMilliseconds(GetTickCount64());
            return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
        }

        public static string GetBatteryStatus()
        {
            try
            {
                var battery = Windows.Devices.Power.Battery.AggregateBattery;
                var report = battery.GetReport();

                if (report.Status == Windows.System.Power.BatteryStatus.NotPresent)
                    return "No Battery";

                if (report.RemainingCapacityInMilliwattHours.HasValue && report.FullChargeCapacityInMilliwattHours.HasValue)
                {
                    double pct = (double)report.RemainingCapacityInMilliwattHours.Value / (double)report.FullChargeCapacityInMilliwattHours.Value * 100.0;
                    return $"{pct:F0}% ({report.Status})";
                }
                return report.Status.ToString();
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
