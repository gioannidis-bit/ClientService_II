// ����������� ��� Service.cs
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using ClientService.Classes.Factories;
using ClientService.Classes.Interfaces;
using ClientService.Models.Base;
using log4net;

namespace ClientService.Helpers
{
    internal class Service : ServiceBase
    {
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private string ExecName;

        public static string SERVICENAME;

        public static ConfigurationModel config;

        public static IScanner scanner;

        private Timer usbMonitorTimer;
        private Timer flagCheckTimer;

        private readonly string reconnectFlagPath = @"C:\ProgramData\DocumentScanner\reconnect.flag";

        private IContainer components;

        public Service(string ExecName, string SERVICENAME)
        {
            string exName = Process.GetCurrentProcess().ProcessName;
            logger.Info(exName);
            if (!string.IsNullOrEmpty(exName))
            {
                this.ExecName = ExecName;
                Service.SERVICENAME = SERVICENAME;
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

        public void Start(string[] args)
        {
            logger.Info("Initializing Protel Document Scanner");
            InitializeConfiguration();
            InitializeDevice();

            // ���������� ������� ��� �� flag files
            try
            {
                string flagsPath = Path.GetDirectoryName(reconnectFlagPath);
                if (!Directory.Exists(flagsPath))
                    Directory.CreateDirectory(flagsPath);
            }
            catch (Exception ex)
            {
                logger.Error($"Error creating flags directory: {ex.Message}");
            }

            // �������� ���� timer ��� �� ������� �� ������ ��� flag file
            // ��� ����������� ������������
            flagCheckTimer = new Timer(CheckForManualReconnect, null, 5000, 5000);

            // �������� ���� timer ��� �� ������� ��������� �� � scanner ����� ������������
            usbMonitorTimer = new Timer(CheckScannerConnection, null, 10000, 10000);
           
            logger.Info("Scanner manual reconnect system initialized");

            DeviceHelper.StartDeviceWatcher();
        }

        public new void Stop()
        {
            // ���������� ���� timers
            if (usbMonitorTimer != null)
            {
                usbMonitorTimer.Dispose();
                usbMonitorTimer = null;
            }

            if (flagCheckTimer != null)
            {
                flagCheckTimer.Dispose();
                flagCheckTimer = null;
            }

            if (!Environment.UserInteractive)
            {
                logger.Info("Application stopped as Windows Service");
            }
            else
            {
                logger.Info("Application closed as Console Application");
            }
            DeviceHelper.StopDeviceWatcher();
        }

        public static void InitializeConfiguration()
        {
            logger.Info("Initializing Configuration");
            config = new ConfigurationHelper().GetConfiguration();
        }

        public static void InitializeDevice()
        {
            logger.Info("Initializing Device");

            // ������� ��� USB ��������
            bool usbDevicesAvailable = false;
            try
            {
                usbDevicesAvailable = DeviceHelper.AreUsbDevicesAvailable();
                logger.Info($"USB devices available: {usbDevicesAvailable}");
            }
            catch (Exception ex)
            {
                logger.Error($"Error checking USB devices: {ex.Message}");
            }

            // ���������� ��� scanner ��� �������
            try
            {
                scanner = new ConcreteScannerFactory().GetScanner(config);
                scanner.Connect();
            }
            catch (Exception ex)
            {
                logger.Error($"Error initializing scanner: {ex.Message}");
            }
        }

        private void CheckScannerConnection(object state)
        {
            try
            {
                // ������� �� � scanner ����� ������������
                bool isConnected = false;

                if (scanner != null)
                {
                    // ������� ���������� ��������
                    if (scanner is ClientService.Classes.Devices.AccessIS.AccessISScanner)
                    {
                        isConnected = ((ClientService.Classes.Devices.AccessIS.AccessISScanner)scanner).IsConnected();
                    }
                    else
                    {
                        // ������������ ������� �� ��� ����� AccessIS scanner
                        isConnected = DeviceHelper.IsScannerConnected();
                    }
                }

                if (!isConnected)
                {
                    logger.Warn("Scanner disconnected - attempting to reinitialize");

                    // ������ ����������������
                    CompleteDeviceReinitialization();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error checking scanner connection: {ex.Message}");
            }
        }

        private void CheckForManualReconnect(object state)
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
                    catch (Exception ex)
                    {
                        logger.Error($"Failed to delete reconnect flag: {ex.Message}");
                    }

                    // ������ ���������������� ��� ��������
                    CompleteDeviceReinitialization();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error in flag check timer: {ex.Message}");
            }
        }

        public static void CompleteDeviceReinitialization()
        {
            logger.Info("Performing complete device reinitialization");

            try
            {
                // ������������ ��� ��������� scanner
                if (scanner != null)
                {
                    try
                    {
                        scanner.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Error disconnecting scanner: {ex.Message}");
                    }

                    // �������� �� scanner �� null ��� �� ���������� �� GC
                    scanner = null;
                }

                // �������� GC ��� �� ����������� ���� ������
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // ����������� ��� �������������
                Thread.Sleep(2000);

                // ���������� ���� scanner
                logger.Info("Creating new scanner instance");
                InitializeDevice();

                logger.Info("Device reinitialization completed successfully");
            }
            catch (Exception ex)
            {
                logger.Error($"Error during device reinitialization: {ex.Message}", ex);
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