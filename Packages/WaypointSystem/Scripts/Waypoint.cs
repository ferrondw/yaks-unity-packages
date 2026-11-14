using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yakanashe.WaypointSystem
{
    [AddComponentMenu("Yakapedia/Waypoint")]
    public class Waypoint : MonoBehaviour
    {
        [SerializeField] private string waypointName = "New Waypoint";

        public string WaypointName
        {
            get => waypointName;
            set => waypointName = value;
        }

        [SerializeField] private Color visualisationColor = Color.green;
        public Color VisualisationColor => visualisationColor;

        [SerializeField] private bool shouldDrawLines = true;

        public bool ShouldDrawLines
        {
            get => shouldDrawLines;
            set => shouldDrawLines = value;
        }

        [SerializeField] private bool shouldTransformWithParent = true;

        public bool ShouldTransformWithParent
        {
            get => shouldTransformWithParent;
            set => shouldTransformWithParent = value;
        }

        [SerializeField] private List<Vector3> points;

        public List<Vector3> Points
        {
            get => points;
            set => points = value;
        }

        public Vector3 this[Index i]
        {
            get => Points[i];
            set => Points[i] = value;
        }
    }
}