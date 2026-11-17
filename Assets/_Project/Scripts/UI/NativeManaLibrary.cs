using System;
using UnityEngine;
namespace HearthstoneClone.UI
{
    [Serializable] public class NativeManaCurve { public string property; public AnimationCurve curve; }
    [Serializable] public class NativeManaClip { public float duration; public NativeManaCurve[] curves; }
    public class NativeManaLibrary : ScriptableObject
    {
        public NativeManaClip birth, used, restore;
    }
}
