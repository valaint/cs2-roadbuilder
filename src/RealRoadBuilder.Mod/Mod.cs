using Colossal.Logging;
using Game;
using Game.Modding;

namespace RealRoadBuilder.Mod;

public sealed class Mod : IMod
{
    public const string Name = "RealRoad Builder";
    public const string Version = "0.1.0-dev";

    public static readonly ILog Log =
        LogManager.GetLogger(nameof(RealRoadBuilder)).SetShowsErrorsInUI(false);

    public void OnLoad(UpdateSystem updateSystem)
    {
        Log.Info($"Loading {Name} {Version}.");
        Log.Info("Alignment core is available. Network construction is intentionally disabled in v0.1 foundation work.");
    }

    public void OnDispose()
    {
        Log.Info($"Disposing {Name}.");
    }
}
