using Godot;

namespace ActsFromThePast;

public partial class CeilingDustEffect : NSts1Effect
{
    private int _count = 20;
    private float _x;
    private Action<NSts1Effect> _addEffectCallback;
    
    public static CeilingDustEffect Create(Action<NSts1Effect> addEffectCallback)
    {
        var effect = new CeilingDustEffect();
        effect._addEffectCallback = addEffectCallback;
        effect.Setup();
        return effect;
    }
    
    protected override void Initialize()
    {
        _x = (float)GD.RandRange(0.0, 1870.0);
    }
    
    protected override void Update(float delta)
    {
        if (_count != 0)
        {
            int num = (int)(GD.Randi() % 9); // 0-8
            _count -= num;
            
            const float ceilingY = 640f - 540f; // floorY + 640 in StS1, converted to Godot coords (540 is our floorY)
            
            for (int i = 0; i < num; i++)
            {
                var fallingDust = FallingDustEffect.Create(_x, ceilingY);
                _addEffectCallback?.Invoke(fallingDust);
                
                if (GD.Randf() < 0.8f)
                {
                    var dustCloud = CeilingDustCloudEffect.Create(_x, ceilingY);
                    _addEffectCallback?.Invoke(dustCloud);
                }
            }
            
            if (_count <= 0)
            {
                IsDone = true;
            }
        }
    }
}
