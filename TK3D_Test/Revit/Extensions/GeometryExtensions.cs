using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TK3D_Test.Revit.Extensions
{
    public static class GeometryExtensions
    {
        public static bool IsParallelTo(this Line line, Line checkedLine, double tolerance = 1E-09)
        {
            return line.Direction.IsParallelTo(checkedLine.Direction, tolerance);
        }

        public static bool IsParallelTo(this Line line, XYZ checkedVector, double tolerance = 1E-09)
        {
            return line.Direction.IsParallelTo(checkedVector, tolerance);
        }

        public static bool IsParallelTo(this XYZ vector, XYZ checkedVector, double tolerance = 1E-09)
        {
            return Math.Abs(Math.Abs(vector.DotProduct(checkedVector)) - 1.0) < tolerance;
        }



        public static bool IsPerpendicularTo(this Line line, Line checkedLine, double tolerance = 1E-09)
        {
            return line.Direction.IsPerpendicularTo(checkedLine.Direction, tolerance);
        }

        public static bool IsPerpendicularTo(this XYZ vector, XYZ checkedVector, double tolerance = 1E-09)
        {
            return Math.Abs(Math.Abs(vector.DotProduct(checkedVector))) < tolerance;
        }

        public static bool IsLieOnSameStraightLine(this Line firstLine, Line secondLine, double tolerance = 1E-09)
        {
            if (!firstLine.IsParallelTo(secondLine))
            {
                return false;
            }

            IList<XYZ> list = firstLine.Tessellate();
            IList<XYZ> list2 = secondLine.Tessellate();
            XYZ direction = firstLine.Direction;
            foreach (XYZ item in list)
            {
                foreach (XYZ item2 in list2)
                {
                    if (!(Math.Abs(item.DistanceTo(item2)) < tolerance))
                    {
                        XYZ checkedVector = (item2 - item).Normalize();
                        if (!direction.IsParallelTo(checkedVector))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

    }
}
