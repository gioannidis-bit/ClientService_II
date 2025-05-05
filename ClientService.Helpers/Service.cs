// ClientService.Helpers/Service.cs
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using ClientService.Classes.Devices.AccessIS;
using ClientService.Classes.Factories;
using ClientService.Classes.Interfaces;
using ClientService.Models.Base;
using log4net;

namespace ClientService.Helpers
{
    // ClientService.Helpers/Service.cs - �����������
    internal class Service : ServiceBase
    {
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private string ExecName;

        // ������ ��� private �� public static
        public static string SERVICENAME;

        public static ConfigurationModel config;

        public static IScanner scanner;

        // �������� ��� watchdog
        public static ScannerWatchdog watchdog;

        private IContainer components;

        public Service(string ExecName, string SERVICENAME)
        {
            string exName = Process.GetCurrentProcess().ProcessName;
            logger.Info(exName);
            if (!string.IsNullOrEmpty(exName))
            {
                this.ExecName = ExecName;
                Service.SERVICENAME = SERVICENAME; // ��������� ��� static ������
            }
            base.ServiceName = SERVICENAME;
        }

         protected override void OnStart(string[] args)
        {
            Start(args);
        }

        protected override void OnStop()
        {
            Stop();
        }

        private System.Threading.Timer usbMonitorTimer;


        public void Start(string[] args)
        {
            logger.Info("Initializing Protel Document Scanner");
            InitializeConfiguration();
            InitializeDevice();

            // ������ �������������� USB �������� ���� 10 ������������
            usbMonitorTimer = new System.Threading.Timer(CheckScannerConnection, null, 10000, 10000);

            logger.Info("Scanner monitoring system initialized");
        }

        private void CheckScannerConnection(object state)
        {
            try
            {
                // ������� �� � scanner ����� ������������
                bool isConnected = false;

                if (scanner != null)
                {
                    // ����� ��� ���� ������� IsConnected
                    isConnected = ((AccessISScanner)scanner).IsConnected();
                }

                if (!isConnected)
                {
                    logger.Warn("Scanner disconnected - attempting to reinitialize");
                    // ������������ ��� ��������
                    InitializeDevice();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error checking scanner connection: {ex.Message}");
            }
        }

        // ��� ������� ��� ������ �������������
        private void CheckForServiceRestart()
        {
            try
            {
                string restartFlagPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "DocumentScanner", "reconnect.flag")),
                    "restart_occurred.flag");

                if (System.IO.File.Exists(restartFlagPath))
                {
                    string restartTime = System.IO.File.ReadAllText(restartFlagPath);
                    logger.Info($"Service was automatically restarted at {restartTime}");

                    // �������� ��� flag �������
                    try
                    {
                        System.IO.File.Delete(restartFlagPath);
                    }
                    catch
                    {
                        // ������� ���������
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error checking for service restart: {ex.Message}", ex);
            }
        }

        public new void Stop()
        {
            // ����������� ��� watchdog
            if (watchdog != null)
            {
                logger.Info("Disposing scanner watchdog");
                watchdog.Dispose();
                watchdog = null;
            }

            if (!Environment.UserInteractive)
            {
                logger.Info("Application stopped as Windows Service");
            }
            else
            {
                logger.Info("Application closed as Console Application");
            }
        }

        public static void InitializeConfiguration()
        {
            logger.Info("Initializing Configuration");
            config = new ConfigurationHelper().GetConfiguration();
        }

        // ����������� ��� ������� InitializeDevice ��� Service.cs
        public static void InitializeDevice()
        {
            logger.Info("Initializing Device");

            try
            {
                // ������� ��� USB ��������
                bool usbDevicesAvailable = false;
                try
                {
                    usbDevicesAvailable = DeviceHelper.AreUsbDevicesAvailable();
                    logger.Info($"USB devices available: {usbDevicesAvailable}");
                }
                catch (Exception usbEx)
                {
                    logger.Error($"Error checking USB devices: {usbEx.Message}");
                }

                // ���������� ��� scanner �� safe try/catch
                try
                {
                    scanner = new ConcreteScannerFactory().GetScanner(config);
                }
                catch (Exception scannerEx)
                {
                    logger.Error($"Error creating scanner: {scannerEx.Message}");
                    throw; // �������� ��� ���������� ��� �����������
                }

                // ���������� �������� �� safe try/catch
                try
                {
                    scanner.Connect();
                }
                catch (Exception connectEx)
                {
                    logger.Error($"Error connecting scanner: {connectEx.Message}");
                    throw; // �������� ��� ���������� ��� �����������
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Critical error initializing device: {ex.Message}", ex);
                throw; // �������� ��� ��������� ��� ���������� ��� �����������
            }
        }

        // �������� ��� Service.cs
        // ����������� ��� ������� CompleteDeviceReinitialization ��� Service.cs
        public static void CompleteDeviceReinitialization()
        {
            logger.Info("Performing complete device reinitialization");

            try
            {
                // ������������ ��� ����� ��� ��������� scanner
                if (scanner != null)
                {
                    try
                    {
                        logger.Info("Disconnecting existing scanner");
                        scanner.Disconnect();
                    }
                    catch (Exception disconnectEx)
                    {
                        logger.Error($"Error disconnecting scanner: {disconnectEx.Message}");
                        // ����������� ���� �� ������
                    }

                    // ������� �� null �� scanner ��� �� ���������� �� GC
                    scanner = null;
                }

                // ������� ��������� ��� GC ���� �� �������������� �� �����
                try
                {
                    logger.Info("Forcing resource cleanup");
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
                catch (Exception gcEx)
                {
                    logger.Error($"Error during garbage collection: {gcEx.Message}");
                    // ����������� ���� �� ������
                }

                // ����������� ����������� ��� �� ������� �������� ��� �� ����� ���������������
                Thread.Sleep(2000);

                // �� ���� ���������� ��� scanner �� ��� ���� ����� ��� ������� ���� ��� ��������
                try
                {
                    logger.Info("Creating new scanner instance");
                    scanner = new ConcreteScannerFactory().GetScanner(config);

                    // ������� �� �� �������
                    logger.Info("Connecting to device");
                    scanner.Connect();

                    logger.Info("Device reinitialization completed successfully");
                }
                catch (Exception initEx)
                {
                    logger.Error($"Error during scanner initialization: {initEx.Message}");
                    throw; // �������� ��� ��������� ��� ��������� ����������
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error during device reinitialization: {ex.Message}", ex);
                throw; // �������� ��� ��������� ��� ��������� ����������
            }
        }


        // �������� ������� ��� ������������ ��� scanner
        public static void ForceReconnectScanner()
        {
            try
            {
                logger.Info("Manual scanner reconnection requested");
                scanner.Disconnect();
                System.Threading.Thread.Sleep(1000);
                scanner.Connect();
                watchdog?.NotifyDataReceived();
                logger.Info("Manual scanner reconnection completed");
            }
            catch (Exception ex)
            {
                logger.Error($"Error during manual scanner reconnection: {ex.Message}", ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new Container();
            base.ServiceName = "Service";
        }
    }
}