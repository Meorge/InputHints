using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using System.Text.RegularExpressions;
using System.Linq;

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
                // Debug.LogError("More than one instance of InputHintManager exists, deleting newer one");
                Destroy(this);
                return;
            }    
        }
        
        void OnDestroy() {
            // print("Removing local subscriber to onChange");
            InputUser.onChange -= DeviceChanged;
        }

        internal void DeviceChanged(InputUser user, InputUserChange change, InputDevice device) {
            // print($"DeviceChanged event triggered: {change}");
            if (change == InputUserChange.ControlsChanged || change == InputUserChange.ControlSchemeChanged || change == InputUserChange.DevicePaired) {
                InputHintManager.textMeshes.ForEach((textMesh) => textMesh.Refresh());
            }
        }

        public static bool ActionExists(string actionName) {
            return instance.iconBindings.ContainsKey(actionName);
        }

        public static string GetStringKeyForAction(string actionName) {
            return instance.iconBindings[actionName];
        }

        public static string GetTMProSpriteForAction(string actionName) {
            // actionName is something like "Gameplay/Fire"
            var action = instance.inputActions.FindAction(actionName);

            // List of path strings for controls
            var controls = action.controls.Select(control => control.path);

            // This regex pattern will allow us to split up controller paths.
            // For example, "/XboxOneGampadMacOSWireless/buttonEast" will be split
            // into "XboxOneGamepadMacOSWireless" and "buttonEast".
            var splitControlPattern = @"^/(?<DeviceName>.+?)/(?<ControlName>.+)$";
            var splitControlRegex = new Regex(splitControlPattern);

            string output = "";
            foreach (var control in controls) {
                Match controlMatch = splitControlRegex.Match(control);

                if (!controlMatch.Success) {
                    Debug.LogError($"Failed to parse \"{control}\" for action \"{actionName}\"");
                    output += control;
                    continue;
                }

                var deviceName = controlMatch.Groups["DeviceName"].Value;
                var controlName = controlMatch.Groups["ControlName"].Value;

                // Find the hint asset that corresponds to this controller
                InputHintAsset hintAsset = instance.hintAssets.Find(potential => new Regex(potential.controllerName).Match(deviceName).Success);

                if (hintAsset == null) {
                    Debug.LogError($"Failed to find hint asset for controller \"{deviceName}\" for action \"{actionName}\"");
                    continue;
                }

                InputHintAssetItem item = hintAsset.items.Find(potential => new Regex(potential.key).Match(controlName).Success);
                
                if (item == null) {
                    Debug.LogError($"Failed to find item for control name \"{controlName}\" on controller \"{deviceName}\" for action \"{actionName}\"");
                    output += controlName;
                    continue;
                }

                output += item.textMeshProString;
            }

            return output;
        }

        public static string ReplaceStringsWithTMProSprites(string text) {
            string pattern = @"{{(.*?)}}";
            Regex regex = new Regex(pattern);
            string result = regex.Replace(text, (match) => GetTMProSpriteForAction(match.Groups[1].Value));
            return result;
        }
    }
}
