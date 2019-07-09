using UnityEngine;
using System;
using System.Collections.Generic;


namespace MN
{
    public class SingletonMono<T> : MonoBehaviour where T : Component
    {
        protected static T _Instance = null;

        public static T Instance
        {
            get
            {

                if (_Instance == null) {

                    _Instance = FindObjectOfType(typeof(T)) as T;

                    if (_Instance == null){
                        GameObject obj = new GameObject();
                        obj.hideFlags = HideFlags.DontSave;
                        obj.name = typeof(T).ToString();
                        _Instance = obj.AddComponent(typeof(T)) as T;
                    }
                }

                return _Instance;
            }
        }
    }
}