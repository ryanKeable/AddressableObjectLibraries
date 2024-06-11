using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace MightyAddressables
{
    public sealed class SoundLibraryEditorGUI : AddressableDataLibraryEditorGUI<SoundLibrary, SoundEvent>
    {
        protected override void OpenNewEntryWizard()
        {
            SoundEventWizard.Init<SoundEventWizard>(WizardLabel);
        }

        public static string SoundEventIdentifierPopup(string soundKey, string filter = null)
        {
            SoundLibrary library = SoundLibrary.instance;
            if (library == null) return null;

            List<string> keys = library.AllEntryNames.ToList();

            if (!string.IsNullOrEmpty(filter))
            {
                // Go in reverse so I can modify the list as I go
                int count = keys.Count - 1;
                for (int i = count; i >= 0; i--)
                {
                    if (keys[i].ToLower().Contains(filter.ToLower())) continue;
                    keys.RemoveAt(i);
                }
            }

            if (string.IsNullOrEmpty(filter) || keys.Count < 1)
            {
                keys.Insert(0, "none");
            }

            int selected = keys.IndexOf(soundKey);
            if (selected < 0) selected = 0;

            selected = EditorGUILayout.Popup("soundKey", selected, keys.ToArray());
            return keys[selected];
        }


    }
}

