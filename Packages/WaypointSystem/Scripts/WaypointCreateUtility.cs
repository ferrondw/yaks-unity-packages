#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEngine;

public static class WaypointCreateUtility
{
    private const string undoMessage = "Created new waypoint";
    private const string baseName = "Waypoint";

    [MenuItem("GameObject/Yakapedia/Waypoint")]
    private static void CreateWaypoint()
    {
        var newWaypoint = ObjectFactory.CreateGameObject(baseName, typeof(Waypoint));

        var lastView = SceneView.lastActiveSceneView;
        newWaypoint.transform.position = lastView ? lastView.pivot : Vector3.zero;

        StageUtility.PlaceGameObjectInCurrentStage(newWaypoint);
        GameObjectUtility.EnsureUniqueNameForSibling(newWaypoint);

        Undo.RegisterCreatedObjectUndo(newWaypoint, undoMessage);
        Selection.activeGameObject = newWaypoint;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        newWaypoint.GetComponent<Waypoint>().WaypointName = newWaypoint.name;
    }
}
#endif