using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TK3D_Test.Revit.Extensions;

namespace TK3D_Test.Revit
{
    public static class GeometryHelper
    {
        private const double DefaultTolerance = 1e-4;

        public static CurveLoop CreatePolygonFromPoints(List<XYZ> points, XYZ normal)
        {
            if (points == null || points.Count < 3)
                throw new ArgumentException("At least three points are required to form a polygon.");

            var pts = RemoveDuplicates(points);
            if (pts.Count < 3)
                throw new ArgumentException("After removing duplicates, there are not enough points left to form a polygon.");

            XYZ center = GetGeometricCenter(pts);
            var sorted = SortPointsCounterClockwise(pts, center, normal);

            var loop = new CurveLoop();
            for (int i = 0; i < sorted.Count; ++i)
            {
                var a = sorted[i];
                var b = sorted[(i + 1) % sorted.Count];
                loop.Append(Line.CreateBound(a, b));
            }

            return loop;
        }

        public static Plane GetPlane(double z)
        {
            return Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0.0, 0.0, z));
        }

        /// <summary>Return true if point is on the positive side of plane (dot >= tol).</summary>
        public static bool IsInside(XYZ pt, Plane plane, double tol = DefaultTolerance)
        {
            if (pt == null || plane == null) return false;
            return (pt - plane.Origin).DotProduct(plane.Normal) >= tol;
        }

        private static List<XYZ> RemoveDuplicates(List<XYZ> points, double tol = DefaultTolerance)
        {
            var result = new List<XYZ>();
            foreach (var p in points)
            {
                if (!result.Any(u => u.DistanceTo(p) < tol))
                    result.Add(p);
            }
            return result;
        }

        private static XYZ GetGeometricCenter(List<XYZ> points)
        {
            if (points == null || points.Count == 0)
                throw new ArgumentException("Points must not be null or empty.");

            double sx = 0, sy = 0, sz = 0;
            foreach (var p in points)
            {
                sx += p.X;
                sy += p.Y;
                sz += p.Z;
            }
            return new XYZ(sx / points.Count, sy / points.Count, sz / points.Count);
        }

        private static List<XYZ> SortPointsCounterClockwise(List<XYZ> points, XYZ center, XYZ normal)
        {
            if (points == null || center == null || normal == null)
                throw new ArgumentNullException();

            var xAxis = GetPerpendicularVector(normal).Normalize();
            var yAxis = normal.CrossProduct(xAxis).Normalize();

            return points
                .OrderBy(p =>
                {
                    var v = p - center;
                    double x = v.DotProduct(xAxis);
                    double y = v.DotProduct(yAxis);
                    return Math.Atan2(y, x);
                })
                .ToList();
        }

        private static XYZ GetPerpendicularVector(XYZ normal)
        {
            if (normal == null) throw new ArgumentNullException(nameof(normal));

            // pick the axis least parallel to normal
            XYZ axis;
            double ax = Math.Abs(normal.X), ay = Math.Abs(normal.Y), az = Math.Abs(normal.Z);

            if (ax <= ay && ax <= az)
                axis = XYZ.BasisX;
            else if (ay <= ax && ay <= az)
                axis = XYZ.BasisY;
            else
                axis = XYZ.BasisZ;

            var perp = normal.CrossProduct(axis);
            if (perp.IsZeroLength())
            {
                // fallback: cross with global X
                perp = normal.CrossProduct(XYZ.BasisX);
                if (perp.IsZeroLength())
                    perp = normal.CrossProduct(XYZ.BasisY);
            }
            return perp;
        }

        public static bool HasSameEndPoint(this Curve firstCurve, Curve secondCurve, out XYZ samePoint, double tolerance = DefaultTolerance)
        {
            samePoint = null;
            if (firstCurve == null || secondCurve == null) return false;

            try
            {
                XYZ f0 = firstCurve.GetEndPoint(0);
                XYZ f1 = firstCurve.GetEndPoint(1);
                XYZ s0 = secondCurve.GetEndPoint(0);
                XYZ s1 = secondCurve.GetEndPoint(1);

                if (f0.DistanceTo(s0) < tolerance) { samePoint = f0; return true; }
                if (f1.DistanceTo(s0) < tolerance) { samePoint = f1; return true; }
                if (f0.DistanceTo(s1) < tolerance) { samePoint = f0; return true; }
                if (f1.DistanceTo(s1) < tolerance) { samePoint = f1; return true; }
                return false;
            }
            catch
            {
                samePoint = null;
                return false;
            }
        }

        /// <summary>
        /// Intersect a curve with a solid. If resultCurve is set and its length differs from the original curve,
        /// returns true (indicates partial intersection / trim).
        /// </summary>
        public static bool IsIntersectSolid(Curve curve, Solid solid, out Curve resultCurve, double tol = DefaultTolerance)
        {
            resultCurve = null;
            if (curve == null || solid == null) return false;

            try
            {
                var opts = new SolidCurveIntersectionOptions { ResultType = SolidCurveIntersectionMode.CurveSegmentsOutside};
                var intersections = solid.IntersectWithCurve(curve, opts);
                if (intersections != null && intersections.SegmentCount > 0)
                {
                    // choose longest segment
                    var segs = intersections.Cast<Curve>();
                    resultCurve = segs.OrderByDescending(s => s.Length).FirstOrDefault();
                    if (resultCurve != null && Math.Abs(resultCurve.Length - curve.Length) > tol)
                        return true;
                }
            }
            catch
            {
                // swallow; caller handles false
            }
            return false;
        }

        public static bool IsStartPointCloser(Curve curve, XYZ point)
        {
            if (curve == null || point == null) return false;
            return point.DistanceTo(curve.GetEndPoint(0)) < point.DistanceTo(curve.GetEndPoint(1));
        }

        public static Solid GetWallSolid(Wall wall, Transform transform)
        {
            if (wall == null) return null;

            var opts = new Options { ComputeReferences = false, IncludeNonVisibleObjects = false };
            var geomElem = wall.get_Geometry(opts);
            if (geomElem == null) return null;

            foreach (GeometryObject go in geomElem)
            {
                if (go is Solid s && s.Volume > 1e-9)
                {
                    if (transform != null && !transform.IsIdentity)
                        return SolidUtils.CreateTransformed(s, transform);
                    return s;
                }
            }
            return null;
        }

        public static Curve GetExtendedCurve(Curve curve, double extensionValue = 1.0)
        {
            if (curve == null) return null;

            switch (curve)
            {
                case Line line:
                    {
                        var p0 = line.GetEndPoint(0);
                        var p1 = line.GetEndPoint(1);
                        var dir = (p1 - p0).Normalize();
                        var newP0 = p0 - dir.Multiply(extensionValue);
                        var newP1 = p1 + dir.Multiply(extensionValue);
                        return Line.CreateBound(newP0, newP1);
                    }
                case Arc arc:
                    return ExtendArc(arc, extensionValue);
                default:
                    return curve;
            }
        }

        public static Arc GetArcTrimmedOrExtended(Arc arc, XYZ targetPoint)
        {
            if (arc == null) throw new ArgumentNullException(nameof(arc));
            if (targetPoint == null) throw new ArgumentNullException(nameof(targetPoint));

            var center = arc.Center;
            var xdir = arc.XDirection;
            var ydir = arc.YDirection;
            var r = arc.Radius;

            var e1 = arc.GetEndPoint(0);
            var e2 = arc.GetEndPoint(1);

            double a1 = Math.Atan2((e1 - center).DotProduct(ydir), (e1 - center).DotProduct(xdir));
            double a2 = Math.Atan2((e2 - center).DotProduct(ydir), (e2 - center).DotProduct(xdir));
            if (a1 > a2) a2 += 2.0 * Math.PI;

            var v = targetPoint - center;
            double at = Math.Atan2(v.DotProduct(ydir), v.DotProduct(xdir));

            if (e1.DistanceTo(targetPoint) < e2.DistanceTo(targetPoint))
                a1 = at;
            else
                a2 = at;

            if (a1 > a2) a2 += 2.0 * Math.PI;
            return Arc.Create(center, r, a1, a2, xdir, ydir);
        }

       

        public static List<XYZ> SortAlongAxis(IEnumerable<XYZ> pts, XYZ axis)
        {
            if (pts == null) return new List<XYZ>();
            if (axis == null) axis = XYZ.BasisX;
            axis = axis.Normalize();
            return pts.OrderBy(p => p.DotProduct(axis)).ThenBy(p => p.Z).ToList();
        }

      

        public static XYZ GetPointWithMinProjection(IReadOnlyList<XYZ> points, XYZ direction)
        {
            if (points == null || points.Count == 0 || direction == null) return null;
            var dir = direction.Normalize();
            XYZ minPt = null;
            double minVal = double.MaxValue;
            foreach (var p in points)
            {
                double v = p.DotProduct(dir);
                if (v < minVal)
                {
                    minVal = v;
                    minPt = p;
                }
            }
            return minPt;
        }

        public static List<CurveLoop> ReCreate(IEnumerable<CurveLoop> curveLoops)
        {
            if (curveLoops == null) return new List<CurveLoop>();
            return curveLoops.Select(cl => ReCreate(cl)).ToList();
        }

        public static List<Curve> BreakOverlappingLines(List<Curve> curves)
        {
            if (curves == null) return new List<Curve>();
            var list = curves.ToList();
            int iter = 0;
            bool changed;
            do
            {
                ++iter;
                if (iter > 100) throw new OverflowException("BreakOverlappingLines exceeded iteration limit.");

                changed = false;
                for (int i = 0; i < list.Count && !changed; ++i)
                {
                    if (list[i] is Line lineA)
                    {
                        for (int j = 0; j < list.Count && !changed; ++j)
                        {
                            if (i == j) continue;
                            if (list[j] is Line lineB && IsColinearAndOnSegment(lineA, lineB))
                            {
                                double p0 = ProjectParam(lineA.GetEndPoint(0), lineB);
                                double p1 = ProjectParam(lineA.GetEndPoint(1), lineB);
                                if (double.IsNaN(p0) || double.IsNaN(p1)) continue;
                                if (p1 < p0) { var t = p1; p1 = p0; p0 = t; }

                                var a = lineB.Evaluate(p0, false);
                                var b = lineB.Evaluate(p1, false);

                                var b0 = lineB.GetEndPoint(0);
                                var b1 = lineB.GetEndPoint(1);

                                // remove originals
                                list.RemoveAt(i);
                                list.Remove(lineB);

                                if (b0.DistanceTo(a) > DefaultTolerance) list.Add(Line.CreateBound(b0, a));
                                if (b.DistanceTo(b1) > DefaultTolerance) list.Add(Line.CreateBound(b, b1));

                                changed = true;
                            }
                        }
                    }
                }
            } while (changed);

            return list;
        }

        public static bool IsValidWallProfile(IList<Curve> curves)
        {
            if (curves == null || curves.Count < 3) return false;

            var dict = new Dictionary<XYZ, int>(new XyzEqualityComparer(DefaultTolerance));
            foreach (var c in curves)
            {
                var ends = new[] { c.GetEndPoint(0), c.GetEndPoint(1) };
                foreach (var e in ends)
                {
                    if (dict.TryGetValue(e, out int val)) dict[e] = val + 1;
                    else dict[e] = 1;
                }
            }
            // each vertex must appear exactly twice in a closed loop
            return dict.Values.All(v => v == 2);
        }

       

        private static bool IsColinearAndOnSegment(Line lineA, Line lineB, double tol = DefaultTolerance)
        {
            if (lineA == null || lineB == null) return false;
            if (!lineA.IsParallelTo(lineB, tol)) return false;

            var a0 = lineA.GetEndPoint(0);
            var a1 = lineA.GetEndPoint(1);
            var b0 = lineB.GetEndPoint(0);
            var b1 = lineB.GetEndPoint(1);

            var pA0 = lineB.Project(a0)?.XYZPoint;
            var pA1 = lineB.Project(a1)?.XYZPoint;
            if (pA0 == null || pA1 == null) return false;

            if (pA0.DistanceTo(a0) > tol || pA1.DistanceTo(a1) > tol) return false;

            var v1 = (a0 - b0).Normalize();
            var v2 = (a1 - b1).Normalize();
            return v1.IsParallelTo(v2, tol) && Math.Abs(v2.DotProduct(v1) + 1.0) < tol;
        }

        private static double ProjectParam(XYZ p, Line line)
        {
            var res = line.Project(p);
            return res != null ? res.Parameter : double.NaN;
        }

        private static CurveLoop ReCreate(CurveLoop curveLoop)
        {
            if (curveLoop == null) return null;
            var curves = curveLoop.ToList();
            return CurveLoop.Create(curves);
        }

        private static Arc ExtendArc(Arc arc, double extension)
        {
            if (arc == null) throw new ArgumentNullException(nameof(arc));
            var center = arc.Center;
            var startVec = (arc.GetEndPoint(0) - center).Normalize();
            var ydir = arc.Normal.CrossProduct(startVec).Normalize();
            var endVec = (arc.GetEndPoint(1) - center);
            double ang = Math.Atan2(endVec.DotProduct(ydir), endVec.DotProduct(startVec));
            if (ang < 0) ang += 2.0 * Math.PI;

            double dAng = extension / arc.Radius;
            // expand start backward by dAng, end forward by dAng
            double startAngle = -dAng;
            double endAngle = ang + dAng;
            return Arc.Create(center, arc.Radius, startAngle, endAngle, startVec, ydir);
        }

       


        // Small comparer that treats two XYZ as equal if they are within tolerance.
        private class XyzEqualityComparer : IEqualityComparer<XYZ>
        {
            private readonly double _tol;
            public XyzEqualityComparer(double tolerance = DefaultTolerance) => _tol = tolerance;
            public bool Equals(XYZ a, XYZ b)
            {
                if (a == null && b == null) return true;
                if (a == null || b == null) return false;
                return a.DistanceTo(b) <= _tol;
            }
            public int GetHashCode(XYZ obj)
            {
                if (obj == null) return 0;
                // Quantize components to produce stable hash within tolerance
                long xi = (long)Math.Round(obj.X / _tol);
                long yi = (long)Math.Round(obj.Y / _tol);
                long zi = (long)Math.Round(obj.Z / _tol);
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + xi.GetHashCode();
                    hash = hash * 31 + yi.GetHashCode();
                    hash = hash * 31 + zi.GetHashCode();
                    return hash;
                }
            }
        }
    }
}
