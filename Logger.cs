namespace PurpleLemonPhotography.WebsiteOptimizer
{
    using System;

    public class Logger
    {
        public Logger(bool debugMessages)
        {
            this.InDebugMode = debugMessages;
        }

        public bool InDebugMode { get; set; }

        public void LogDebugMessage(string format, params object[] arg)
        {
            if (!this.InDebugMode)
            {
                return;
            }

            Console.WriteLine("****************************************");
            this.LogMessage(format, arg);
            Console.WriteLine();
        }

        public void LogMessage(string format, params object[] arg)
        {
            Console.WriteLine(format, arg);
        }
    }
}