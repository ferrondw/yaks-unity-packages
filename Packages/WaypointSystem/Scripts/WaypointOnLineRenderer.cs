using UnityEngine;

namespace Yakanashe.WaypointSystem
{
    [ExecuteAlways, RequireComponent(typeof(LineRenderer), typeof(Waypoint))]
    public class WaypointOnLineRenderer : MonoBehaviour
    {
        private LineRenderer _renderer;
        private Waypoint _waypoint;

        private void LateUpdate()
        {
            _renderer ??= GetComponent<LineRenderer>();
            _waypoint ??= GetComponent<Waypoint>();

            if (_waypoint.Points.Count <= 1) return;

            _renderer.positionCount = _waypoint.Points.Count;
            _renderer.SetPositions(_waypoint.Points.ToArray());
        }
    }
}