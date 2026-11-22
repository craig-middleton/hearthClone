using System;
using UnityEngine;
namespace HearthstoneClone.UI
{
    public class NativeHeroDeathLibrary:ScriptableObject
    {
        [Serializable] public class Curve{public string path,property,type;public AnimationCurve curve;}
        public GameObject sourcePrefab;
        public Curve[] crack,debris,light;
        public Vector3[] crackVertices;public Vector2[] crackUvs;public int[] crackTriangles;public Color[] crackColors;
        public Texture crackPaint;public Material crackUiMaterial;
        public AnimationCurve crackClock,explodeClock;
        public float crackDuration=.5f,explodeSlowDuration=.8f,holdDuration=1.3f;
        public Vector3 debrisCenter;public float debrisWidth;
        public AudioClip crackSound,explodeSound;
    }
}
