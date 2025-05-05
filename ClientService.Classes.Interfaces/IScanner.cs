namespace ClientService.Classes.Interfaces
{
    public interface IScanner
    {
        int Connect();
        string Send(string data);
        int Disconnect();
        bool IsConnected(); // Νέα μέθοδος
    }
}