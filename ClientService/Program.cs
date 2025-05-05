using System;
using System.Reflection;
using ClientService.Helpers;
using log4net;

namespace ClientService;

internal class Program
{
	private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	[STAThread]
    private static void Main(string[] args)
    {
        try
        {
            // Έλεγχος αν είμαστε σε Debug mode
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // ’μεση εκκίνηση του service όπως θα εκτελούνταν κανονικά
                Service service = new Service("DocumentScanner", "DocumentScanner");
                service.Start(args);

                // Κράτα το πρόγραμμα ανοιχτό για debugging
                Console.WriteLine("Press any key to stop service...");
                Console.ReadKey();

                // Σταμάτα το service
                service.Stop();
            }
            else
            {
                // Κανονική εκτέλεση
                new MainCode().Main();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ReadKey(); // Κράτα το παράθυρο ανοιχτό για να δεις το error
        }
    }
}
