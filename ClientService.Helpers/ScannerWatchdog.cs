using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Timers;
using ClientService.Classes.Factories;
using log4net;

namespace ClientService.Helpers
{
    public class ScannerWatchdog : IDisposable
    {
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private System.Timers.Timer watchdogTimer;
        private System.Timers.Timer flagCheckTimer;
        private DateTime lastDataReceived;
        private readonly TimeSpan reconnectThreshold;
        private readonly int reconnectIntervalMs;
        private readonly string reconnectFlagPath = @"C:\ProgramData\DocumentScanner\reconnect.flag";
        private readonly string statusFlagPath = @"C:\ProgramData\DocumentScanner\status.txt";
        private int reconnectAttempts = 0;
        private bool isReconnecting = false; // Flag to prevent multiple concurrent reconnections

        public ScannerWatchdog(int reconnectIntervalSeconds)
        {
            try
            {
                // Δημιούργησε το directory αν δεν υπάρχει
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(reconnectFlagPath));
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to create directory: {ex.Message}");
                }

                reconnectIntervalMs = reconnectIntervalSeconds * 1000; // Convert to milliseconds
                reconnectThreshold = TimeSpan.FromSeconds(reconnectIntervalSeconds);
                lastDataReceived = DateTime.Now;

                // Δημιούργησε και ρύθμισε το timer για έλεγχο του flag file
                // Αυτό θα τρέχει ανεξάρτητα από το watchdog timer για να διασφαλίσουμε
                // ότι μπορεί να γίνει χειροκίνητη επανεκκίνηση ακόμα κι αν υπάρχει πρόβλημα
                flagCheckTimer = new System.Timers.Timer(5000); // Check every 5 seconds
                flagCheckTimer.Elapsed += SafeFlagCheckTimerCallback;
                flagCheckTimer.AutoReset = true;
                flagCheckTimer.Enabled = true;

                // Δημιούργησε και ρύθμισε το κύριο timer με καθυστέρηση έναρξης
                // για να δώσουμε στο σύστημα χρόνο να σταθεροποιηθεί
                watchdogTimer = new System.Timers.Timer(reconnectIntervalMs);
                watchdogTimer.Elapsed += SafeCheckScannerConnection;
                watchdogTimer.AutoReset = true;
                watchdogTimer.Enabled = false; // Ξεκινάει με καθυστέρηση

                logger.Info($"Scanner watchdog initialized with {reconnectIntervalSeconds} second threshold");
                logger.Info($"Reconnect flag file path: {reconnectFlagPath}");
                logger.Info($"Status file path: {statusFlagPath}");

                // Αρχικός έλεγχος για USB συσκευές
                try
                {
                    if (DeviceHelper.AreUsbDevicesAvailable())
                    {
                        logger.Info("Initial USB devices check: Available");
                    }
                    else
                    {
                        logger.Warn("Initial USB devices check: Not available");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"Error checking USB devices: {ex.Message}");
                }

