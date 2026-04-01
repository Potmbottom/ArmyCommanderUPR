using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ArmyCommander
{
    public class GameInstaller : LifetimeScope
    {
        [SerializeField] private GameRoot _gameRoot;
        [SerializeField] private VirtualJoystick _virtualJoystick;

        protected override void Configure(IContainerBuilder builder)
        {
            // PresentationModels
            builder.Register<FieldPModel>(Lifetime.Singleton).As<IFieldPModel>();
            builder.Register<TrainingFieldPModel>(Lifetime.Singleton).As<ITrainingFieldPModel>();
            builder.Register<PlayerPModel>(Lifetime.Singleton).As<IPlayerPModel>();
            builder.Register<ArmyUpgradePModel>(Lifetime.Singleton).As<IArmyUpgradePModel>();
            builder.Register<ResourcePModel>(Lifetime.Singleton).As<IResourcePModel>();
            builder.Register<UIModel>(Lifetime.Singleton).As<IUIModel>();

            // Input
#if UNITY_ANDROID && !UNITY_EDITOR
            builder.RegisterComponent(_virtualJoystick);
            builder.Register<AndroidInputProvider>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
#else
            builder.Register<PCInputProvider>(Lifetime.Singleton).As<IInputProvider>();
#endif

            // Services — order defines ITickable execution order
            builder.RegisterEntryPoint<InputService>();
            builder.RegisterEntryPoint<SpawnService>();
            builder.RegisterEntryPoint<AIService>();
            builder.RegisterEntryPoint<BarrackService>().AsSelf();
            builder.RegisterEntryPoint<ProjectileService>();
            builder.RegisterEntryPoint<TransformService>();
            builder.Register<ResourceService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<UIService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ArmyUpgradeService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

            // Root
            builder.RegisterComponent(_gameRoot).AsImplementedInterfaces().AsSelf();
        }
    }
}
