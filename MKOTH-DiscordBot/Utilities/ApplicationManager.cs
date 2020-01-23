using System;
using System.Diagnostics;
using System.Management;
using System.Reflection;

namespace MKOTHDiscordBot
{
    public static class ApplicationManager
    {
        public static (float ramUsageMB, float freeRamGB, float ramSizeGB, ushort cpuUsagePercent) GetResourceUsage()
        {
            var prcName = Process.GetCurrentProcess().ProcessName;
            var ramUsageMB = (float)new PerformanceCounter("Process", "Working Set - Private", prcName).RawValue / 1024 / 1024;
            var freeRamGB = new PerformanceCounter("Memory", "Available MBytes").NextValue() / 1024;
            ulong ramSizeGB = 0;
            ushort cpuUsagePercent = 0;

            var ramQuery = new ObjectQuery("SELECT * FROM CIM_PhysicalMemory");
            var managementRamCollection = new ManagementObjectSearcher(ramQuery).Get();
            var cpuQuery = new ObjectQuery("SELECT * FROM CIM_Processor");
            var managementCPUCollection = new ManagementObjectSearcher(cpuQuery).Get();

            foreach (var item in managementRamCollection)
            {
                ramSizeGB += (ulong)item["Capacity"] / 1024 / 1024 / 1024;
            }
            
            foreach (var item in managementCPUCollection)
            {
                cpuUsagePercent = (ushort?)item?["LoadPercentage"] ?? 0;
            }

            return (ramUsageMB, freeRamGB, ramSizeGB, cpuUsagePercent);
        }

        public static void RestartApplication(ulong responseChannelId)
        {
            ClosingPreparation();
            Process.Start("dotnet", $"\"{Assembly.GetExecutingAssembly().Location}\" Restarted {responseChannelId}");
            Environment.Exit(0);
        }

        public static void ShutDownApplication()
        {
            ClosingPreparation();
            Environment.Exit(0);
        }

        private static void ClosingPreparation()
        {
            try
            {
                ApplicationContext.DiscordClient.LogoutAsync().GetAwaiter().GetResult();
                ApplicationContext.DiscordClient.StopAsync().GetAwaiter().GetResult();
                ApplicationContext.DiscordClient.Dispose();
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }
}
