using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MightyAddressables
{
    public sealed class SoundLibrary : AddressableDataLibrary<SoundLibrary, SoundEvent>
    {

#if UNITY_EDITOR
        protected override string DefaultAssetPath => AddressableDataLibrary.DataLibraryPath + "/SoundLibrary";
#endif

    }
}