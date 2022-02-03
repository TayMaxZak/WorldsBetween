using UnityEditor;
using UnityEngine;

public class CreateItemAsset : MonoBehaviour
{
	[MenuItem("CustomAssets/Create Item Asset")]
	static void CreateAsset()
	{
		// A script (potentially representing an Item) is selected
		if (!(Selection.activeObject is MonoScript script))
			return;

		// Not including folders in project view
		if (Selection.assetGUIDs.Length == 0)
			return;

		System.Type type = script.GetClass();

		string nameToUse = type.ToString();

		Debug.Log(nameToUse + " might be " + typeof(Item));

		// Is an Item
		if (!type.Equals(typeof(Item)) && !type.IsSubclassOf(typeof(Item)))
			return;

		Debug.Log(nameToUse + " is " + typeof(Item));

		string folderPath = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
		if (folderPath.Contains("."))
			folderPath = folderPath.Remove(folderPath.LastIndexOf('/') + 1);
		Debug.Log(folderPath);

		ScriptableObject instance = ScriptableObject.CreateInstance(nameToUse);

		// Avoid overwriting
		AssetDatabase.CreateAsset(instance, folderPath + nameToUse + ".asset");
	}
}