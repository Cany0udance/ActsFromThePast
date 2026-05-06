using BaseLib.Config;

namespace ActsFromThePast;

public class ActsFromThePastConfig : SimpleModConfig
{
    
    [ConfigHoverTip]
    public static bool RebalancedMode { get; set; } = false;
    
    [ConfigHoverTip]
    public static bool AllowNonLegacySharedEventsInLegacyActs { get; set; } = true;

    [ConfigHoverTip]
    public static bool AllowLegacySharedEventsInNonLegacyActs { get; set; } = false;
    
    /*
     
     Currently unused
    
    [ConfigHoverTip]
    public static bool DarvOnlyInLegacyActs { get; set; } = false;
    
    */
    
    [ConfigHoverTip]
    public static bool LegacyEnemiesGiveClassicSlimed { get; set; } = false;
}