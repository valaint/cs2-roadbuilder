using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using RealRoadBuilder.Mod.Tools;
using Unity.Entities;

namespace RealRoadBuilder.Mod.Settings;

[FileLocation(Mod.Name)]
[SettingsUITabOrder(GeneralTab)]
[SettingsUIGroupOrder(PlanningGroup, WorldGroup, PreviewGroup)]
[SettingsUIShowGroupName(PlanningGroup, WorldGroup, PreviewGroup)]
public sealed class RealRoadBuilderSettings : ModSetting
{
    public const string GeneralTab = "General";
    public const string PlanningGroup = "Planning";
    public const string WorldGroup = "Live world";
    public const string PreviewGroup = "Preview";

    public RealRoadBuilderSettings(IMod mod)
        : base(mod)
    {
        SetDefaults();
    }

    [SettingsUISection(GeneralTab, PlanningGroup)]
    [SettingsUISlider(min = 60, max = 120, step = 20)]
    public int DesignSpeedKph { get; set; }

    [SettingsUISection(GeneralTab, PlanningGroup)]
    [SettingsUISlider(min = 20f, max = 80f, step = 10f)]
    public float SearchCellSizeMeters { get; set; }

    [SettingsUISection(GeneralTab, PlanningGroup)]
    [SettingsUISlider(min = 1, max = 4, step = 1)]
    public int MaximumPlanningIterations { get; set; }

    [SettingsUISection(GeneralTab, PlanningGroup)]
    public bool AllowExceptionalGeometry { get; set; }

    [SettingsUISection(GeneralTab, WorldGroup)]
    public bool UseLiveWorldConstraints { get; set; }

    [SettingsUISection(GeneralTab, WorldGroup)]
    public bool BuildingsAreHardObstacles { get; set; }

    [SettingsUISection(GeneralTab, WorldGroup)]
    [SettingsUISlider(min = 0f, max = 30f, step = 2f)]
    public float BuildingClearanceMeters { get; set; }

    [SettingsUISection(GeneralTab, WorldGroup)]
    [SettingsUISlider(min = 0f, max = 40f, step = 2f)]
    public float ExistingRoadInfluenceMeters { get; set; }

    [SettingsUISection(GeneralTab, WorldGroup)]
    [SettingsUISlider(min = 0f, max = 50f, step = 2f)]
    public float SurfaceRailInfluenceMeters { get; set; }

    [SettingsUISection(GeneralTab, WorldGroup)]
    [SettingsUISlider(min = 0.1f, max = 3f, step = 0.1f)]
    public float MinimumBridgeWaterDepthMeters { get; set; }

    [SettingsUISection(GeneralTab, PreviewGroup)]
    [SettingsUISlider(min = 0.5f, max = 4f, step = 0.5f)]
    public float PreviewLineWidth { get; set; }

    [SettingsUISection(GeneralTab, PreviewGroup)]
    [SettingsUIButton()]
    public bool ActivatePreviewTool
    {
        set
        {
            if (!value)
            {
                return;
            }

            World.DefaultGameObjectInjectionWorld?
                .GetExistingSystemManaged<RealRoadPreviewToolSystem>()?
                .ToggleTool(true);
        }
    }

    [SettingsUISection(GeneralTab, PreviewGroup)]
    [SettingsUIButton()]
    public bool ClearPreview
    {
        set
        {
            if (!value)
            {
                return;
            }

            World.DefaultGameObjectInjectionWorld?
                .GetExistingSystemManaged<RealRoadPreviewToolSystem>()?
                .ClearPreview();
        }
    }

    [SettingsUISection(GeneralTab, PreviewGroup)]
    [SettingsUIMultilineText]
    public string PreviewStatus =>
        World.DefaultGameObjectInjectionWorld?
            .GetExistingSystemManaged<RealRoadPreviewToolSystem>()?
            .StatusText ?? "Preview tool is not active.";

    public override void SetDefaults()
    {
        DesignSpeedKph = 100;
        SearchCellSizeMeters = 40f;
        MaximumPlanningIterations = 3;
        AllowExceptionalGeometry = false;

        UseLiveWorldConstraints = true;
        BuildingsAreHardObstacles = true;
        BuildingClearanceMeters = 10f;
        ExistingRoadInfluenceMeters = 12f;
        SurfaceRailInfluenceMeters = 16f;
        MinimumBridgeWaterDepthMeters = 0.25f;

        PreviewLineWidth = 1.5f;
    }
}
