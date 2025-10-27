using Autodesk.Revit.UI;
using Autodesk.Windows;
using Nice3point.Revit.Toolkit.External;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TK3D_Test.Revit;
using RibbonPanel = Autodesk.Revit.UI.RibbonPanel;

namespace TK3D_Test.Application
{
    public class Application : ExternalApplication
    {
        public static string pluginName { get; set; }
        public static UIControlledApplication UiControlledApp { get; private set; }
        public override void OnStartup()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var requestedAssembly = new AssemblyName(args.Name).Name + ".dll";
                var addinFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var assemblyPath = Path.Combine(addinFolder, requestedAssembly);

                if (System.IO.File.Exists(assemblyPath))
                {
                    return Assembly.LoadFrom(assemblyPath);
                }
                return null; // Return null if assembly is not found
            };

            UiControlledApp = Application;
            pluginName = Application.ActiveAddInId.GetAddInName();



            var testTabName = "TurnKey 3D";
            RibbonPanel testPanel = null;

            try
            {
                HelperRevitUI.AddRibbonTab(Application, testTabName);
            }
            catch
            {
                // Tab might already exist
            }

            testPanel = HelperRevitUI.AddRibbonPanel(Application, testTabName, "Architectural");

            BitmapImage bitmapImage = new BitmapImage(new Uri("pack://application:,,,/TK3D_Test;component/Resources/house.png"));
            PushButtonData testPushButtonData = new("Elemnts Creation", "Elemnts Creation", Assembly.GetExecutingAssembly().Location, typeof(Command).FullName)
            {
                LargeImage = bitmapImage
            };
            PushButton testPushButton = testPanel.AddItem(testPushButtonData) as PushButton;
        }
    }
}
