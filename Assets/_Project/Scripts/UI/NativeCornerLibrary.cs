using System;
using UnityEngine;

namespace HearthstoneClone.UI
{
    // A narrow translation of the recovered ORG FSM data, with no client assemblies.
    public class NativeCornerLibrary : ScriptableObject
    {
        public Machine[] machines;
        [Serializable] public class Machine
        {
            public string path, start;
            public State[] states;
        }
        [Serializable] public class State
        {
            public string name;
            public Action[] actions;
            public Transition[] transitions;
        }
        [Serializable] public class Transition { public string eventName, toState; }
        [Serializable] public class Action
        {
            public string type;
            public Parameter[] parameters;
            public string[] soundPaths;
            public AudioClip[] sounds;
            public float[] weights;
        }
        [Serializable] public class Parameter
        {
            public string name, text, variable, path;
            public bool none;
            public float number;
            public Vector3 vector;
        }
    }
}
