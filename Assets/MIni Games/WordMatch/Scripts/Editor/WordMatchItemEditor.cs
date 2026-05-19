using UnityEditor;

namespace CoreLoop.WordMatch
{
    [CustomEditor(typeof(WordMatchItem))]
    public class WordMatchItemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var cardTypeProp = serializedObject.FindProperty("cardType");
            EditorGUILayout.PropertyField(cardTypeProp);

            var type = (WordMatchItem.CardType)cardTypeProp.enumValueIndex;

            if (type == WordMatchItem.CardType.ImageCard)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contentImage"));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contentText"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("audioButton"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("matchPoint"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
