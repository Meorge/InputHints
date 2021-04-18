using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using System.Text.RegularExpressions;

namespace Meorge.InputHints {
public class InputHintManager : MonoBehaviour
{
    public static InputHintManager instance { get; private set; } = null;
    [SerializeField] InputActionAsset inputActions = null;

    [SerializeField] List<InputHintAsset> hintAssets = new List<InputHintAsset>();

    Dictionary<string, string> iconBindings = new Dictionary<string, string>();

    internal static List<TextMeshProWithInputHints> textMeshes = new List<TextMeshProWithInputHints>();

    void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InputUser.onChange += DeviceChanged;
        }

        else {
            Debug.LogError("More than one instance of InputHintManager exists, deleting newer one");
            Destroy(this);
            return;
        }
        
    }
    
    void Start() {
        print("Starting InputHintManager, rebuilding dictionary");
        RebuildDictionary();
    }

    void OnDestroy() {
        print("Removing local subscriber to onChange");
        InputUser.onChange -= DeviceChanged;
    }

    internal void DeviceChanged(InputUser user, InputUserChange change, InputDevice device) {
        print($"DeviceChanged event triggered: ${change}, ${device}");
        if (change == InputUserChange.ControlsChanged || change == InputUserChange.ControlSchemeChanged || change == InputUserChange.DevicePaired) {
            RebuildDictionary();
            InputHintManager.textMeshes.ForEach((textMesh) => textMesh.Refresh());
        }
    }

    void RebuildDictionary()
    {
        print("Rebuilding dictionary");
        iconBindings.Clear();

        var actionMaps = inputActions.actionMaps;

        string output = "";
        if (Keyboard.current != null) output += $"Current keyboard layout: {Keyboard.current.keyboardLayout}\n";
        
        foreach (InputActionMap item in actionMaps) {
            foreach (var action in item.actions) {
                var actionName = $"{item.name}/{action.name}";

                var displayStringOptions = 
                    InputBinding.DisplayStringOptions.DontOmitDevice |
                    InputBinding.DisplayStringOptions.DontUseShortDisplayNames
                ;

                var displayString = action.GetBindingDisplayString(displayStringOptions);

                iconBindings.Add(actionName, displayString);

                output += $"{actionName} -> {displayString}\n";
            }
        }
        print(output);
        print("===");
    }

    public static bool ActionExists(string actionName) {
        return instance.iconBindings.ContainsKey(actionName);
    }

    public static string GetStringKeyForAction(string actionName) {
        return instance.iconBindings[actionName];
    }

    public static string GetTMProSpriteForAction(string actionName) {
        // actionName is something like "Gameplay/Fire"

        // Let's get the string key that corresponds to this action
        // something like "A [Xbox Wireless Controller]"

        if (!ActionExists(actionName)) {
            Debug.LogError($"Failed to find action in iconBindings: \"{actionName}\"");
            return actionName;
        }

        string key = GetStringKeyForAction(actionName);

        string outputString = "";
        string[] allButtons = key.Split(new string[] { " | " }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string singleControl in allButtons) {        
            // Next, we need to separate this into two strings - one that
            // stores the actual button (like "A"), and one that stores
            // the controller name (like "Xbox Wireless Controller").
            // print($"Parsing \"{singleControl}\"");
            string pattern = @"^(?<buttonName>.+?)\s\[(?<controllerName>.+?)\]$";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(singleControl);

            if (!match.Success) {
                Debug.LogError($"Failed to parse \"{singleControl}\" for action \"{actionName}\"");
                outputString += singleControl;
                continue;
            }

            string buttonName = match.Groups["buttonName"].Value;
            string controllerName = match.Groups["controllerName"].Value;

            // Now that we have this info, let's go find the name of the
            // sprite we want.
            InputHintAsset controller = instance.hintAssets.Find((potentialController) => new Regex(potentialController.controllerName).Match(controllerName).Success);

            if (controller == null) {
                // That controller wasn't found
                Debug.LogError($"Failed to find controller \"{controllerName}\" for action \"{actionName}\"");


                // BUG: If uncommented, this can leave information about non-connected controllers onscreen
                // outputString += singleControl;
                continue;
            }
            // That controller was found
            InputHintAssetItem sprite = controller.items.Find((potentialSprite) => new Regex(potentialSprite.key).Match(buttonName).Success);

            if (sprite == null) {
                // That sprite wasn't found
                Debug.LogError($"Failed to find icon \"{controllerName}.{buttonName}\" for action \"{actionName}\"");
                outputString += buttonName;
                continue;
            }

            outputString += sprite.textMeshProString;

        }

        // Return the relevant sprite tag
        return outputString == "" ? key : outputString;
    }

    public static string ReplaceStringsWithTMProSprites(string text) {
        string pattern = @"{{(.*?)}}";
        Regex regex = new Regex(pattern);
        string result = regex.Replace(text, (match) => GetTMProSpriteForAction(match.Groups[1].Value));
        return result;
    }
}
}
