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
            }

            else {
                Debug.LogError("More than one instance of InputHintManager exists, deleting newer one");
                Destroy(this);
            }
            InputUser.onChange += DeviceChanged;
        }
        
        void Start() {
            RebuildDictionary("Keyboard");
            // Experimenting();
        }

        void OnDestroy() {
            InputUser.onChange -= DeviceChanged;
        }

        static internal void DeviceChanged(InputUser user, InputUserChange change, InputDevice device) {
            if (change == InputUserChange.ControlsChanged) {
                print($"Controls changed to {user.controlScheme.Value.name}");
                instance.RebuildDictionary(user.controlScheme.Value.name);
                textMeshes.ForEach((textMesh) => textMesh.Refresh());
            }
        }

        void Experimenting(string newSchemeName) {
            var activeSchemeIndex = inputActions.FindControlSchemeIndex(newSchemeName);
            // print($"active index is {activeSchemeIndex}");

            // "Keyboard" or "Gamepad"
            // print(inputActions.controlSchemes[activeSchemeIndex].bindingGroup);

            foreach (var actionMap in inputActions.actionMaps) {
                foreach (var action in actionMap.actions) {
                    string s = "";
                    s += $"{actionMap.name}/{action.name} ->";
                    foreach (var control in action.controls) {
                        s += control.path + " ";
                    }
                    print(s);
                }
            }
            // foreach (var actionMap in inputActions.actionMaps) {

            //     foreach (var action in actionMap.actions) {
            //         print(action.name);

            //         foreach (var binding in action.bindings) {
            //             print(binding.effectivePath);
            //         }
            //     }
            //     print("----");
            // }
        }

        void RebuildDictionary(string newSchemeName)
        {
            Experimenting(newSchemeName);
            return;

            iconBindings.Clear();

            var actionMaps = inputActions.actionMaps;

            var deviceLayoutName = string.Empty;
            var controlPath = "";

            // control schemes (right now 0 is gamepad, 1 is keyboard, 2 is Switch Pro Controller)
            var bindingIndex = 0;

            print("----");

            foreach (InputActionMap item in actionMaps) {
                foreach (var action in item.actions) {
                    var actionName = $"{item.name}/{action.name}";

                    var displayStringOptions = InputBinding.DisplayStringOptions.DontOmitDevice | InputBinding.DisplayStringOptions.DontUseShortDisplayNames;

                    var displayString = action.GetBindingDisplayString(bindingIndex, out deviceLayoutName, out controlPath, displayStringOptions);

                    iconBindings.Add(actionName, displayString);

                    print($"{actionName} -> {displayString}; deviceLayoutName={deviceLayoutName}, controlPath={controlPath}");

                    var s = "[";
                    foreach (var b in action.bindings) {
                        // print($"{b.effectivePath}");
                        s += b.effectivePath + " ";
                    }
                    s += "]";
                    print(s);
                }
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
                InputHintAsset controller = instance.hintAssets.Find((potentialController) => potentialController.controllerName == controllerName);

                if (controller == null) {
                    // That controller wasn't found
                    Debug.LogError($"Failed to find controller \"{controllerName}\" for action \"{actionName}\"");
                    outputString += singleControl;
                    continue;
                }
                // That controller was found
                InputHintAssetItem sprite = controller.items.Find((potentialSprite) => potentialSprite.key == buttonName);

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
