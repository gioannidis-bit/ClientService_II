// ClientService.Helpers/PowerManagementHelper.cs
using System;
using System.Runtime.InteropServices;
using System.Reflection;
using log4net;

namespace ClientService.Helpers
{
    public class PowerManagementHelper
    {
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // Windows power request constants
        private const uint POWER_REQUEST_CONTEXT_VERSION = 0;
        private const uint POWER_REQUEST_CONTEXT_SIMPLE_STRING = 0x1;
        private const uint POWERREQUEST_DISPLAY_REQUIRED = 0x1;
        private const uint POWERREQUEST_SYSTEM_REQUIRED = 0x2;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct POWER_REQUEST_CONTEXT
        {
            public uint Version;
            public uint Flags;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string SimpleReasonString;
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr PowerCreateRequest(ref POWER_REQUEST_CONTEXT Context);

        [DllImport("kernel32.dll")]
        private static extern bool PowerSetRequest(IntPtr PowerRequestHandle, uint RequestType);

        [DllImport("kernel32.dll")]
        private static extern bool PowerClearRequest(IntPtr PowerRequestHandle, uint RequestType);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private IntPtr powerRequestHandle = IntPtr.Zero;

        public bool PreventSystemSleep()
        {
            try
            {
                if (powerRequestHandle != IntPtr.Zero)
                {
                    logger.Info("Power request already active");
                    return true;
                }

                logger.Info("Setting up power request to prevent system sleep");

                POWER_REQUEST_CONTEXT powerRequestContext = new POWER_REQUEST_CONTEXT
                {
                    Version = POWER_REQUEST_CONTEXT_VERSION,
                    Flags = POWER_REQUEST_CONTEXT_SIMPLE_STRING,
                    SimpleReasonString = "Protel Document Scanner Service - Prevent sleep to maintain USB scanner connectivity"
                };

                powerRequestHandle = PowerCreateRequest(ref powerRequestContext);

                if (powerRequestHandle == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    logger.Error($"Failed to create power request. Error code: {error}");
                    return false;
                }

                // Ενεργοποίηση του αιτήματος διατήρησης ισχύος συστήματος
                bool systemResult = PowerSetRequest(powerRequestHandle, POWERREQUEST_SYSTEM_REQUIRED);

                // Ενεργοποίηση του αιτήματος διατήρησης οθόνης
                bool displayResult = PowerSetRequest(powerRequestHandle, POWERREQUEST_DISPLAY_REQUIRED);

                if (!systemResult || !displayResult)
                {
                    int error = Marshal.GetLastWin32Error();
                    logger.Error($"Failed to set power request. Error code: {error}");

                    // Καθαρισμός πόρων σε περίπτωση αποτυχίας
                    if (powerRequestHandle != IntPtr.Zero)
                    {
                        CloseHandle(powerRequestHandle);
                        powerRequestHandle = IntPtr.Zero;
                    }

                    return false;
                }

                logger.Info("Power request set successfully. System will not enter sleep mode while USB scanner is active.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"Error setting up power request: {ex.Message}", ex);

                // Καθαρισμός πόρων σε περίπτωση εξαίρεσης
                if (powerRequestHandle != IntPtr.Zero)
                {
                    CloseHandle(powerRequestHandle);
                    powerRequestHandle = IntPtr.Zero;
                }

                return false;
            }
        }

        public bool AllowSystemSleep()
        {
            try
            {
                if (powerRequestHandle == IntPtr.Zero)
                {
                    logger.Info("No active power request to clear");
                    return true;
                }

                logger.Info("Clearing power request to allow system sleep");

                // Απενεργοποίηση του αιτήματος διατήρησης ισχύος συστήματος
                bool systemResult = PowerClearRequest(powerRequestHandle, POWERREQUEST_SYSTEM_REQUIRED);

                // Απενεργοποίηση του αιτήματος διατήρησης οθόνης
                bool displayResult = PowerClearRequest(powerRequestHandle, POWERREQUEST_DISPLAY_REQUIRED);

                // Κλείσιμο του handle
                bool closeResult = CloseHandle(powerRequestHandle);
                powerRequestHandle = IntPtr.Zero;

                if (!systemResult || !displayResult || !closeResult)
                {
                    int error = Marshal.GetLastWin32Error();
                    logger.Error($"Failed to clear power request. Error code: {error}");
                    return false;
                }

                logger.Info("Power request cleared successfully. System can enter sleep mode.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"Error clearing power request: {ex.Message}", ex);

                // Προσπαθούμε να καθαρίσουμε τους πόρους σε περίπτωση εξαίρεσης
                if (powerRequestHandle != IntPtr.Zero)
                {
                    CloseHandle(powerRequestHandle);
                    powerRequestHandle = IntPtr.Zero;
                }

                return false;
            }
        }
    }
}