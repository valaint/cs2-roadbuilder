using Colossal.Logging;
using Game;
using Game.Modding;

namespace RealRoadBuilder.Mod;

public sealed class Mod : IMod
{
    public const string Name = "RealRoad Builder";
    public const string Version = "0.3.0-dev";

    public static readonly ILog Log =
        LogManager.GetLogger("RealRoadBuilder").SetShowsErrorsInUI(false);

    public void OnLoad(UpdateSystem updateSystem)
    {
        Log.Info($"Loading {Name} {Version}.");
        Log.Info("Terrain-aware corridor routing and preliminary vertical profile optimization are available in the core. In-game network construction remains intentionally disabled.");
    }

    public void OnDispose()
    {
        Log.Info($"Disposing {Name}.");
    }
}
