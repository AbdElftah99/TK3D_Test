using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TK3D_Test.Core;
using TK3D_Test.Revit.Factories;

namespace TK3D_Test.Revit
{
    public class DataCache
    {
        private readonly Dictionary<int, IList<FloorType>> _floorTypesCache = [];
        private readonly Dictionary<int, IList<CeilingType>> _ceilingTypesCache = [];
        private readonly Dictionary<int, IList<WallType>> _wallTypesCache = [];
        private readonly Dictionary<int, IList<Level>> _levelsCashe = [];
        private readonly Dictionary<int, Transform> _transformFromlinksCashe = [];

        public IList<FloorType> GetFloorTypes(Document doc)
        {
            IList<FloorType> floorTypes;
            if (!this._floorTypesCache.TryGetValue(doc.GetHashCode(), out floorTypes))
            {
                floorTypes = RevitDataFactory.GetFloorTypes(doc);
                this._floorTypesCache[doc.GetHashCode()] = floorTypes;
            }
            return floorTypes;
        }

        public IList<CeilingType> GetCeilingTypes(Document doc)
        {
            IList<CeilingType> ceilingTypes;
            if (!this._ceilingTypesCache.TryGetValue(doc.GetHashCode(), out ceilingTypes))
            {
                ceilingTypes = RevitDataFactory.GetCeilingTypes(doc);
                this._ceilingTypesCache[doc.GetHashCode()] = ceilingTypes;
            }
            return ceilingTypes;
        }

        public IList<WallType> GetWallTypes(Document doc)
        {
            IList<WallType> wallTypes;
            if (!this._wallTypesCache.TryGetValue(doc.GetHashCode(), out wallTypes))
            {
                wallTypes = RevitDataFactory.GetWallTypes(doc);
                this._wallTypesCache[doc.GetHashCode()] = wallTypes;
            }
            return wallTypes;
        }

        public Transform GetTransformFromLink(RevitLinkInstance link)
        {
            Transform totalTransform;
            if (!this._transformFromlinksCashe.TryGetValue(link.GetLinkDocument().GetHashCode(), out totalTransform))
            {
                totalTransform = ((Instance)link).GetTotalTransform();
                this._transformFromlinksCashe[link.GetLinkDocument().GetHashCode()] = totalTransform;
            }
            return totalTransform;
        }

        public IList<Level> GetLevels(Document doc)
        {
            IList<Level> levels;
            if (!this._levelsCashe.TryGetValue(doc.GetHashCode(), out levels))
            {
                levels = RevitDataFactory.GetLevels(doc);
                this._levelsCashe[doc.GetHashCode()] = levels;
            }
            return levels;
        }
    }
}
