using System.Collections.Generic;
using UnityEngine;

namespace Meorge.InputHints {
[CreateAssetMenu(fileName = "New Input Hint Asset", menuName = "Input Hints/Input Hint Asset", order = 1)]
public class InputHintAsset : ScriptableObject
{
    public string controllerName = "";
    public List<InputHintAssetItem> items = new List<InputHintAssetItem>();
}

[System.Serializable]
public class InputHintAssetItem {
    public string key = "";
    public string textMeshProString = "";
}
}