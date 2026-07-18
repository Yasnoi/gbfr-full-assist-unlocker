using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;

namespace GBFR.InfinityFullAssist;

public sealed class Startup : IMod
{
    private Mod? _mod;

    public void StartEx(IModLoaderV1 loaderApi, IModConfigV1 modConfig)
    {
        var loader = (IModLoader)loaderApi;
        var config = (IModConfig)modConfig;
        var logger = (ILogger)loader.GetLogger();
        _mod = new Mod(loader, logger, config);
    }

    public void Suspend()
    {
    }

    public void Resume()
    {
    }

    public void Unload() => DisposeMod();

    public bool CanUnload() => true;

    public bool CanSuspend() => false;

    public Action Disposing => DisposeMod;

    private void DisposeMod() =>
        Interlocked.Exchange(ref _mod, null)?.Dispose();
}
