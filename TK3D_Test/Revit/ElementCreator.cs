using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TK3D_Test.Core;
using TK3D_Test.Revit.Extensions;
using TK3D_Test.Revit.Models;
using Document = Autodesk.Revit.DB.Document;
using View = Autodesk.Revit.DB.View;

namespace TK3D_Test.Revit
{
    internal class ElementCreator
    {
        private Transaction transaction { get; set; }
        // ttaneff: Public getter in order to use Extension Methods outside main class body
        public Transaction GetTranscation() => transaction;

        public ElementCreator(Document doc)
        {
            this.transaction = new Transaction(doc, "Create Model Elements");
        }

        public void GenerateFloors(RevitRoomReader rvtReader, Document doc)
        {
            transaction.Start();
            transaction.SetName("Create Floors");

            var ActivePlanView = doc.ActiveView as ViewPlan;

            try
            {
                foreach (string key in rvtReader.roomContours.Keys)
                {
                    try
                    {
                        var FloorTypeId = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Floors).WhereElementIsElementType().FirstElementId();
                        IList<CurveLoop> curveLoops = rvtReader.roomContours[key];

                        IList<CurveLoop> modifiedCurveLoops = [];
                        foreach (CurveLoop loop in curveLoops)
                        {
                            CurveLoop newLoop = null;
                            if (loop.IsOpen())
                            {
                                newLoop = loop.RebuildAndCloseCurveLoop();
                                modifiedCurveLoops.Add(newLoop);
                            }
                            else
                            {
                                modifiedCurveLoops.Add(loop);
                            }
                        }

                        Floor.Create(doc, modifiedCurveLoops, FloorTypeId, ActivePlanView.GenLevel.Id);
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                TaskDialog.Show("Warning", e.Message);
                transaction.RollBack();
            }

            transaction.Commit();
        }


        public void GenerateCeilings(RevitRoomReader rvtReader, Document doc, double ceilingHeight)
        {
            transaction.Start();
            transaction.SetName("Create Ceilings");

            try
            {
                foreach (string key in rvtReader.roomContours.Keys)
                {
                    try
                    {
                        IList<CurveLoop> curveLoops = rvtReader.roomContours[key];

                        ElementId ceilingTypeId = GetCeilingTypeId(doc, "ACTCeiling");
                        ElementId levelId = GetLevelId(doc, "Level 0");

                        Ceiling newCeiling = Ceiling.Create(doc, curveLoops, ceilingTypeId, levelId);
                        newCeiling.LookupParameter("Height Offset From Level").SetValueString(ceilingHeight.ToString());
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                TaskDialog.Show("Warning", e.Message);
                transaction.RollBack();
            }

            transaction.Commit();
        }

        public void CreateWallFinishing(RevitRoomReader rvtReader, Document doc, string finishTypeName)
        {
            try
            {
                WallType finishType = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .FirstOrDefault(wt => wt.Name.Equals(finishTypeName, StringComparison.OrdinalIgnoreCase));

                if (finishType == null)
                {
                    TaskDialog.Show("Error", $"WallType '{finishTypeName}' not found in the model.");
                    return;
                }

                var creator = new FinishingWallCreator(doc, finishType, 00);

                using (Transaction tx = new(doc, "Create Room Finishing Walls"))
                {
                    tx.Start();
                    creator.CreateAllRooms();
                    tx.Commit();
                }

                return;
            }
            catch (Exception ex)
            {
                return;
            }
        }

        private ElementId GetCeilingTypeId(Document doc, string ceilingTypeName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(CeilingType));

            foreach (CeilingType ceilingType in collector)
            {
                if (ceilingType.Name == ceilingTypeName)
                {
                    return ceilingType.Id;
                }
            }

            return collector.FirstElementId();
        }
        private ElementId GetFloorTypeId(Document doc, string floorTypeName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(FloorType));

            foreach (FloorType floorType in collector)
            {
                if (floorType.Name == floorTypeName)
                {
                    return floorType.Id;
                }
            }

            return collector.FirstElementId();
        }
        private ElementId GetLevelId(Document doc, string levelName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(Level));

            foreach (Level level in collector)
            {
                if (level.Name == levelName)
                {
                    return level.Id;
                }
            }

            return collector.FirstElementId();
        }

    }
}
