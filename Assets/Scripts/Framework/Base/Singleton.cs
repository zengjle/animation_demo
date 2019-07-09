using UnityEngine;
using System;
using System.Collections.Generic;


namespace MN
{
    public class Singleton<T> : IDisposable where T : class, new()
    {
        protected static T _Instance = null;
        protected bool _Disposed = false;

        public static T Instance
        {
            get {
                if (_Instance == null)
                    _Instance = new T() as T;

                return _Instance;
            }
        }

        ~Singleton()
        {
            Dispose(false);
        }

        protected virtual void Release()
        {
                if (_Instance != null) {
                    Dispose();
                    _Instance = null;
                }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // 检查 Dispose 是不是已经被调用了
            if (_Disposed) {
                return;
            }

            if(disposing) {
                // 释放被托管的资源
            }

            _Disposed = true;
        }
    }
}