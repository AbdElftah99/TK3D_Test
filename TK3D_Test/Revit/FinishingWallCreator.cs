using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TK3D_Test.Revit
{
    public class FinishingWallCreator
    {
        private readonly Document _doc;
        private readonly WallType _finishType;
        private readonly double _offset;

        private readonly RoomBoundaryReader _boundaryService;
        private readonly WallCreationService _wallService;

        public FinishingWallCreator(Document doc, WallType finishType, double offsetMm)
        {
            _doc = doc;
            _finishType = finishType;
            _offset = offsetMm;

            _boundaryService = new RoomBoundaryReader(doc);
            _wallService = new WallCreationService(doc);
        }

        public void CreateAllRooms()
        {
            var rooms = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Cast<Room>()
                .Where(r => r.Area > 0)
                .ToList();

            foreach (Room room in rooms)
                CreateWallFinishForRoom(room);
        }

        private void CreateWallFinishForRoom(Room room)
        {
            var boundaries = _boundaryService.GetModelWallBoundaries(room);
            if (boundaries.Count == 0)
                return;

            double height = RoomUtils.GetRoomHeight(room);
            Level level = _doc.GetElement(room.LevelId) as Level;

            List<Wall> roomWalls = [];

            foreach (BoundarySegment seg in boundaries)
            {
                Curve curve = seg.GetCurve();
                if (curve == null || curve.Length < 1e-3)
                    continue;

                XYZ dirToRoom = RoomUtils.ComputeInwardDirection(room, curve);
                double offset = _finishType.Width / 2.0;
                Curve innerCurve = curve.CreateTransformed(Transform.CreateTranslation(dirToRoom.Multiply(offset)));

                Wall wall = _wallService.CreateFinishWall(innerCurve, _finishType, level, height, room);
                roomWalls.Add(wall);
            }

            _wallService.TrimWalls(roomWalls);

            _wallService.JoinAdjacentWalls(roomWalls);
            _wallService.JoinFinishToBaseWalls(room, roomWalls);
        }
    }
}
