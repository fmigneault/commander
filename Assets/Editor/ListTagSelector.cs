using UnityEditor;
using UnityEngine;
using System;

namespace Editor
{
    public static class ListTagSelector
    {
        public static string[] ShowList(string name, SerializedProperty property, string[] originals)
        {            
            EditorGUILayout.PropertyField(property, new GUIContent(name));
            if (property.isExpanded)
            {
                EditorGUI.indentLevel += 1;
                if (originals == null) originals = new string[0];
                var size = EditorGUILayout.IntField("Size", originals.Length);
                var newTags = size >= 0 ? new string[size] : new string[originals.Length];
                for (var i = 0; i < size; i++)
                {
                    if (i >= originals.Length) break;
                    var elementCounter = string.Format("Element {0}", i);
                    newTags[i] = EditorGUILayout.TagField(elementCounter, originals[i]);
                }
                EditorGUI.indentLevel -= 1;
                return newTags;
            }
            return originals;
        }
    }
}