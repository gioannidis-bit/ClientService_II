using System;
using System.Reflection;
using System.Threading;
using ClientService.Classes.Factories;
using ClientService.Classes.Interfaces;
using ClientService.Helpers;
using ClientService.Models.Base;
using log4net;

namespace ClientService.Classes.Devices.AccessIS;

public class AccessISScanner : IScanner
{
    private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    public AccessISCMD device;

    public IConnection connection;

    private ConfigurationModel config;

    public AccessISScanner(ConfigurationModel config)
    {
        this.config = config;
        device = new AccessISCMD();
        device.SetText = Send;
    }

   
    public bool IsConnected()
    {
        try
        {
            // Βασικός έλεγχος - είναι το αντικείμενο device αρχικοποιημένο;
            if (device == null)
            {
                return false;
            }

            // Χρήση της μεθόδου της συσκευής για έλεγχο σύνδεσης
            return device.IsDeviceConnected();
        }
        catch (Exception ex)
        {
            logger.Error($"Error checking scanner connection: {ex.Message}");
            return false;
        }
    }

    public int Connect()
    {
        try
        {
            device.Initialise();
            logger.Info("AccessIS Scanner Connected");
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message);
        }
        return 0;
    }

    public int Disconnect()
    {
        try
        {
            device.Release();
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message);
        }
        return 0;
    }

    public void CreateConnection()
    {
        ConnectionFactory factory = new ConcreteConnectionFactory();
        connection = factory.GetConnection(config);
    }

    // Τροποποίηση στην κλάση AccessISScanner.cs - μέθοδος Reconnect
    public void Reconnect()
    {
        try
        {
            if (device != null)
            {
                logger.Info("Disposing old device");
                device.Dispose();
                device = null;
            }

            // Επιπρόσθετη καθυστέρηση για εξασφάλιση απελευθέρωσης πόρων
            Thread.Sleep(500);

            // Βεβαιώνουμε ότι έχουμε καθαρίσει την παλιά συσκευή πριν δημιουργήσουμε νέα
            GC.Collect();
            GC.WaitForPendingFinalizers();

            logger.Info("Creating new device instance");
            device = new AccessISCMD();
            if (device != null)
            {
                device.SetText = Send;
                device.Initialise();
                logger.Info("Device successfully reinitialized");
            }
            else
            {
                logger.Error("Failed to create new device instance");
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error during reconnect: {ex.Message}", ex);
        }
    }

    // Τροποποίηση στην κλάση AccessISScanner.cs - μέθοδος Send
    public string Send(string data)
    {
        if (config.Debug)
        {
            logger.Info("Sending Data: " + data);
        }

        try
        {
            if (string.IsNullOrEmpty(data))
            {
                logger.Warn("Attempted to send null or empty data");
                return string.Empty;
            }

            // Έλεγχος εάν το αντικείμενο device είναι null
            if (device == null)
            {
                logger.Error("Device object is null during Send operation");
                return string.Empty;
            }

            if (data.ToString().StartsWith("C") && !data.ToString().ToLower().StartsWith("c<ita") && !data.ToString().ToLower().StartsWith("ca"))
            {
                try
                {
                    new CardToCloudHelper().SendToCloud(data, config.RowSeparator, config.PostToCloudDelay ?? 20);
                }
                catch (Exception cloudEx)
                {
                    logger.Error($"Error sending to cloud: {cloudEx.Message}");
                }
            }
            else
            {
                try
                {
                    CreateConnection();
                    if (connection != null)
                    {
                        string sendResult = connection.Send((int)config.RowSeparator + data);
                        logger.Debug($"Send operation result: {sendResult}");
                    }
                    else
                    {
                        logger.Error("Connection object is null");
                    }
                }
                catch (Exception connEx)
                {
                    logger.Error($"Error during connection send: {connEx.Message}");
                }

                try
                {
                    Reconnect();
                }
                catch (Exception reconnectEx)
                {
                    logger.Error($"Error during reconnect: {reconnectEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Exception in Send method: {ex.Message}", ex);
        }
        finally
        {
            if (connection != null)
            {
                try
                {
                    connection.Dispose();
                }
                catch (Exception disposeEx)
                {
                    logger.Error($"Error disposing connection: {disposeEx.Message}");
                }
                finally
                {
                    connection = null;
                }
            }
        }

        return string.Empty;
    }
}