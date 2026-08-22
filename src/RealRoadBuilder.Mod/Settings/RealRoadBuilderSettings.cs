using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Unity.Entities;
using RealRoadBuilder.Mod.Tools;

namespace RealRoadBuilder.Mod.Settings;

[FileLocation(Mod.Name)]
[SettingsUIGroupOrder(PlanningGroup, PreviewGroup)]
[SettingsUIShowGroupName(PlanningGroup, PreviewGroup)]
public sealed class RealRoadBuilderSettings : ModSetting
{
    public const string PlanningGroup = "Planning";
    public const string PreviewGroup = "Preview";

    public RealRoadBuilderSettings(IMod mod)
        : base(mod)
    {
        SetDefaults();
    }

    [SettingsUISection(PlanningGroup)]
    [SettingsUISlider(min = 60, max = 120, step = 20)]
    public int DesignSpeedKph { get; set; }

    [SettingsUISection(PlanningGroup)]
    [SettingsUISlider(min = 20f, max = 80f, step = 10f)]
    public float SearchCellSizeMeters { get; set; }

    [SettingsUISection(PlanningGroup)]
    [SettingsUISlider(min = 1, max = 4, step = 1)]
    public int MaximumPlanningIterations { get; set; }

    [SettingsUISection(PlanningGroup)]
    public bool AllowExceptionalGeometry { get; set; }

    [SettingsUISection(PreviewGroup)]
    [SettingsUISlider(min = 0.5f, max = 4f, step = 0.5f)]
    public float PreviewLineWidth { get; set; }

    [SettingsUISection(PreviewGroup)]
    [SettingsUIButton]
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

    [SettingsUISection(PreviewGroup)]
    [SettingsUIButton]
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

    [SettingsUISection(PreviewGroup)]
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
        PreviewLineWidth = 1.5f;
    }
}
