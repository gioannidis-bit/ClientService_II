// ClientService.Helpers/DeviceHelper.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using log4net;

namespace ClientService.Helpers
{
    public class DeviceHelper
    {
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // Στο DeviceHelper.cs, προσθέστε:
        public static bool IsScannerConnected(string deviceId = "VID_08FF") // Αλλάξτε το VID_08FF με το αναγνωριστικό του δικού σας scanner
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
                logger.Error($"Error checking scanner connection: {ex.Message}");
                return false;
            }
        }



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
                            logger.Debug($"Found USB device: {deviceInfo}");
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
        /// Προσπαθεί να επαναφέρει τις USB συσκευές
        /// </summary>
        /// <returns>True αν η επαναφορά ήταν επιτυχής, αλλιώς False</returns>
        public static bool ResetUsbDevices()
        {
            try
            {
                logger.Info("Attempting to reset USB devices");

                // Χρήση taskkill για να τερματίσουμε τυχόν διεργασίες που μπλοκάρουν USB
                try
                {
                    ProcessStartInfo psiTaskkill = new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = "/F /IM winusb.exe /IM usbhub.exe /IM usbaudio.exe 2>nul",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    Process taskkillProcess = Process.Start(psiTaskkill);
                    string taskkillOutput = taskkillProcess.StandardOutput.ReadToEnd();
                    taskkillProcess.WaitForExit();

                    logger.Info($"Taskkill output: {taskkillOutput}");
                }
                catch (Exception ex)
                {
                    logger.Error($"Error using taskkill: {ex.Message}", ex);
                }

                // Σύντομη αναμονή για να επιτρέψουμε στο σύστημα να ανιχνεύσει ξανά τις συσκευές
                Thread.Sleep(2000);

                // Έλεγχος αν έχουμε συσκευές USB μετά την επαναφορά
                List<string> devices = GetConnectedUsbDevices();
                bool hasDevicesAfterReset = devices.Count > 0;

                logger.Info($"After reset: Found {devices.Count} USB devices");

                return hasDevicesAfterReset;
            }
            catch (Exception ex)
            {
                logger.Error($"Error resetting USB devices: {ex.Message}", ex);
                return false;
            }
        }
    }
}