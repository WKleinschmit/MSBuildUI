using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace MSBuildUI
{
    public static class OokiDialogsExtensions
    {
        public static void AddFilter(this VistaFileDialog vfd, string displayName, string pattern)
        {
            string filter = $@"{displayName}|{pattern}";
            if (string.IsNullOrEmpty(vfd.Filter))
                vfd.Filter = filter;
            else
                vfd.Filter += $@"|{filter}";
        }

        public static void AddFilter(this VistaFileDialog vfd, string extension)
        {
            if (!extension.StartsWith("."))
                extension = "." + extension;

            using (RegistryKey hkExt = Registry.ClassesRoot.OpenSubKey(extension))
            {
                if (hkExt == null || !(hkExt.GetValue(null) is string identifier))
                    return;

                using (RegistryKey hkIdentifier = Registry.ClassesRoot.OpenSubKey(identifier))
                {
                    if (hkIdentifier == null || !(hkIdentifier.GetValue(null) is string displayName))
                        return;

                    vfd.AddFilter(displayName, $"*{extension}");
                }
            }
        }
    }
}
