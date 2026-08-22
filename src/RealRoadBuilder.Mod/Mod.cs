using Colossal.Logging;
using Game;
using Game.Modding;

namespace RealRoadBuilder.Mod;

public sealed class Mod : IMod
{
    public const string Name = "RealRoad Builder";
    public const string Version = "0.2.0-dev";

    public static readonly ILog Log =
        LogManager.GetLogger("RealRoadBuilder").SetShowsErrorsInUI(false);

    public void OnLoad(UpdateSystem updateSystem)
    {
        Log.Info($"Loading {Name} {Version}.");
        Log.Info("Horizontal transition and vertical profile cores are available. In-game network construction remains intentionally disabled.");
    }

    public void OnDispose()
    {
        Log.Info($"Disposing {Name}.");
    }
}
