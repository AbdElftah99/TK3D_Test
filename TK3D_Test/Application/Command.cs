using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TK3D_Test.MVVM.View;
using TK3D_Test.MVVM.ViewModel;
using TK3D_Test.Revit;

namespace TK3D_Test.Application
{
    [Transaction(TransactionMode.Manual)]
    public class Command : ExternalCommand
    {
        public override void Execute()
        {
            var viewModel   = new MainViewModel(Document);
            var view        = new MainView(viewModel);
            view.ShowDialog();
            if (view.DialogResult == true)
            {
                RevitRoomReader rvtReader       = new(Document);
                ElementCreator elementCreator   = new(Document);

                elementCreator.GenerateFloors(rvtReader, Document);
                elementCreator.GenerateCeilings(rvtReader, Document, Convert.ToDouble(viewModel.CeilingHeight));
                elementCreator.CreateWallFinishing(rvtReader, Document, viewModel.SelectedWallFinishType);

                TaskDialog.Show("Result", $"Elements created Successfully.");
            }
        }
    }
}
