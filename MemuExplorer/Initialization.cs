using System.Diagnostics;

namespace MemuExplorer
{
    public class Initialization
    {
        public string NodePath { get; set; }

        public Initialization(string nodePath= @"c:/Program Files/nodejs/")
        {
            NodePath = nodePath;
        }

        public void InstallElectron()
        {
            Process installElectron = new Process();
            installElectron.StartInfo = new ProcessStartInfo(NodePath+ @"\npm.cmd");
            installElectron.StartInfo.Arguments = "install electron -packager -g";
            installElectron.Start();
            installElectron.WaitForExit();
        }

        public void InstallAppium()
        {
            Process installAppium = new Process();
            installAppium.StartInfo = new ProcessStartInfo(NodePath + @"\npm.cmd");
            installAppium.StartInfo.Arguments = "install -g appium";
            installAppium.Start();
            installAppium.WaitForExit();
        }

        public void NpmCommand(string command)
        {
            Process installAppium = new Process();
            installAppium.StartInfo = new ProcessStartInfo(NodePath + @"\npm.cmd");
            installAppium.StartInfo.Arguments = command;
            installAppium.Start();
            installAppium.WaitForExit();
        }

    }
}
