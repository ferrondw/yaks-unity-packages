using UnityEngine;

namespace Yakanashe.WaypointSystem
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class WaypointMover : MonoBehaviour
    {
        [SerializeField] private Waypoint waypoint;

        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private bool pingPong = true;
        [SerializeField] private bool rotateWithPath = true;

        private int _currentIndex;
        private bool _reversing;
        private float _startTime;

        private Rigidbody2D _rigidbody;

        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (_currentIndex == waypoint.Points.Count - 1)
            {
                if (pingPong)
                {
                    _reversing = true;
                }
                else
                {
                    _currentIndex = 0;
                    _rigidbody.position = waypoint[_currentIndex];
                }
            }

            if (_currentIndex == 0 && pingPong)
            {
                _reversing = false;
            }

            var nextPoint = waypoint[_currentIndex + (_reversing ? -1 : 1)];
            var stepDiff = waypoint[_currentIndex] - nextPoint;
            var movePosition = (Time.fixedTime - _startTime) / stepDiff.magnitude * moveSpeed;
            _rigidbody.position = Vector3.Lerp(waypoint[_currentIndex], nextPoint, movePosition);
            // https://discussions.unity.com/t/quaternion-lookrotation-in-2d/564357/5
            if (rotateWithPath)
                transform.eulerAngles = new Vector3(0, 0,
                    90 + Mathf.Atan2(_rigidbody.position.y - nextPoint.y, _rigidbody.position.x - nextPoint.x) *
                    Mathf.Rad2Deg);

            if (_rigidbody.position == (Vector2)nextPoint)
            {
                _currentIndex += _reversing ? -1 : 1;
                _startTime = Time.fixedTime;
            }
        }
    }
}