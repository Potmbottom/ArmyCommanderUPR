using System;
using R3;
using UnityEngine;

namespace ArmyCommander
{
    public interface IBarrackSlotPModel : IDisposable
    {
        Vector3 BuildPoint { get; }
        ReadOnlyReactiveProperty<TroopType> TroopType { get; }
        ReadOnlyReactiveProperty<bool> IsPlayerInZone { get; }

        void SetBuildPoint(Vector3 point);
        void SetTroopType(TroopType type);
        void SetPlayerInZone(bool value);
    }
}
