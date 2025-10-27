using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Room = TK3D_Test.Revit.Models.Room;

namespace TK3D_Test.Revit
{
    public class RevitRoomReader
    {
        public Dictionary<string, IList<CurveLoop>> roomContours { get; set; }


        #region Constructor
        public RevitRoomReader(Document doc)
        {
            this.roomContours = [];
            ExtractRoomContours(doc);
        }
        #endregion

        #region Methods

        private void ExtractRoomContours(Document doc)
        {
            // Get all rooms in the model
            List<SpatialElement> roomCollector = [.. new FilteredElementCollector(doc, doc.ActiveView.Id)
                                                    .OfClass(typeof(SpatialElement))
                                                    .OfCategory(BuiltInCategory.OST_Rooms)
                                                    .WhereElementIsNotElementType()
                                                    .Cast<SpatialElement>()];

            SpatialElementBoundaryOptions options = new()
            {
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
            };

            int counter = 0;

            foreach (var room in roomCollector)
            {
                var boundarySegments = room.GetBoundarySegments(options);
                var curveLoops = CreateCurveLoopFromRoomBoundary(boundarySegments);

                int loopsCounter = curveLoops.Count;

                foreach (CurveLoop curveLoop in curveLoops)
                {
                    try
                    {
                        string roomId = $"{room.Name} {counter:000}";
                        this.roomContours.Add(roomId, [curveLoop]);

                        Room roomModel = new Room(room.Name, counter, curveLoop);
                        roomModel.RevitRoom = (Autodesk.Revit.DB.Architecture.Room)room;
                        counter++;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private IList<CurveLoop> CreateCurveLoopFromRoomBoundary(IList<IList<BoundarySegment>> boundarySegments, double tolerance = 1e-4)
        {
            var curveLoops = new List<CurveLoop>();

            foreach (var boundaryList in boundarySegments)
            {
                try
                {
                    CurveLoop loop = new();
                    List<XYZ> points = [];

                    foreach (BoundarySegment segment in boundaryList)
                    {
                        Curve curve = segment.GetCurve();

                        if (curve == null || !curve.IsBound)
                            continue;

                        XYZ start = curve.GetEndPoint(0);
                        XYZ end = curve.GetEndPoint(1);

                        // Avoid duplicate points caused by floating point issues
                        if (points.Count == 0 || !points.Last().IsAlmostEqualTo(start, tolerance))
                            points.Add(start);

                        points.Add(end);
                    }

                    // Build curve loop from clean points
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        Line line = Line.CreateBound(points[i], points[i + 1]);
                        loop.Append(line);
                    }

                    // Close the loop if needed
                    if (!points.First().IsAlmostEqualTo(points.Last(), tolerance))
                    {
                        Line closing = Line.CreateBound(points.Last(), points.First());
                        loop.Append(closing);
                    }

                    curveLoops.Add(loop);
                }
                catch (Exception ex)
                {
                }
            }

            return curveLoops;
        }
        #endregion
    }
}
