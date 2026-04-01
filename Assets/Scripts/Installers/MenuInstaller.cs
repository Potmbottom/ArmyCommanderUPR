using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ArmyCommander
{
    public class MenuInstaller : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<MenuRoot>().AsImplementedInterfaces().AsSelf();
        }
    }
}
