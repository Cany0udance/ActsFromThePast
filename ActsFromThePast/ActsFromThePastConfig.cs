using BaseLib.Config;

namespace ActsFromThePast;

public class ActsFromThePastConfig : SimpleModConfig
{
    
    [ConfigHoverTip]
    public static bool RebalancedMode { get; set; } = false;
    
    public static bool LegacyActsOnly { get; set; } = false;
    
    [ConfigHoverTip]
    public static bool DarvOnlyInLegacyActs { get; set; } = false;
    
    [ConfigHoverTip]
    public static bool LegacyEnemiesGiveClassicSlimed { get; set; } = false;
}