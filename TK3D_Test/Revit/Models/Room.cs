using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TK3D_Test.Revit.Comparers;

namespace TK3D_Test.Revit.Models
{
    public class Room
    {
        public Autodesk.Revit.DB.Architecture.Room RevitRoom { get; set; }

        public List<Element> GeneratedElements; // <-- set of all Revit elements, generated for this Room <TODO: Dictionary by ElementId?>
        public List<CurveLoop> Contours;       // <-- curves defining the contour of the Room (currently, read from DWG)

        // Cached geometric properties of the contour
        private double area;
        private double ixx, iyy, ixy, i11, i22;
        private double theta; // <-- principal axes rotation
        private XYZ i1, i2;
        private XYZ center;

        public XYZ Center => center;
        public double Area => area;
        public XYZ Major => i1; // LocY
        public XYZ Minor => i2; // LocX (x-positive, along the longer projection or closer to X when double-symmetric)
        public double I1 => i11;
        public double I2 => i22;
        /// <summary>
        /// Rotation of principal axes CCW from X(1,0,0) in degrees [°]
        /// </summary>
        public double Rotation => theta * 180 / Math.PI;

        private string label;
        private int regId;
        private string type;
        public override int GetHashCode()
        {
            return label.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj is Room other)
            {
                return other.label.Equals(this.label);
            }
            else
                return false;
        }

        public string Id => label;
        public string Type => type;

        public Room(string roomType, int regNumber, CurveLoop extContour)
        {
            type = roomType;
            regId = regNumber;
            label = $"{roomType} {regId.ToString("000")}";
            Contours = new List<CurveLoop>() { extContour };    // the outer perimeter is always the first CurveLoop
            GeneratedElements = new List<Element>();
            GeometricProps();
        }

        public bool HasIslands => Contours.Count > 1;

        public void AddElement(Element newElement)
        {
            if (newElement is null)
                return;
            GeneratedElements.Add(newElement);
        }

        public List<Element> GetCeilings()
        {
            return GeneratedElements.Where(e => e is Ceiling).ToList();
        }
        public List<Element> GetFloors()
        {
            return GeneratedElements.Where(e => e is Ceiling).ToList();
        }

        private void GeometricProps()
        {
            CurveLoop extContour = Contours.First();
            List<XYZ> vertices = new List<XYZ>();
            foreach (Curve c in extContour)
            {
                vertices.Add(c.GetEndPoint(0));
            }

            int n = vertices.Count;
            if (n < 3) throw new ArgumentException("A polygon must have at least 3 vertices.");

            area = 0;
            double cx = 0, cy = 0;
            ixx = 0; iyy = 0; ixy = 0;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                double xi = vertices[i].X, yi = vertices[i].Y;
                double xj = vertices[j].X, yj = vertices[j].Y;
                double xyij = xi * yj - xj * yi;

                area += xyij;
                cx += (xi + xj) * xyij;
                cy += (yi + yj) * xyij;
                ixx += (yi * yi + yi * yj + yj * yj) * xyij;
                iyy += (xi * xi + xi * xj + xj * xj) * xyij;
                ixy += (xi * yj + 2 * xi * yi + 2 * xj * yj + xj * yi) * xyij;
            }

            area = area * 0.5;
            //if (Math.Abs(A) < tolerance) ...

            double inv6A = 1.0 / (6 * area);
            cx *= inv6A;
            cy *= inv6A;

            double inv12 = 1 / 12.0;
            ixx *= inv12;
            iyy *= inv12;
            ixy *= inv12 * 0.5;

            // Correct values for central axes
            center = new XYZ(cx, cy, 0);

            ixx = ixx - area * cy * cy;
            iyy = iyy - area * cx * cx;
            ixy = ixy - area * cx * cy;
            if (area < 0)   // CW vs. CCW polygon
            {
                area *= -1;
                ixx *= -1;
                iyy *= -1;
                ixy *= -1;
            }

            // Calculate principal values
            double c1 = (ixx + iyy) * 0.5;
            double c2 = (ixx - iyy) * 0.5;
            double c3 = Math.Sqrt(c2 * c2 + ixy * ixy);
            i11 = c1 + c3;
            i22 = c1 - c3;

