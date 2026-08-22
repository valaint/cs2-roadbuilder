using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using RealRoadBuilder.Mod.Settings;
using RealRoadBuilder.Mod.Tools;

namespace RealRoadBuilder.Mod;

public sealed class Mod : IMod
{
    public const string Name = "RealRoad Builder";
    public const string Version = "0.7.0-dev";

    public static readonly ILog Log =
        LogManager.GetLogger("RealRoadBuilder").SetShowsErrorsInUI(false);

    public static RealRoadBuilderSettings Settings { get; private set; } = null!;

    public void OnLoad(UpdateSystem updateSystem)
    {
        Log.Info($"Loading {Name} {Version}.");

        Settings = new RealRoadBuilderSettings(this);
        Settings.RegisterInOptionsUI();
        AssetDatabase.global.LoadSettings(
            Name,
            Settings,
            new RealRoadBuilderSettings(this));

        updateSystem.UpdateAt<RealRoadPreviewToolSystem>(SystemUpdatePhase.ToolUpdate);

        Log.Info(
            "Read-only CS2 planning preview with live terrain, buildings, networks, and water is registered. " +
            "Network construction remains intentionally disabled.");
    }

    public void OnDispose()
    {
        Settings?.UnregisterInOptionsUI();
        Log.Info($"Disposing {Name}.");
    }
}
