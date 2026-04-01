using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ArmyCommander
{
    public class BootInstaller : LifetimeScope
    {
        [SerializeField] private TroopsConfig _troopsConfig;
        [SerializeField] private ProjectileConfig _projectileConfig;
        [SerializeField] private ResourceDropConfig _resourceDropConfig;
        [SerializeField] private LevelConfig _levelConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_troopsConfig);
            builder.RegisterInstance(_projectileConfig);
            builder.RegisterInstance(_resourceDropConfig);
            builder.RegisterInstance(_levelConfig);
            builder.Register<PlayerPrefsStorage>(Lifetime.Singleton).As<ISaveStorage>();
        }
    }
}
