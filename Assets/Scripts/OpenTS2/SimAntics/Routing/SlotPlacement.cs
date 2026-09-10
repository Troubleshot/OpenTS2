using System;

namespace OpenTS2.SimAntics.Routing
{
    // Computes where an object's slot sits in the world. The slot offset is in the object's local
    // ground plane (X/Y); it is rotated by the object's facing (counter-clockwise degrees) and
    // added to the object's tile centre, then snapped to a tile. This is what routing to a precise
    // slot (e.g. a chair's sitting slot) needs. The mapping from an entity's stored facing to
    // degrees is the caller's concern.
    public static class SlotPlacement
    {
        public static void GetSlotWorldPosition(GridPosition objectTile, float objectRotationDegrees,
            float offsetX, float offsetY, out float worldX, out float worldY)
        {
            var radians = objectRotationDegrees * (Math.PI / 180.0);
            var cos = (float)Math.Cos(radians);
            var sin = (float)Math.Sin(radians);
            var rotatedX = offsetX * cos - offsetY * sin;
            var rotatedY = offsetX * sin + offsetY * cos;

            LotTileCoordinates.TileCenterToWorld(objectTile, out var centerX, out var centerY);
            worldX = centerX + rotatedX;
            worldY = centerY + rotatedY;
        }

        public static GridPosition GetSlotTile(GridPosition objectTile, float objectRotationDegrees,
            float offsetX, float offsetY)
        {
            GetSlotWorldPosition(objectTile, objectRotationDegrees, offsetX, offsetY, out var worldX, out var worldY);
            return LotTileCoordinates.WorldToTile(worldX, worldY);
        }
    }
}
