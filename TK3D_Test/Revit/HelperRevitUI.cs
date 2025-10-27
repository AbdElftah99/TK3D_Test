using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TK3D_Test.Revit
{
    public static class HelperRevitUI
    {
        public static void AddRibbonTab(UIControlledApplication application, string TabName)
        {
            application.CreateRibbonTab(TabName);
        }
        public static RibbonPanel AddRibbonPanel(UIControlledApplication application, string TabName, string panelName)
        {
            return application.CreateRibbonPanel(TabName, panelName);
        }
        public static PushButton AddPushButton(RibbonPanel panel, string name, string title, Type targetClass, string path)
        {
            PushButton button = panel.AddItem(new PushButtonData(name, title, path, targetClass.FullName)) as PushButton;
            return button;
        }
    }
}
