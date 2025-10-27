using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System.Collections.Generic;
using System.Linq;
using TK3D_Test.Revit;

public class WallCreationService
{
    private readonly Document _doc;

    public WallCreationService(Document doc)
    {
        _doc = doc;
    }

    public Wall CreateFinishWall(Curve curve, WallType type, Level level, double height, Room room)
    {
        Wall wall = Wall.Create(
            _doc,
            curve,
            type.Id,
            level.Id,
            height,
            0.0,
            false,
            false);

        WallUtils.DisallowWallJoinAtEnd(wall, 0);
        WallUtils.DisallowWallJoinAtEnd(wall, 1);

        return wall;
    }

    public void TrimWalls(List<Wall> walls)
    {
        foreach (var wall in walls)
        {
            Solid mullionsSolid = GetMullionsSolid(wall, Transform.Identity);
            Curve resultCurve;


            Curve wallCurve = wall.Location is LocationCurve location ? null : ((LocationCurve)wall.Location).Curve;

            if (GeometryHelper.IsIntersectSolid(wallCurve, mullionsSolid, out resultCurve))
            {
                using (Transaction transaction = new(this._doc, "Trim Wall"))
                {
                    transaction.Start();
                    if (wall.Location is LocationCurve location2)
                        location2.Curve = resultCurve;
                    transaction.Commit();
                }
            }
        }
    }

    public Solid GetMullionsSolid(Wall Wall, Transform Transform)
    {
        if (Wall == null || Wall.CurtainGrid == null)
            return null;

        Solid combinedSolid = null;

        Transform transform = Transform ?? Transform.Identity;
        Options geomOptions = new Options { ComputeReferences = false, IncludeNonVisibleObjects = false };

        foreach (ElementId mullionId in Wall.CurtainGrid.GetMullionIds())
        {
            Element mullion = _doc.GetElement(mullionId);
            if (mullion == null)
                continue;

            GeometryElement geomElem = mullion.get_Geometry(geomOptions);
            if (geomElem == null)
                continue;

            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid && solid.Volume > 1e-6) 
                {
                    Solid transformedSolid = SolidUtils.CreateTransformed(solid, transform);

                    if (combinedSolid == null)
                    {
                        combinedSolid = transformedSolid;
                    }
                    else
                    {
                        try
                        {
                            combinedSolid = BooleanOperationsUtils.ExecuteBooleanOperation(
                                combinedSolid,
                                transformedSolid,
                                BooleanOperationsType.Union
                            );
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        return combinedSolid;
    }

    public void JoinAdjacentWalls(List<Wall> walls)
    {
        for (int i = 0; i < walls.Count; i++)
        {
            var wallA = walls[i];
            LocationCurve lcA = wallA.Location as LocationCurve;
            if (lcA == null) continue;

            XYZ endA = lcA.Curve.GetEndPoint(1);

            for (int j = 0; j < walls.Count; j++)
            {
                if (i == j) continue;

                var wallB = walls[j];
                LocationCurve lcB = wallB.Location as LocationCurve;
                if (lcB == null) continue;

                XYZ startB = lcB.Curve.GetEndPoint(0);

                if (endA.IsAlmostEqualTo(startB, 1e-4))
                {
                    try
                    {
                        JoinGeometryUtils.JoinGeometry(_doc, wallA, wallB);
                    }
                    catch { }
                }
            }
        }
    }

    
    public void JoinFinishToBaseWalls(Room room, List<Wall> finishWalls)
    {
        var collector = new FilteredElementCollector(_doc)
            .OfClass(typeof(Wall))
            .Cast<Wall>()
            .Where(w => !finishWalls.Contains(w))
            .ToList();

        foreach (Wall finishWall in finishWalls)
        {
            LocationCurve lcFinish = finishWall.Location as LocationCurve;
            if (lcFinish == null) continue;

            XYZ midPoint = lcFinish.Curve.Evaluate(0.5, true);

            foreach (Wall baseWall in collector)
            {
                LocationCurve lcBase = baseWall.Location as LocationCurve;
                if (lcBase == null) continue;

                double dist = lcBase.Curve.Distance(midPoint);
                if (dist < finishWall.Width + baseWall.Width)
                {
                    try
                    {
                        if (!JoinGeometryUtils.AreElementsJoined(_doc, finishWall, baseWall))
                            JoinGeometryUtils.JoinGeometry(_doc, finishWall, baseWall);
                    }
                    catch {  }
                }
            }
        }
    }

}

