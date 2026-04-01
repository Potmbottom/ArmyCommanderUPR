using System;
using R3;
using UnityEngine;

namespace ArmyCommander
{
    public interface IResourceDropPModel : IDisposable
    {
        int DataIndex { get; }
        ResourceType ResourceType { get; }
        int Amount { get; }
        Vector3 Position { get; }
        bool IsCollected { get; }
        Observable<IResourceDropPModel> OnCollected { get; }

        void Collect();
    }
}
