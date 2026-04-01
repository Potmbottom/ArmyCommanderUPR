using UnityEngine;

namespace ArmyCommander
{
    public class ResourceDropControl : BaseControl<IResourceDropPModel>
    {
        private const string PlayerTag = "Player";

        protected override void OnModelBind(IResourceDropPModel model)
        {
            transform.position = model.Position;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Model == null || Model.IsCollected) return;
            if (other == null) return;
            if (!other.CompareTag(PlayerTag)) return;
            Model.Collect();
        }
    }
}
