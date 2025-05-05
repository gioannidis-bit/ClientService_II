// ClientService.Classes.Devices.AccessIS/AccessISCMD.cs
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ClientService.Helpers;
using log4net;

namespace ClientService.Classes.Devices.AccessIS
{
    public class AccessISCMD : IDisposable
    {
        private delegate void updateTextBox(string text);

        private delegate void updateDisplayMrz(int status);

        private delegate void updatedReaderButtonStatus(bool enableReader, bool disableReader);

        private delegate void disableButtons();

        private delegate void msrDelegate(ref uint Parameter, [MarshalAs(UnmanagedType.LPStr)] StringBuilder data, int dataSize);

        private delegate void msrConnectionDelegate(ref uint Parameter, bool connectionStatus);

        private enum PacketType
        {
            MRZ,
            MSR
        }

        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private msrDelegate msrData;

        private msrConnectionDelegate msrDataConnection;

        private const string DLL_LOCATION = "Access_IS_MSR.dll";

        public Func<string, string> SetText { get; set; }

        [DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern void initialiseMsr(bool managedCode);

        [DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern void msrRelease();

        [DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern bool enableMSR();

        [DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern bool disableMSR();

        [DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern PacketType getPacketType();

        [DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        private static extern string getDeviceName();

        [DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern int getMrzFailureStatus();

        [DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern bool registerMSRCallback(msrDelegate Callback, ref uint Parameter);

        [DllImport("Access_IS_MSR.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern bool registerMSRConnectionCallback(msrConnectionDelegate Callback, ref uint Parameter);



        public bool IsDeviceConnected()
        {
            try
            {
                string deviceName = getDeviceName();
                return !string.IsNullOrEmpty(deviceName);
            }
            catch (Exception ex)
            {
                // If an exception occurs when trying to get the device name,
                // the device is likely not connected
                return false;
            }
        }



        public void Initialise()
        {
            try
            {
                uint Val = 0u;
                initialiseMsr(managedCode: true);
                msrData = MsrCallback;
                msrDataConnection = MsrConnectionCallback;
                registerMSRCallback(msrData, ref Val);
                registerMSRConnectionCallback(msrDataConnection, ref Val);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }

        public void Release()
        {
            uint Val = 0u;
            registerMSRCallback(null, ref Val);
            registerMSRConnectionCallback(null, ref Val);
            msrData = null;
            msrDataConnection = null;
        }

        // ClientService.Classes.Devices.AccessIS/AccessISCMD.cs - στη μέθοδο MsrCallback
        // Τροποποίηση στην κλάση AccessISCMD.cs
        // Ασφαλέστερη έκδοση του MsrCallback στο AccessISCMD.cs
        // Τροποποίηση του MsrCallback στο AccessISCMD.cs
        private void MsrCallback(ref uint Parameter, [MarshalAs(UnmanagedType.LPStr)] StringBuilder data, int dataSize)
        {
            try
            {
                if (data == null)
                {
                    logger.Warn("AccessIS callback received null data");
                    return;
                }

                // Δημιουργία αντιγράφου των δεδομένων για ασφάλεια
                string safeDataCopy = null;
                try
                {
                    safeDataCopy = data.ToString();
                }
                catch (Exception copyEx)
                {
                    logger.Error($"Error copying data from scanner: {copyEx.Message}");
                    return;
                }

                logger.Info("AccessIS listener triggered");

                // Έλεγχος για null στο callback delegate
                if (SetText == null)
                {
                    logger.Error("SetText callback is null");
                    return;
                }

                // Κλήση του callback με safe try/catch
                try
                {
                    SetText(safeDataCopy);
                }
                catch (Exception setTextEx)
                {
                    logger.Error($"Error in SetText callback: {setTextEx.Message}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Unhandled exception in MsrCallback: {ex.Message}", ex);
            }
        }

        private void MsrConnectionCallback(ref uint Parameter, bool connectionStatus)
        {
            try
            {
                logger.Info($"AccessIS connection status changed: {connectionStatus}");
            }
            catch (Exception ex)
            {
                logger.Error($"Exception in MsrConnectionCallback: {ex.Message}", ex);
            }
        }

        // Add to AccessISCMD.cs
        public bool CheckConnection()
        {
            try
            {
                string deviceName = getDeviceName();
                return !string.IsNullOrEmpty(deviceName);
            }
            catch
            {
                return false;
            }
        }


        public void Dispose()
        {
            Release();
        }

        ~AccessISCMD()
        {
        }
    }
}