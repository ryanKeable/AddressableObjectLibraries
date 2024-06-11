using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;


namespace MightyAddressables
{
    public class SoundEventWizard : AddressableEntryWizard<SoundLibrary, SoundEvent>
    {
        protected override AddressableDataLibraryEditor<SoundLibrary, SoundEvent> LibraryEditorInstance { get => SoundLibraryEditor.instance; }
        protected override AddressableDataLibrary<SoundLibrary, SoundEvent> LibraryInstance { get => SoundLibrary.instance; }


        protected override bool CheckForTarget()
        {
            return true;
        }

        protected override bool CheckForTargetOfType()
        {
            return true;
        }

        protected override void CreateNewAsset()
        {
            CreatedAsset = SoundEvent.CreateInstance<SoundEvent>();
        }

    }
}