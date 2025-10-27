using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TK3D_Test.Revit
{
    public class RoomBoundaryReader
    {
        private readonly Document _doc;

        public RoomBoundaryReader(Document doc)
        {
            _doc = doc;
        }

        public List<BoundarySegment> GetModelWallBoundaries(Room room)
        {
            SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions
            {
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
            };

            var boundaries = room.GetBoundarySegments(opt);
            if (boundaries == null)
                return new List<BoundarySegment>();

            return boundaries
                .SelectMany(loop => loop)
                .Where(seg => _doc.GetElement(seg.ElementId) is Wall)
                .ToList();
        }
    }
}
