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
            FaustVst.FaustVst plugin = new FaustVst.FaustVst();

            WindowsFormsHost<FaustVst.FaustVst> host = new WindowsFormsHost<FaustVst.FaustVst>(plugin);

            host.Run();
        }
    }
}
