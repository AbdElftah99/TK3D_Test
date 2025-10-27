using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TK3D_Test.Revit.Comparers
{
    public class XYZDirectionComparer : IEqualityComparer<XYZ>
    {
        private readonly double _tolerance;

        public XYZDirectionComparer(double tolerance = 1e-6)
        {
            _tolerance = tolerance;
        }

        public bool Equals(XYZ x, XYZ y)
        {
            if (x == null || y == null)
                return false;

            XYZ a = GetCanonicalDirection(x);
            XYZ b = GetCanonicalDirection(y);

            return a.IsAlmostEqualTo(b, _tolerance);
        }

        public int GetHashCode(XYZ obj)
        {
            XYZ dir = GetCanonicalDirection(obj);
            return $"{Math.Round(dir.X, 6)}_{Math.Round(dir.Y, 6)}_{Math.Round(dir.Z, 6)}".GetHashCode();
        }

        private XYZ GetCanonicalDirection(XYZ direction)
        {
            direction = direction.Normalize();

            if (Math.Abs(direction.X) >= Math.Abs(direction.Y) && Math.Abs(direction.X) >= Math.Abs(direction.Z))
                return direction.X < 0 ? -direction : direction;

            if (Math.Abs(direction.Y) >= Math.Abs(direction.X) && Math.Abs(direction.Y) >= Math.Abs(direction.Z))
                return direction.Y < 0 ? -direction : direction;

            return direction.Z < 0 ? -direction : direction;
        }
    }
}
