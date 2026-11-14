using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yakanashe.WaypointSystem
{
    [CustomEditor(typeof(Waypoint))]
    public class WaypointEditor : Editor
    {
        [SerializeField] private Texture addButton;
        [SerializeField] private Texture editButton;
        [SerializeField] private Texture deleteButton;
        [SerializeField] private Texture simplifyButton;
        [SerializeField] private Texture smoothButton;

        private bool _isEditing;
        private bool _isInserting;
        private bool _isDeleting;
        private bool _isDragging;
        private bool _isViewingExtraOptions;

        private GUIStyle _style;

        private readonly Vector2 _addButtonOffset = new(1f, -0.5f);
        private readonly Vector2 _editButtonOffset = new(0.2f, -0.15f);
        private readonly Vector2 _insertButtonOffset = new(0, 0);
        private readonly Vector2 _deleteButtonOffset = new(0.2f, -0.15f);
        private readonly Vector2 _insertToggleButtonOffset = new(0.6f, -0.15f);
        private readonly Vector2 _deleteToggleButtonOffset = new(1f, -0.15f);
        private readonly Vector2 _simplifyButtonOffset = new(0.2f, -0.5f);
        private readonly Vector2 _smoothButtonOffset = new(0.6f, -0.5f);
        private readonly Vector2 _waypointNameOffset = new(0.3f, 0.24f);

        private readonly Color _toggleColor = new(0.8f, 0.8f, 0.8f);
        private readonly Color _addColor = new(0.1f, 1f, 0.1f);
        private readonly Color _insertColor = new(0.5f, 0.8f, 1f);
        private readonly Color _deleteColor = new(1f, 0.1f, 0.1f);
        private readonly Color _simplifyColor = new(1f, 0.6f, 1f);
        private readonly Color _smoothColor = new(.5f, .5f, .9f);
        private readonly float _buttonSize = 22;

        private const float LineThickness = 2f;
        private const float SmoothStrength = 1.5f;
        private const float NewPointOffset = 0.3f;
        private const float HandleSize = 12;
        private const float SimplifyTolerance = 0.5f;
        private const int SmoothIterationAmount = 3;
        private const int MinWaypointAmount = 3;

        private static Camera SceneCamera => SceneView.currentDrawingSceneView.camera;

        private Waypoint _waypoint => (Waypoint)target;

        private Vector3 _positionLastFrame;
        private Quaternion _rotationLastFrame;
        private Vector3 _scaleLastFrame;
        private bool _hasLastTransform = false;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();

            EditorGUILayout.Space(15);

            GUILayout.Label("Toggles");
            GUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.345f, 0.345f, 0.345f);
            if (GUILayout.Button("Edit"))
            {
                _isEditing = !_isEditing;
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Insert"))
            {
                _isInserting = !_isInserting;
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Delete"))
            {
                _isDeleting = !_isDeleting;
                SceneView.RepaintAll();
            }

            GUILayout.EndHorizontal();

            GUILayout.Label("Actions");
            GUILayout.BeginHorizontal();
            GUI.contentColor = _simplifyColor;
            if (GUILayout.Button("Simplify"))
            {
                Undo.RecordObject(_waypoint, "Simplified Waypoint");
                _waypoint.Points = Simplify(_waypoint.Points);
                SceneView.RepaintAll();
            }

            GUI.contentColor = _smoothColor;
            if (GUILayout.Button("Smooth"))
            {
                for (int i = 0; i < SmoothIterationAmount; i++)
                {
                    Undo.RecordObject(_waypoint, $"Smoothed Waypoint (iteration {i})");
                    _waypoint.Points = VectorModification.SmoothPoints(_waypoint.Points, SmoothStrength);
                }

                SceneView.RepaintAll();
            }

            GUI.contentColor = _addColor;
            if (GUILayout.Button("Add"))
            {
                AddWaypoint();
                SceneView.RepaintAll();
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            GUI.backgroundColor = new Color(0.345f, 0.345f, 0.345f);
            GUI.contentColor = Color.white;
            _isViewingExtraOptions = GUILayout.Toggle(_isViewingExtraOptions, "Show Extra Options");

            if (_isViewingExtraOptions)
            {
                GUILayout.Space(15);
                if (GUILayout.Button("Reset Z To Parent"))
                {
                    ResetZToParent();
                    SceneView.RepaintAll();
                }

                GUI.backgroundColor = _deleteColor;
                if (GUILayout.Button("Reset All Waypoints"))
                {
                    ResetWaypoints();
                    SceneView.RepaintAll();
                }

                GUI.backgroundColor = new Color(0.345f, 0.345f, 0.345f);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ResetWaypoints()
        {
            Undo.RecordObject(_waypoint, $"Reset all points of Waypoint \"{_waypoint.WaypointName}\"");
            _waypoint.Points = new List<Vector3>();
        }

        private void AddWaypoint()
        {
            _waypoint.Points ??= new List<Vector3>();

            Undo.RecordObject(_waypoint, $"Added point in Waypoint \"{_waypoint.WaypointName}\"");

            _waypoint.Points.Add(_waypoint[^1] + Vector3.up * NewPointOffset);

            if (!_isEditing)
            {
                _isEditing = true;
            }
        }

        private void AddWaypoint(int index)
        {
            if (index < 0 || index >= _waypoint.Points.Count - 1)
            {
                Debug.LogWarning("Index out of range for insertion between points.");
                return;
            }

            Undo.RecordObject(_waypoint, $"Added point in Waypoint \"{_waypoint.WaypointName}\" at index {index}");

            var newPoint = (_waypoint[index] + _waypoint[index + 1]) * 0.5f;
            _waypoint.Points.Insert(index + 1, newPoint);
            _isEditing = true;

            SceneView.RepaintAll();
        }

        private void Smooth(List<Vector3> points, int iterationAmount, float threshhold)
        {
            if (points.Count < MinWaypointAmount)
            {
                Debug.LogWarning($"There should be at least {MinWaypointAmount} points to smooth the points.");
                return;
            }

            Undo.RecordObject(_waypoint, $"Smoothed Waypoint \"{_waypoint.WaypointName}\"");

            for (var i = 0; i < iterationAmount; i++)
            {
                VectorModification.SmoothPoints(points, threshhold);
            }
        }

        private List<Vector3> Simplify(List<Vector3> inputPoints)
        {
            if (inputPoints == null || inputPoints.Count < MinWaypointAmount)
            {
                Debug.LogWarning("Input list is null or has too few points. No simplification performed.");
                return inputPoints;
            }

            Undo.RecordObject(_waypoint, $"Simplified Waypoint \"{_waypoint.WaypointName}\"");

            var simplifiedPoints = VectorModification.Simplify(inputPoints, SimplifyTolerance);

            return simplifiedPoints;
        }

        private void OnSceneGUI()
        {
            _waypoint.Points ??= new List<Vector3> { _waypoint.transform.position };

            if (!Application.isPlaying)
            {
                _waypoint[0] = _waypoint.transform.position;
            }

            MoveWithParent();

            Handles.color = _waypoint.VisualisationColor;

            _style = new GUIStyle
            {
                normal = { textColor = _waypoint.VisualisationColor }
            };

            var labelOffsetX = SceneCamera.transform.right *
                               (_waypointNameOffset.x * HandleUtility.GetHandleSize(_waypoint[^1]));
            var labelOffsetY = SceneCamera.transform.up *
                               (_waypointNameOffset.y * HandleUtility.GetHandleSize(_waypoint[^1]));
            Handles.Label(_waypoint[^1] + labelOffsetX + labelOffsetY, _waypoint.WaypointName, _style);


            if (GizmosButton(_waypoint[^1], _editButtonOffset, editButton, _toggleColor))
            {
                _isEditing = !_isEditing;
            }

            if (_isEditing)
            {
                if (GizmosButton(_waypoint[^1], _addButtonOffset, addButton, _addColor))
                {
                    AddWaypoint();
                }

                if (GizmosButton(_waypoint[^1], _deleteToggleButtonOffset, deleteButton, _toggleColor))
                {
                    _isDeleting = !_isDeleting;
                }

                if (GizmosButton(_waypoint[^1], _insertToggleButtonOffset, addButton, _toggleColor))
                {
                    _isInserting = !_isInserting;
                }

                if (GizmosButton(_waypoint[^1], _simplifyButtonOffset, simplifyButton, _simplifyColor))
                {
                    _waypoint.Points = Simplify(_waypoint.Points);
                }

                if (GizmosButton(_waypoint[^1], _smoothButtonOffset, smoothButton, _smoothColor))
                {
                    Smooth(_waypoint.Points, SmoothIterationAmount, SmoothStrength);
                }
            }

            DrawGizmos();
        }

        private bool GizmosButton(Vector3 position, Vector2 offset, Texture texture, Color tint)
        {
            var offsetHorizontal = SceneCamera.transform.right * (offset.x * HandleUtility.GetHandleSize(position));
            var offsetVertical = SceneCamera.transform.up * (offset.y * HandleUtility.GetHandleSize(position));
            var offsetPosition = position + offsetHorizontal + offsetVertical;

            if (SceneCamera.worldToCameraMatrix.MultiplyPoint(offsetPosition).z >= 0f)
                return false; // skip if looking back

            var screenPosition = HandleUtility.WorldToGUIPoint(offsetPosition);

            Handles.BeginGUI();

            var buttonPosition =
                new Vector2(screenPosition.x - _buttonSize * 0.6f, screenPosition.y - _buttonSize * 0.5f);
            var buttonRect = new Rect(buttonPosition.x, buttonPosition.y, _buttonSize * 1.2f, _buttonSize);

            GUI.contentColor = tint;

            var pressed = GUI.Button(buttonRect, texture);

            Handles.EndGUI();

            return pressed;
        }


        private void DrawGizmos()
        {
            var length = _waypoint.Points.Count;
            for (int i = 0; i < length; i++)
            {
                DrawSingleWaypoint(i);
                DrawSingleLine(i);

                if (!_isEditing) continue;

                HandleInsertion(i);
                HandleDeletion(i);
            }
        }

        private void DrawSingleLine(int i)
        {
            if (i < _waypoint.Points.Count - 1 && _waypoint.ShouldDrawLines)
            {
                Handles.DrawLine(_waypoint[i], _waypoint[i + 1], LineThickness);
            }
        }

        private void HandleDeletion(int i)
        {
            if (_isDeleting && i > 0 && i < _waypoint.Points.Count - 1)
            {
                if (GizmosButton(_waypoint[i], _deleteButtonOffset, deleteButton, _deleteColor))
                {
                    Delete(i);
                }
            }
        }

        private void HandleInsertion(int i)
        {
            if (!_isInserting || i >= _waypoint.Points.Count - 1)
            {
                return;
            }

            var midPoint = (_waypoint[i] + _waypoint[i + 1]) * 0.5f;
            if (GizmosButton(midPoint, _insertButtonOffset, addButton, _insertColor))
            {
                AddWaypoint(i);
            }
        }

        private void DrawSingleWaypoint(int i)
        {
            Handles.DrawSolidDisc(_waypoint[i], SceneCamera.transform.forward,
                HandleUtility.GetHandleSize(_waypoint[i]) / HandleSize);

            if (_isEditing && i > 0)
            {
                _waypoint[i] = Handles.PositionHandle(_waypoint[i], Quaternion.identity);
            }
        }

        private void MoveWithParent()
        {
            if (!_waypoint.transform.hasChanged || !_waypoint.ShouldTransformWithParent)
            {
                _hasLastTransform = true;
                return;
            }

            if (!_hasLastTransform)
            {
                _positionLastFrame = _waypoint.transform.position;
                _rotationLastFrame = _waypoint.transform.rotation;
                _scaleLastFrame = _waypoint.transform.localScale;
                _hasLastTransform = true;
                _waypoint.transform.hasChanged = false;
            }

            var rotationDelta = _waypoint.transform.rotation * Quaternion.Inverse(_rotationLastFrame);

            // made it divide because multiplying made scaling explode
            var parentScale = _waypoint.transform.localScale;
            var scaleRatio = Vector3.one;

            if (parentScale.x != 0 && parentScale.y != 0 && parentScale.z != 0) // divide by zero creating NaN otherwise
            {
                scaleRatio = new Vector3(parentScale.x / _scaleLastFrame.x, parentScale.y / _scaleLastFrame.y,
                    parentScale.z / _scaleLastFrame.z);
            }

            var length = _waypoint.Points.Count;
            for (var i = 0; i < length; i++)
            {
                var offset = _waypoint[i] - _positionLastFrame;
                offset = Vector3.Scale(offset, scaleRatio);
                offset = rotationDelta * offset;
                _waypoint[i] = _waypoint.transform.position + offset;
            }

            _positionLastFrame = _waypoint.transform.position;
            _rotationLastFrame = _waypoint.transform.rotation;
            _scaleLastFrame = _waypoint.transform.localScale;
            _waypoint.transform.hasChanged = false;
        }

        private void Delete(Index i)
        {
            Undo.RecordObject(_waypoint, $"Deleted point in Waypoint \"{_waypoint.WaypointName}\" at index {i}");
            _waypoint.Points.Remove(_waypoint[i]);
        }

        private void ResetZToParent()
        {
            Undo.RecordObject(_waypoint, "Reset Z To Parent");

            List<Vector3> points = _waypoint.Points;
            float zPos = _waypoint.transform.position.z;

            var l = points.Count;
            for (int i = 0; i < l; i++)
            {
                points[i] = new Vector3(points[i].x, points[i].y, zPos);
            }

            _waypoint.Points = points;
        }
    }
}