using ImpliciX.SharedKernel.Tools;

namespace ImpliciX.SharedKernel.Modules
{
    public static class ImpliciXModuleExt
    {
        public static void InitializeDependencies(this IImpliciXModule[] modules, IConfigureDependency configureDependency)
        {
            foreach (var boilerModule in modules)
            {
                boilerModule.InitializeDependencies(configureDependency);
            }
        }

        public static void InitializeResources(this IImpliciXModule[] modules, IProvideDependency provideDependency)
        {
            foreach (var boilerModule in modules)
            {
                boilerModule.InitializeResources(provideDependency);
            }
        }

        public static void StartAll(this IImpliciXModule[] modules)
        {
            foreach (var boilerModule in modules)
            {
                boilerModule.Start();
            }
        }

        public static void StopAll(this IImpliciXModule[] modules)
        {
            foreach (var boilerModule in modules)
            {
                boilerModule.Stop();
            }
        }

        public static void DisposeAll(this IImpliciXModule[] modules)
        {
            foreach (var boilerModule in modules)
            {
                boilerModule.Dispose();
            }
        }
    }
}