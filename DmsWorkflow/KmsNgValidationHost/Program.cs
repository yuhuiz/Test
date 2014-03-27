using System;

namespace KmsNgWorkflow
{
    /// <summary>
    /// Main application class
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">Supplied arguments</param>
        [MTAThread]
        public static void Main(string[] args)
        {
            Microsoft.Online.Management.WorkflowContracts.Runner.Program.Main(args);
        }
    }
}
