using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TK3D_Test.Core;

namespace TK3D_Test.Revit.Factories
{
    public class RevitDataFactory
    {
        public static IList<WallType> GetWallTypes(Document doc) =>
       [.. new FilteredElementCollector(doc)
            .WhereElementIsElementType()
            .OfClass(typeof(WallType))
            .Cast<WallType>()
            .OrderBy(t => t.Name, new OrdinalStringComparer())];

        public static IList<FloorType> GetFloorTypes(Document doc) =>
            [.. new FilteredElementCollector(doc)
            .WhereElementIsElementType()
            .OfCategory(BuiltInCategory.OST_Floors)
            .OfType<FloorType>()
            .OrderBy(t => t.Name, new OrdinalStringComparer())];

        public static IList<CeilingType> GetCeilingTypes(Document doc) =>
            [.. new FilteredElementCollector(doc)
            .WhereElementIsElementType()
            .OfCategory(BuiltInCategory.OST_Ceilings)
            .OfType<CeilingType>()
            .OrderBy(t => t.Name, new OrdinalStringComparer())];

        public static IList<RevitLinkInstance> GetRevitLinkInstances(Document doc) =>
            [.. new FilteredElementCollector(doc)
            .OfClass(typeof(RevitLinkInstance))
            .Cast<RevitLinkInstance>()];

        public static List<string> GetColumnNames(Document doc)
        {
            var columnFilter = new LogicalOrFilter(
                new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns),
                new ElementCategoryFilter(BuiltInCategory.OST_Columns));

            return new FilteredElementCollector(doc)
                .WhereElementIsElementType()
                .WherePasses(columnFilter)
                .OfType<FamilySymbol>()
                .Select(fs => fs.Family.Name)
                .Distinct()
                .OrderBy(name => name, new OrdinalStringComparer())
                .ToList();
        }

        public static IList<Level> GetLevels(Document doc) =>
            [.. new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .OfClass(typeof(Level))
            .Cast<Level>()];
    }
}
