// ClientService.Classes.Devices.IDBox/IDBoxCMD.cs
using System;
using System.Reflection;
using ClientService.Helpers;
using ClientService.Models.Base;
using log4net;

namespace ClientService.Classes.Devices.IDBox
{
    public class IDBoxCMD : IDisposable
    {
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ConfigurationModel config;

        private Scanner m_scanner;

        private DelegateReadMRZ m_delegateReadMRZ;

        public Func<string, string> SetText { get; set; }

        public IDBoxCMD(ConfigurationModel config)
        {
            this.config = config;
        }

        public void Initialise()
        {
            try
            {
                Release();
                m_delegateReadMRZ = readMRZ;
                m_scanner = new Scanner(m_delegateReadMRZ);
                ConnectToIdBox(config.ComPort);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }

        private void ConnectToIdBox(string portName)
        {
            bool is_connected = false;
            try
            {
                is_connected = false;
                if (m_scanner.Connect(portName, CommunicationMode.UART_9600))
                {
                    if (m_scanner.GetVersion() != "")
                    {
                        is_connected = true;
                    }
                    else
                    {
                        m_scanner.Disconnect();
                        if (m_scanner.Connect(portName, CommunicationMode.UART_115200))
                        {
                            if (m_scanner.GetVersion() != "")
                            {
                                is_connected = true;
                            }
                            else
                            {
                                m_scanner.Disconnect();
                            }
                        }
                    }
                }
                if (is_connected)
                {
                    m_scanner.ContinuousReading = true;
                    // Notify watchdog on successful connection
                    Service.watchdog?.NotifyDataReceived();
                }
            }
            catch
            {
                if (m_scanner != null)
                {
                    m_scanner.Disconnect();
                }
            }
        }

        // ClientService.Classes.Devices.IDBox/IDBoxCMD.cs - στη μέθοδο readMRZ
        private void readMRZ(string mrz)
        {
            logger.Info("IDBox listener triggered");
            if (mrz != null && mrz.EndsWith("\r\n\r\n"))
            {
                mrz = mrz.Substring(0, mrz.Length - 4);
            }
            // Ενημέρωση του watchdog για τη λήψη δεδομένων
            Service.watchdog?.NotifyDataReceived();
            SetText(mrz);
        }
        public void Release()
        {
            if (m_scanner != null && m_scanner.IsConnected())
            {
                m_scanner.Disconnect();
            }
        }

        public void Dispose()
        {
            Release();
        }
    }
}