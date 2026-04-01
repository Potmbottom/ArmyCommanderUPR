using System;
using R3;
using UnityEngine;

namespace ArmyCommander
{
    public abstract class BaseControl<TModel> : MonoBehaviour where TModel : class
    {
        protected TModel Model { get; private set; }
        protected CompositeDisposable Disposables { get; private set; }

        // Set once by SpawnService via pool actionOnGet — internal to keep it out of public API
        internal Action ReleaseToPool { private get; set; }

        public void Bind(TModel model)
        {
            Disposables?.Dispose();
            Disposables = new CompositeDisposable();
            Model = model;
            OnModelBind(model);
        }

        protected abstract void OnModelBind(TModel model);

        public void Release()
        {
            Disposables?.Dispose();
            Disposables = null;
            Model = null;
            ReleaseToPool?.Invoke();
        }

        private void OnDestroy() => Disposables?.Dispose();
    }
}
