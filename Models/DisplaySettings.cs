namespace vTFMS.Models;

public class DisplaySettings
{
    public string BackgroundColor { get; set; } = "#000000";
    public string BoundaryColor { get; set; } = "#AAFF00";
    public string TraconColor { get; set; } = "#00CCFF";
    public string ArtccColor { get; set; } = "#CC0000";
    public string AirportColor { get; set; } = "#00CCFF";
    public string VorColor { get; set; } = "#FF9900";
    public string NdbColor { get; set; } = "#FF00FF";
    public string FixColor { get; set; } = "#FFFFFF";
    public string JetRoutesColor { get; set; } = "#FF0000";
    public string VictorRoutesColor { get; set; } = "#800080";
    public string DataBlockFont { get; set; } = "Courier New";
    public double DataBlockFontSize { get; set; } = 10;
    public string MapLabelFont { get; set; } = "Courier New";
    public double MapLabelFontSize { get; set; } = 9;
    public string DataBlockColor { get; set; } = "#00CCFF";
    public string MapLabelColor { get; set; } = "#00CCFF";

    public DisplaySettings Clone() => new()
    {
        BackgroundColor = BackgroundColor,
        BoundaryColor = BoundaryColor,
        TraconColor = TraconColor,
        ArtccColor = ArtccColor,
        AirportColor = AirportColor,
        VorColor = VorColor,
        NdbColor = NdbColor,
        FixColor = FixColor,
        JetRoutesColor = JetRoutesColor,
        VictorRoutesColor = VictorRoutesColor,
        DataBlockFont = DataBlockFont,
        DataBlockFontSize = DataBlockFontSize,
        MapLabelFont = MapLabelFont,
        MapLabelFontSize = MapLabelFontSize,
        DataBlockColor = DataBlockColor,
        MapLabelColor = MapLabelColor
    };
}