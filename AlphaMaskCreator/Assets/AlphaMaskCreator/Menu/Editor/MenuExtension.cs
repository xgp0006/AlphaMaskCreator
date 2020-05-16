using UnityEngine;
using UnityEditor;


namespace AlphaMaskCreator
{
    [ExecuteInEditMode]
    public class MenuExtension : MonoBehaviour
    {

        [MenuItem("GameObject/Alpha Mask Creator/Add Alpha Mask Creator", false, 10)]
        static void AddBatchFunctions(MenuCommand menuCommand)
        {
            // create new gameobject
            GameObject go = new GameObject("Alpha Mask Creator");

            // add this component
            go.AddComponent<AlphaMaskCreator>();

            // ensure gameobject gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

            // tegister the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);

            Selection.activeObject = go;

        }

    }

}
