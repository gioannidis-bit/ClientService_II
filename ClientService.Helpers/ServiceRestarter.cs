// ClientService.Helpers/ServiceRestarter.cs
using System;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;
using log4net;

namespace ClientService.Helpers
{
    public class ServiceRestarter
    {
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // Επανεκκίνηση του τρέχοντος service
        public static bool RestartService(string serviceName)
        {
            try
            {
                logger.Info($"Attempting to restart service: {serviceName}");

                // Δημιουργία ενός αρχείου restarter batch που θα εκτελεστεί μετά από καθυστέρηση
                string batchPath = CreateRestartBatch(serviceName);

                // Εκκίνηση του batch με καθυστέρηση
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batchPath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(psi);

                logger.Info($"Service restart process initiated for {serviceName}");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to restart service: {ex.Message}", ex);
                return false;
            }
        }

        // Δημιουργία του batch script για επανεκκίνηση
        private static string CreateRestartBatch(string serviceName)
        {
            string tempPath = System.IO.Path.GetTempPath();
            string batchFile = System.IO.Path.Combine(tempPath, $"RestartService_{serviceName}.bat");

            // Περιεχόμενο του batch file
            string batchContent =
@"@echo off
REM Wait a few seconds to allow the original process to exit
timeout /t 5 /nobreak > nul

REM Stop and restart the service
net stop ""{0}""
timeout /t 2 /nobreak > nul
net start ""{0}""

REM Clean up this batch file
del ""%~f0""
";

            // Αντικατάσταση του placeholder με το όνομα του service
            batchContent = string.Format(batchContent, serviceName);

            // Αποθήκευση σε αρχείο
            System.IO.File.WriteAllText(batchFile, batchContent);

            return batchFile;
        }

        // Επανεκκίνηση της εφαρμογής (αν δεν τρέχει ως service)
        public static void RestartApplication()
        {
            try
            {
                logger.Info("Attempting to restart application");

                string exePath = Assembly.GetExecutingAssembly().Location;

                // Δημιουργία batch για επανεκκίνηση εφαρμογής
                string batchContent =
@"@echo off
REM Wait a few seconds
timeout /t 3 /nobreak > nul

REM Start the application again
start """" ""{0}""

REM Clean up this batch file
del ""%~f0""
";

                string batchFile = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"RestartApp_{Process.GetCurrentProcess().Id}.bat");

                System.IO.File.WriteAllText(batchFile, string.Format(batchContent, exePath));

                // Εκτέλεση του batch
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batchFile}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                // Τερματισμός της τρέχουσας εφαρμογής
                logger.Info("Exiting current application instance for restart");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to restart application: {ex.Message}", ex);
            }
        }

        // Γενική μέθοδος επανεκκίνησης (είτε service είτε εφαρμογή)
        public static void RestartCurrentProcess()
        {
            try
            {
                // Έλεγχος αν τρέχουμε ως service
                bool isService = !Environment.UserInteractive;
                logger.Info($"Restarting current process (running as service: {isService})");

                if (isService)
                {
                    // Επανεκκίνηση ως service
                    RestartService(Service.SERVICENAME);
                }
                else
                {
                    // Επανεκκίνηση ως εφαρμογή
                    RestartApplication();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error in RestartCurrentProcess: {ex.Message}", ex);
            }
        }
    }
}