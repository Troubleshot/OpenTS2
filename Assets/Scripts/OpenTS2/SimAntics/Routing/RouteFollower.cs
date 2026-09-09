using System.Collections.Generic;
using UnityEngine;

namespace OpenTS2.SimAntics.Routing
{
    // Render-side driver that walks a transform along a routing path. Each frame it advances a
    // RouteTraversal by Speed * deltaTime and copies the interpolated position onto the object.
    // The lot ground plane is X/Y with Z as height (verified from lot object placement), so the
    // path's world X/Y map to the transform's local X/Y and Height stays on Z.
    public class RouteFollower : MonoBehaviour
    {
        // Movement speed in tiles per second.
        public float Speed = 3f;

        private RouteTraversal _traversal;
        private float _height;

        public bool HasRoute => _traversal != null;
        public bool IsComplete => _traversal == null || _traversal.IsComplete;

        public void SetRoute(IReadOnlyList<GridPosition> path, float height)
        {
            _traversal = new RouteTraversal(path);
            _height = height;
            ApplyToTransform();
        }

        private void Update()
        {
            if (_traversal == null || _traversal.IsComplete)
                return;
            _traversal.Advance(Speed * Time.deltaTime);
            ApplyToTransform();
        }

        private void ApplyToTransform()
        {
            if (_traversal == null)
                return;

            transform.localPosition = new Vector3(_traversal.WorldX, _traversal.WorldY, _height);

            if (Mathf.Abs(_traversal.HeadingX) > 1e-4f || Mathf.Abs(_traversal.HeadingY) > 1e-4f)
            {
                var forward = new Vector3(_traversal.HeadingX, _traversal.HeadingY, 0f);
                transform.localRotation = Quaternion.LookRotation(forward, Vector3.forward);
            }
        }
    }
}
