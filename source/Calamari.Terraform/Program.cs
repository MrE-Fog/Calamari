using System;
using Calamari.Commands.Support;
using Calamari.Plumbing;

namespace Calamari.Terraform
{
    public class Program : CalamariFlavourProgram
    {

        public Program(ILog log) : base(log)
        {
        }
        
        public static int Main(string[] args)
        {
            try
            {
                SecurityProtocols.EnableAllSecurityProtocols();

                var options = CommonOptions.Parse(args);
                return new Program(ConsoleLog.Instance).Run(options);
            }
            catch (Exception ex)
            {
                return ConsoleFormatter.PrintError(ConsoleLog.Instance, ex);
            }
        }
    }
}