            theta = 0;
            if (Math.Abs(ixy) < 1E-7)
            {
                // NOTE: for double-symmetry shapes (e.g. square) where any set of axes are principal (return 0,0,0 for both i1 and i2)
                if (Math.Abs(ixx - iyy) < 1E-7)
                {
                    theta = 0;
                    i1 = new XYZ(0, 0, 0);
                    i2 = new XYZ(0, 0, 0);
                    return;
                }
                else
                {
                    if (iyy > ixx)
                        theta = 0;
                    else
                        theta = Math.PI * 0.5;
                }
            }
            else
                theta = 0.5 * Math.Atan2(2 * ixy, iyy - ixx);
            i2 = new XYZ(Math.Cos(theta), Math.Sin(theta), 0);
            if (i2.X < -1E-3)
                i2 = i2.Multiply(-1);
            i1 = new XYZ(-i2.Y, i2.X, 0);
        }

        public bool IsRectangular(out double b, out double l, double tolerance = 1E-3 /* about 0.06 degrees tolerance */)
        {
            b = 0;
            l = 0;
            if (HasIslands)
                return false;
            if (Contours is null)
                return false;
            if (Contours.Count < 1)
                return false;

            if (i1.GetLength() < 1E-5 || i2.GetLength() < 1E-5)
            {
                #region Helper PCA methods
                List<Tuple<double, double, List<double>>> vecSet = new List<Tuple<double, double, List<double>>>();
                void AddVecBin(double x, double y, double len)
                {
                    foreach (var vec in vecSet)
                    {
                        double dcos = vec.Item1 * x + vec.Item2 * y;
                        if (Math.Abs(Math.Abs(dcos) - 1) < 1E-4)
                        {
                            vec.Item3.Add(len);
                            return;
                        }
                    }
                    vecSet.Add(new Tuple<double, double, List<double>>(x, y, new List<double>() { len }));
                }
                #endregion

                foreach (Curve c in Contours.First())
                {
                    if (c is Line cur)
                        AddVecBin(cur.Direction.X, cur.Direction.Y, cur.Length);
                    else
                        return false;
                }
                double x1 = 0, y1 = 0, vMax = 0;
                foreach (var vec in vecSet)
                {
                    double vSum = vec.Item3.Sum();
                    if (vSum > vMax)
                    {
                        vMax = vSum;
                        x1 = vec.Item1;
                        y1 = vec.Item2;
                    }
                }
                if (Math.Abs(x1) > Math.Abs(y1))
                {
                    if (x1 < -1E-3)
                        i2 = new XYZ(-x1, -y1, 0);
                    else
                        i2 = new XYZ(x1, y1, 0);
                }
                else
                {
                    i2 = new XYZ(x1, y1, 0);
                    i2 = i2.CrossProduct(XYZ.BasisZ);
                    if (i2.X < -1E-3)
                        i2 = i2.Multiply(-1);
                }
                i1 = i2.CrossProduct(XYZ.BasisZ);
                theta = Math.Atan2(i2.Y, i2.X);
            }

            double bmin = 1E60, bmax = 1E-60, hmin = 1E60, hmax = 1E-60;
            double prevSign = 0;
            Line prev = null;
            foreach (Curve c in Contours.First())
            {
                if (c is Line cur)
                {
                    XYZ pi = cur.GetEndPoint(0);
                    double bi = (pi - center).DotProduct(i1);
                    double hi = (pi - center).DotProduct(i2);
                    if (bi < bmin)
                        bmin = bi;
                    if (bi > bmax)
                        bmax = bi;
                    if (hi < hmin)
                        hmin = hi;
                    if (hi > hmax)
                        hmax = hi;
                    if (prev is null)
                    {
                        prev = cur;
                        continue;
                    }
                    double cos = cur.Direction.DotProduct(prev.Direction);
                    double sin = cur.Direction.CrossProduct(prev.Direction).Z;
                    if (Math.Abs(cos - 1.0) < tolerance * tolerance) // colinear segments continue
                    {
                        continue;
                    }
                    // non-orthogonal segments encountered -> contour is irregular
                    // also check for cross product and 'winding' direction
                    else if (Math.Abs(cos) > tolerance)
                    {
                        return false;
                    }
                    else
                    {
                        if (prevSign != 0)
                        {
                            if (prevSign * sin < 0)
                                return false;
                        }
                        else
                            prevSign = sin;
                    }
                    prev = cur;
                }
                else
                    return false;
            }
            // obtain B and L dimensions from aligned bounding box projection.
            b = bmax - bmin;
            l = hmax - hmin;
            return true;
        }

        public void GetAxisAlignedBounds(out double width, out double length, out XYZ minPoint, out XYZ maxPoint)
        {
            width = 0;
            length = 0;
            minPoint = null;
            maxPoint = null;

            if (Contours == null || Contours.Count == 0)
                return;

            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            foreach (var loop in Contours)
            {
                foreach (var curve in loop)
                {
                    XYZ p1 = curve.GetEndPoint(0);
                    XYZ p2 = curve.GetEndPoint(1);

                    minX = Math.Min(minX, Math.Min(p1.X, p2.X));
                    maxX = Math.Max(maxX, Math.Max(p1.X, p2.X));
                    minY = Math.Min(minY, Math.Min(p1.Y, p2.Y));
                    maxY = Math.Max(maxY, Math.Max(p1.Y, p2.Y));
                }
            }

            width = maxX - minX;
            length = maxY - minY;
            minPoint = new XYZ(minX, minY, 0);
            maxPoint = new XYZ(maxX, maxY, 0);
        }

        public void GetOrientedRoomBounds(
            out XYZ origin,
            out XYZ widthVector,
            out XYZ lengthVector,
            out double width,
            out double length)
        {
            origin = null;
            widthVector = null;
            lengthVector = null;
            width = 0;
            length = 0;

            if (Contours == null || Contours.Count == 0)
                return;

            var loop = Contours.FirstOrDefault();
            if (loop == null || loop.Count() < 2)
                return;

            // Try all unique edge directions
            List<XYZ> uniqueDirections = loop
                .Select(c => (c.GetEndPoint(1) - c.GetEndPoint(0)).Normalize())
                .Where(v => v.GetLength() > 0.01)
                .Distinct(new XYZDirectionComparer())
                .ToList();

            double minArea = double.MaxValue;

            foreach (var dirX in uniqueDirections)
            {
                XYZ dirY = new XYZ(-dirX.Y, dirX.X, 0); // Perpendicular vector
                List<XYZ> points = loop.SelectMany(c => new[] { c.GetEndPoint(0), c.GetEndPoint(1) }).ToList();

                var projectionsX = points.Select(p => p.DotProduct(dirX)).ToList();
                var projectionsY = points.Select(p => p.DotProduct(dirY)).ToList();

                double minX = projectionsX.Min();
                double maxX = projectionsX.Max();
                double minY = projectionsY.Min();
                double maxY = projectionsY.Max();

                double w = maxX - minX;
                double l = maxY - minY;
                double area = w * l;

                if (area < minArea)
                {
                    minArea = area;

                    width = w;
                    length = l;
                    widthVector = dirX;
                    lengthVector = dirY;

                    XYZ basePoint = dirX.Multiply(minX).Add(dirY.Multiply(minY));
                    origin = basePoint;
                }
            }
        }
    }
}
