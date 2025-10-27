using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TK3D_Test.Revit.Extensions
{
    public static class CurveLoopExtensions
    {
        public static bool IsPointInsideLoop2D(this CurveLoop loop, XYZ point)
        {
            // Project all curves and point to XY plane
            List<XYZ> polygon = loop.Select(c => new XYZ(c.GetEndPoint(0).X, c.GetEndPoint(0).Y, 0)).ToList();
            XYZ testPoint = new XYZ(point.X, point.Y, 0);

            int crossings = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                XYZ a = polygon[i];
                XYZ b = polygon[(i + 1) % polygon.Count];

                // Check if ray crosses the edge
                if (((a.Y > testPoint.Y) != (b.Y > testPoint.Y)) &&
                     (testPoint.X < (b.X - a.X) * (testPoint.Y - a.Y) / (b.Y - a.Y + 1e-10) + a.X))
                {
                    crossings++;
                }
            }

            // Odd number of crossings means inside
            return (crossings % 2 == 1);
        }

        public static CurveArray ConvertCurveLoopToClosedCurveArray(CurveLoop loop)
        {
            CurveArray curveArr = new CurveArray();

            // Create line from last to first only if IsOpen
            if (!loop.IsOpen())
            {
                foreach (var Curve in loop)
                {
                    curveArr.Append(Curve);
                }
            }
            return curveArr;
        }

        public static CurveLoop CloseCurveLoopIfOpen(this CurveLoop loop)
        {
            if (loop == null) throw new ArgumentNullException(nameof(loop));
            if (!loop.IsOpen())
                return loop; // already closed

            Curve lastCurve = loop.Last();
            Curve firstCurve = loop.First();

            XYZ endPoint = lastCurve.GetEndPoint(1);
            XYZ startPoint = firstCurve.GetEndPoint(0);

            // Only add closing segment if needed
            if (!endPoint.IsAlmostEqualTo(startPoint))
            {
                Line closingLine = Line.CreateBound(endPoint, startPoint);
                loop.Append(closingLine);
            }

            return loop;
        }

        public static CurveLoop RebuildAndCloseCurveLoop(this CurveLoop loop)
        {
            if (loop == null || !loop.Any())
                throw new ArgumentException("Input CurveLoop is null or empty.");

            List<XYZ> points = new List<XYZ>();

            foreach (Curve c in loop)
            {
                // Always add the start point
                XYZ start = c.GetEndPoint(0);
                if (points.Count == 0 || !points.Last().IsAlmostEqualTo(start))
                {
                    points.Add(start);
                }
            }

            // Add final point (end of last curve)
            XYZ finalPoint = loop.Last().GetEndPoint(1);
            if (!points.First().IsAlmostEqualTo(finalPoint))
            {
                points.Add(finalPoint);
            }

            // Now rebuild new CurveLoop
            CurveLoop newLoop = new CurveLoop();

            for (int i = 0; i < points.Count - 1; i++)
            {
                XYZ p1 = points[i];
                XYZ p2 = points[i + 1];

                // Avoid zero-length segments
                if (!p1.IsAlmostEqualTo(p2))
                {
                    Line line = Line.CreateBound(p1, p2);
                    newLoop.Append(line);
                }
            }

            return newLoop;
        }
    }
}
