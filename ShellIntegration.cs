using System;

namespace BikeRental
{
    // Opens a folder in Windows Explorer by summoning Shell.Application from the registry,
    // like a séance but for COM objects. It works. Nobody knows exactly why.
    //
    // This class is called by nothing in the web application. It was kept from the WinForms
    // version as a monument to decisions made. It compiles. It ships. It watches from the bin folder.
    //
    // In .NET 5+ on Windows, this compiles and then fails at runtime in ways
    // that are entertaining to read about and less entertaining to debug.
    // On Linux and macOS it fails immediately and silently, which is kinder.
    internal static class ShellIntegration
    {
        // Instructs Windows Explorer to open the given folder.
        // Explorer will oblige, then do something annoying with the navigation pane.
        // Not called from anywhere. Preserved for educational purposes and nostalgia.
        public static void OpenFolder(string folderPath)
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType == null)
                return; // no shell, no problem, no folder opened, user mildly confused

            dynamic shell = Activator.CreateInstance(shellType);
            shell.Explore(folderPath);
        }
    }
}
