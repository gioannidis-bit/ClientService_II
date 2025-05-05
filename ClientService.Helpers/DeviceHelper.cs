// ClientService.Helpers/DeviceHelper.cs
using System;
using System.Management;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using log4net;

namespace ClientService.Helpers
{
    public class DeviceHelper
    {
        private static ManagementEventWatcher insertWatcher;
        private static ManagementEventWatcher removeWatcher;


        public static void StartDeviceWatcher()
        {
            try
            {
                // Δημιουργία ενός watcher για σύνδεση συσκευών
                WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
                insertWatcher = new ManagementEventWatcher(insertQuery);
                insertWatcher.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
                insertWatcher.Start();

                // Δημιουργία ενός watcher για αποσύνδεση συσκευών
                WqlEventQuery removeQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
                removeWatcher = new ManagementEventWatcher(removeQuery);
                removeWatcher.EventArrived += new EventArrivedEventHandler(DeviceRemovedEvent);
                removeWatcher.Start();

                logger.Info("USB device monitoring started");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to start USB monitoring: {ex.Message}");
            }
        }

        private static void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            try
            {
                logger.Info("USB device connected - checking if it's our scanner");

                // Καθυστέρηση για να σταθεροποιηθεί η σύνδεση
                System.Threading.Thread.Sleep(3000);

                // Έλεγχος αν η συσκευή είναι ο scanner μας
                if (IsScannerConnected())
                {
                    logger.Info("Scanner reconnected - reinitializing");

                    // Επανεκκίνηση του scanner
                    Service.CompleteDeviceReinitialization();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error handling USB device insertion: {ex.Message}");
            }
        }

        private static void DeviceRemovedEvent(object sender, EventArrivedEventArgs e)
        {
            try
            {
                logger.Info("USB device removed - checking if scanner is still connected");

                // Έλεγχος αν ο scanner παραμένει συνδεδεμένος
                if (!IsScannerConnected())
                {
                    logger.Warn("Scanner disconnected");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error handling USB device removal: {ex.Message}");
            }
        }

        public static void StopDeviceWatcher()
        {
            try
            {
                if (insertWatcher != null)
                {
                    insertWatcher.Stop();
                    insertWatcher.Dispose();
                    insertWatcher = null;
                }

                if (removeWatcher != null)
                {
                    removeWatcher.Stop();
                    removeWatcher.Dispose();
                    removeWatcher = null;
                }

                logger.Info("USB device monitoring stopped");
            }
            catch (Exception ex)
            {
                logger.Error($"Error stopping USB monitoring: {ex.Message}");
            }
        }



        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Ελέγχει αν υπάρχουν διαθέσιμες συσκευές USB
        /// </summary>
        /// <returns>True αν βρέθηκαν συσκευές USB, αλλιώς False</returns>
        public static bool AreUsbDevicesAvailable()
        {
            try
            {
                logger.Info("Checking if USB devices are available");

                List<string> devices = GetConnectedUsbDevices();

                if (devices.Count > 0)
                {
                    logger.Info($"Found {devices.Count} USB devices");
                    return true;
                }
                else
                {
                    logger.Warn("No USB devices found");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Exception checking USB devices: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Λαμβάνει μια λίστα με τις διαθέσιμες συσκευές USB χρησιμοποιώντας εξωτερική εντολή
        /// </summary>
        /// <returns>Λίστα με τις συσκευές USB</returns>
        public static List<string> GetConnectedUsbDevices()
        {
            List<string> devices = new List<string>();

            try
            {
                logger.Info("Getting list of connected USB devices");

                // Χρήση της εντολής wmic για λήψη πληροφοριών συσκευών
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = "path Win32_USBControllerDevice get Dependent",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                Process process = Process.Start(psi);

                // Ανάγνωση της εξόδου
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Επεξεργασία αποτελεσμάτων
                string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    // Αγνόηση της γραμμής επικεφαλίδας
                    if (!line.Trim().Equals("Dependent", StringComparison.OrdinalIgnoreCase))
                    {
                        // Εξαγωγή του ονόματος της συσκευής από την έξοδο του wmic
                        if (line.Contains("DeviceID="))
                        {
                            string deviceInfo = line.Trim();
                            devices.Add(deviceInfo);
                        }
                    }
                }

                logger.Info($"Found {devices.Count} USB devices");
                return devices;
            }
            catch (Exception ex)
            {
                logger.Error($"Error getting USB devices: {ex.Message}", ex);
                return devices;
            }
        }

        /// <summary>
        /// Ελέγχει αν ο scanner είναι συνδεδεμένος με βάση το αναγνωριστικό του
        /// </summary>
        /// <param name="deviceId">Το αναγνωριστικό του scanner (προεπιλογή: VID_08FF για AccessIS)</param>
        /// <returns>True αν ο scanner είναι συνδεδεμένος, αλλιώς False</returns>
        public static bool IsScannerConnected(string deviceId = "VID_08FF") // Προσαρμόστε το VID ανάλογα με τον scanner σας
        {
            try
            {
                List<string> devices = GetConnectedUsbDevices();

                // Έλεγχος αν κάποια από τις συνδεδεμένες συσκευές αντιστοιχεί στο scanner
                foreach (string device in devices)
                {
                    if (device.Contains(deviceId))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                logger.Error($"Error checking scanner connection: {ex.Message}", ex);
                return false;
            }
        }
    }
}