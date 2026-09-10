using System;
using UnityEngine;

namespace OpenTS2.SimAntics.Routing
{
    // Reflects a VMEntity's logical tile position on a transform, gliding smoothly toward the
    // entity's current tile each frame. The VM owns the logical position (advanced per tick by
    // routing); this component only renders it. Height comes from an optional provider so the sim
    // stays on the floor. A driver advances the logical tile once AtTargetTile reports arrival,
    // which keeps movement smooth and on-path.
    public class VMEntityRenderer : MonoBehaviour
    {
        public VMEntity Entity;
        public float Speed = 3f; // world units per second
        public Func<float, float, float> HeightAt;

        private void Update()
        {
            if (Entity == null)
                return;

            GetTargetGroundPosition(out var targetX, out var targetY);
            var height = HeightAt != null ? HeightAt(targetX, targetY) : transform.localPosition.z;
            var target = new Vector3(targetX, targetY, height);

            var previous = transform.localPosition;
            transform.localPosition = Vector3.MoveTowards(previous, target, Speed * Time.deltaTime);

            var moved = transform.localPosition - previous;
            var forward = new Vector3(moved.x, moved.y, 0f);
            if (forward.sqrMagnitude > 1e-8f)
                transform.localRotation = Quaternion.LookRotation(forward, Vector3.forward);
        }

        // True once the transform has reached the entity's current tile centre on the ground plane.
        public bool AtTargetTile(float epsilon = 0.05f)
        {
            if (Entity == null)
                return true;
            GetTargetGroundPosition(out var targetX, out var targetY);
            var position = transform.localPosition;
            var dx = position.x - targetX;
            var dy = position.y - targetY;
            return dx * dx + dy * dy <= epsilon * epsilon;
        }

        private void GetTargetGroundPosition(out float x, out float y)
        {
            LotTileCoordinates.TileCenterToWorld(new GridPosition(Entity.TileX, Entity.TileY), out x, out y);
        }
    }
}
