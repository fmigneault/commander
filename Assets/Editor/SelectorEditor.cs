using System;
using UnityEditor;
using UnityEngine;

using Cameras;

namespace Editor
{
    [CustomEditor(typeof(SelectorManager))]
    public class SelectorEditor : UnityEditor.Editor
    {
        private SelectorManager selectorManager { get { return target as SelectorManager; } }


        public override void OnInspectorGUI()
        {
            // Draw any public defined variable in the script
            DrawDefaultInspector();

            // Update selected values for tags
            serializedObject.Update();
            selectorManager.SelectTagsByPriority = ListTagSelector.ShowList("Can be selected Tags (sorted by priority):", 
                                                                  serializedObject.FindProperty("SelectTagsByPriority"),
                                                                  selectorManager.SelectTagsByPriority);
            selectorManager.AttackTagsByPriority = ListTagSelector.ShowList("Can be attacked Tags (sorted by priority):",
                                                                  serializedObject.FindProperty("AttackTagsByPriority"),
                                                                  selectorManager.AttackTagsByPriority);
            selectorManager.BuilderTag = EditorGUILayout.TagField("Builder Tag: ", selectorManager.BuilderTag);
            selectorManager.BuildingTag = EditorGUILayout.TagField("Building Tag: ", selectorManager.BuildingTag);
            selectorManager.ButtonTag = EditorGUILayout.TagField("Button Tag: ", selectorManager.ButtonTag);

            // Apply changes
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}