                // Ξεκίνα το κύριο timer με καθυστέρηση
                System.Threading.Timer startupTimer = null;
                startupTimer = new System.Threading.Timer(state => {
                    try
                    {
                        watchdogTimer.Enabled = true;
                        logger.Info("Watchdog timer activated");
                        startupTimer?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Error starting watchdog timer: {ex.Message}");
                    }
                }, null, 10000, Timeout.Infinite); // 10 second delay
            }
            catch (Exception ex)
            {
                logger.Error($"Error in ScannerWatchdog constructor: {ex.Message}", ex);
            }
        }

        public void NotifyDataReceived()
        {
            try
            {
                // Update the last data received timestamp
                lastDataReceived = DateTime.Now;
                reconnectAttempts = 0; // Επαναφορά των προσπαθειών επανασύνδεσης
                logger.Debug("Data received notification registered");
            }
            catch (Exception ex)
            {
                logger.Error($"Error in NotifyDataReceived: {ex.Message}", ex);
            }
        }

        private void SafeCheckScannerConnection(object sender, ElapsedEventArgs e)
        {
            try
            {
                CheckScannerConnection();
            }
            catch (Exception ex)
            {
                logger.Error($"Unhandled exception in timer callback: {ex.Message}", ex);
            }
        }

        private void SafeFlagCheckTimerCallback(object sender, ElapsedEventArgs e)
        {
            try
            {
                CheckForManualReconnectFlag();
                UpdateStatusFile();
            }
            catch (Exception ex)
            {
                logger.Error($"Unhandled exception in flag check timer: {ex.Message}", ex);
            }
        }

        private void CheckScannerConnection()
        {
            try
            {
                // Αν είμαστε ήδη σε διαδικασία επανασύνδεσης, αγνοούμε την κλήση
                if (isReconnecting)
                {
                    logger.Debug("Already reconnecting, skipping check");
                    return;
                }

                // Έλεγχος αν έχει περάσει το χρονικό όριο χωρίς λήψη δεδομένων
                TimeSpan timeSinceLastData = DateTime.Now - lastDataReceived;

                // Αν δεν έχουμε λάβει δεδομένα για διάστημα μεγαλύτερο του ορίου
                if (timeSinceLastData > reconnectThreshold)
                {
                    // Σημειώνουμε ότι ξεκινάμε διαδικασία επανασύνδεσης
                    isReconnecting = true;

                    try
                    {
                        logger.Warn($"No data received from scanner for {timeSinceLastData.TotalSeconds:0,0} seconds. Reconnecting scanner...");

                        // Προσπάθησε να επαναρχικοποιήσεις τη συσκευή με ασφαλή τρόπο
                        SafeReconnect();

                        // Επαναφορά του timer
                        lastDataReceived = DateTime.Now;
                    }
                    finally
                    {
                        // Πάντα επαναφέρουμε την κατάσταση επανασύνδεσης
                        isReconnecting = false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error in CheckScannerConnection: {ex.Message}", ex);
                isReconnecting = false; // Επαναφορά σε περίπτωση εξαίρεσης
            }
        }

        // Τροποποίηση της κλάσης ScannerWatchdog - μέθοδος SafeReconnect
        private void SafeReconnect()
        {
            try
            {
                // Προσεκτική επανεκκίνηση με έλεγχο για null
                if (Service.scanner == null)
                {
                    logger.Error("Scanner object is null, attempting to recreate it");
                    try
                    {
                        // Προσπάθησε να δημιουργήσεις νέο scanner
                        Service.InitializeDevice();
                    }
                    catch (Exception initEx)
                    {
                        logger.Error($"Failed to initialize device: {initEx.Message}");
                    }
                    return;
                }

                // ΑΛΛΑΓΉ: Αντί να επανασυνδέσουμε το υπάρχον scanner αντικείμενο, 
                // το οποίο φαίνεται να προκαλεί Access Violation, επαναδημιουργούμε 
                // πλήρως τη συσκευή χωρίς να καλέσουμε Disconnect()
                try
                {
                    logger.Info("Performing full device reinitialization instead of simple reconnect");

                    // Ορίζουμε το scanner σε null χωρίς να καλέσουμε Disconnect()
                    // αυτό θα επιτρέψει στο GC να καθαρίσει τους πόρους
                    Service.scanner = null;

                    // Εκτέλεση GC για να βεβαιωθούμε ότι οι πόροι απελευθερώνονται
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    // Μικρή καθυστέρηση
                    Thread.Sleep(1000);

                    // Δημιουργία νέου αντικειμένου scanner
                    logger.Info("Creating new scanner instance");
                    Service.InitializeDevice();

                    // Επαναφορά του timer
                    lastDataReceived = DateTime.Now;

                    logger.Info("Full device reinitialization completed");
                }
                catch (Exception ex)
                {
                    logger.Error($"Error during full reinitialization: {ex.Message}");

                    // Δοκιμάζουμε εναλλακτική προσέγγιση
                    try
                    {
                        logger.Info("Attempting to create new scanner without disconnecting old one");

                        // Απευθείας δημιουργία νέου scanner χωρίς απελευθέρωση του παλιού
                        Service.scanner = new ConcreteScannerFactory().GetScanner(Service.config);
                        Service.scanner.Connect();

                        lastDataReceived = DateTime.Now;
                        logger.Info("Alternative scanner reinitialization completed");
                    }
                    catch (Exception altEx)
                    {
                        logger.Error($"Alternative approach failed: {altEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Unhandled exception in SafeReconnect: {ex.Message}", ex);
            }
        }

        private void SafeCompleteReinitialization()
        {
            try
            {
                logger.Info("Starting complete reinitialization");

                // Αύξηση του μετρητή προσπαθειών
                reconnectAttempts++;

                // Έλεγχος αν έχουμε φτάσει στο όριο προσπαθειών
                if (reconnectAttempts >= 5)
                {
                    logger.Warn($"Multiple reconnection failures ({reconnectAttempts}). Attempting service restart.");

                    try
                    {
                        // Δημιουργία του restart flag
                        string restartFlag = Path.Combine(
                            Path.GetDirectoryName(reconnectFlagPath),
                            "restart_required.flag");

                        File.WriteAllText(restartFlag, DateTime.Now.ToString());
                        logger.Info("Created restart flag file");

                        // Επαναφορά μετρητή για την επόμενη φορά
                        reconnectAttempts = 0;
                    }
                    catch (Exception flagEx)
                    {
                        logger.Error($"Failed to create restart flag: {flagEx.Message}");
                    }

                    return;
                }

                // Προσπάθησε να απελευθερώσεις τους υπάρχοντες πόρους
                if (Service.scanner != null)
                {
                    try
                    {
                        logger.Info("Disconnecting existing scanner");
                        Service.scanner.Disconnect();
                    }
                    catch (Exception disconnectEx)
                    {
                        logger.Error($"Error disconnecting scanner: {disconnectEx.Message}");
                    }
                }

                // Εκτέλεση GC για καθαρισμό πόρων
                try
                {
                    logger.Info("Running garbage collection");
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
                catch (Exception gcEx)
                {
                    logger.Error($"Error during garbage collection: {gcEx.Message}");
                }

                // Καθυστέρηση για σταθεροποίηση
                Thread.Sleep(2000);

                // Επανεκκίνηση της συσκευής εκ νέου
                try
                {
                    logger.Info("Creating new scanner instance");
                    Service.scanner = null; // Απελευθέρωση αναφοράς
                    Service.InitializeDevice();

                    logger.Info("Device reinitialization completed successfully");
                    lastDataReceived = DateTime.Now; // Επαναφορά του timer
                }
                catch (Exception initEx)
                {
                    logger.Error($"Error initializing device: {initEx.Message}");
                    throw; // Προώθηση για περαιτέρω χειρισμό
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Unhandled exception in SafeCompleteReinitialization: {ex.Message}", ex);
            }
        }

        private void CheckForManualReconnectFlag()
        {
            try
            {
                if (File.Exists(reconnectFlagPath))
                {
                    logger.Info("Manual reconnect flag detected");

                    try
                    {
                        File.Delete(reconnectFlagPath);
                    }
                    catch (Exception deleteEx)
                    {
                        logger.Error($"Failed to delete reconnect flag: {deleteEx.Message}");
                    }

                    // Προσπάθεια επανασύνδεσης
                    logger.Info("Performing manual reconnect");
                    SafeCompleteReinitialization();

                    logger.Info("Manual reconnect completed");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error checking for manual reconnect flag: {ex.Message}", ex);
            }
        }

        private void UpdateStatusFile()
        {
            try
            {
                bool scannerConnected = false;

                try
                {
                    scannerConnected = (Service.scanner != null);
                }
                catch
                {
                    // Ignore errors in status check
                }

                string status = $"Last Data: {lastDataReceived}\r\n" +
                                $"Current Time: {DateTime.Now}\r\n" +
                                $"Time Since Last Data: {(DateTime.Now - lastDataReceived).TotalSeconds:0.0} seconds\r\n" +
                                $"Reconnect Threshold: {reconnectThreshold.TotalSeconds} seconds\r\n" +
                                $"Reconnect Attempts: {reconnectAttempts}\r\n" +
                                $"Scanner Connected: {scannerConnected}";

                File.WriteAllText(statusFlagPath, status);
            }
            catch
            {
                // Αγνόηση σφαλμάτων στο status file - όχι κρίσιμη λειτουργία
            }
        }

        public void Dispose()
        {
            try
            {
                if (watchdogTimer != null)
                {
                    watchdogTimer.Enabled = false;
                    watchdogTimer.Elapsed -= SafeCheckScannerConnection;
                    watchdogTimer.Dispose();
                    watchdogTimer = null;
                }

                if (flagCheckTimer != null)
                {
                    flagCheckTimer.Enabled = false;
                    flagCheckTimer.Elapsed -= SafeFlagCheckTimerCallback;
                    flagCheckTimer.Dispose();
                    flagCheckTimer = null;
                }

                logger.Info("ScannerWatchdog disposed");
            }
            catch (Exception ex)
            {
                logger.Error($"Error disposing ScannerWatchdog: {ex.Message}", ex);
            }
        }
    }
}