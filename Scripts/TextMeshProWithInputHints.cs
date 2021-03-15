using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

namespace Meorge.InputHints {
[RequireComponent(typeof(TextMeshProUGUI))]
public class TextMeshProWithInputHints : MonoBehaviour
{
    TMP_Text textMesh = null;
    void Awake() {
        textMesh = GetComponent<TMP_Text>();
        textMesh.textPreprocessor = new TextMeshProInputHintPreprocessor();

        InputHintManager.textMeshes.Add(this);
    }

    void OnDestroy() {
        InputHintManager.textMeshes.Remove(this);
    }

    internal void Refresh() {
        textMesh.ForceMeshUpdate(forceTextReparsing: true);
    }
}

public class TextMeshProInputHintPreprocessor : ITextPreprocessor {
    public string PreprocessText(string text) {
        return InputHintManager.ReplaceStringsWithTMProSprites(text);
    }
}
}