using System;
using System.Threading;

namespace log4net.Azure.console
{
    internal static class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static void Main()
        {
            Log.Info("test");
            try
            {
                throw new Exception("throw a message!");
            }
            catch (Exception ex)
            {
                Log.Error("Test exception", ex);
            }
            for (int i = 0; i < 2; i++)
            {
                Thread.Sleep(1000);
                Console.WriteLine("Waiting {0}", i);
            }
        }
    }
}
