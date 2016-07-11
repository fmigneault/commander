using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

using Buildings;

namespace Editor
{
    [CustomEditor(typeof(BuildingPlacementManager))]
    public class BuildingPlacementEditor : UnityEditor.Editor
    {
        private BuildingPlacementManager placementManager { get { return target as BuildingPlacementManager; } }


        public override void OnInspectorGUI()
        {
            // Draw any public defined variable in the script
            DrawDefaultInspector();

            // Update selected values for tags
            serializedObject.Update();
            placementManager.PlacementCollisionTags = ListTagSelector.ShowList("Tags that cause collisions:", 
                                                      serializedObject.FindProperty("PlacementCollisionTags"),
                                                      placementManager.PlacementCollisionTags.ToArray()).ToList();

            // Apply changes
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}

