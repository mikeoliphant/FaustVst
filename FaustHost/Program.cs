using System;
using AudioPlugSharpHost;
using FaustVst;

namespace FaustHost
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            FaustPlugin plugin = new FaustPlugin();

            WindowsFormsHost<FaustPlugin> host = new WindowsFormsHost<FaustPlugin>(plugin);

            host.Run();
        }
    }
}
