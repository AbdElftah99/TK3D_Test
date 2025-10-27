using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TK3D_Test.Revit
{
    public static class RoomUtils
    {
        public static XYZ ComputeInwardDirection(Room room, Curve boundaryCurve)
        {
            XYZ mid = (boundaryCurve.GetEndPoint(0) + boundaryCurve.GetEndPoint(1)) / 2;
            XYZ roomCenter = GetRoomCenter(room);
            XYZ dir = (roomCenter - mid).Normalize();
            return dir;
        }

        public static XYZ GetRoomCenter(Room room)
        {
            LocationPoint lp = room.Location as LocationPoint;
            if (lp != null)
                return lp.Point;

            BoundingBoxXYZ bb = room.get_BoundingBox(null);
            return (bb.Min + bb.Max) / 2;
        }

        public static double GetRoomHeight(Room room)
        {
            double height = room.get_Parameter(BuiltInParameter.ROOM_HEIGHT)?.AsDouble() ?? 3.0;
            return height;
        }
    }
}
