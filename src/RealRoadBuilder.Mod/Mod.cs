using Colossal.Logging;
using Game;
using Game.Modding;

namespace RealRoadBuilder.Mod;

public sealed class Mod : IMod
{
    public const string Name = "RealRoad Builder";
    public const string Version = "0.5.0-dev";

    public static readonly ILog Log =
        LogManager.GetLogger("RealRoadBuilder").SetShowsErrorsInUI(false);

    public void OnLoad(UpdateSystem updateSystem)
    {
        Log.Info($"Loading {Name} {Version}.");
        Log.Info("The core can now iteratively route, fit, evaluate terrain/earthwork/structures, and feed costly solutions back into corridor search. In-game network construction remains intentionally disabled.");
    }

    public void OnDispose()
    {
        Log.Info($"Disposing {Name}.");
    }
}
