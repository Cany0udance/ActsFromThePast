using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ActsFromThePast;

public partial class InteractableTorchEffect : Control
{
    private const string LOG_TAG = "[ActsFromThePast]";
    private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";
    
    public enum TorchSize { S, M, L }
    
    private float _x;
    private float _y;
    private bool _activated = true;
    private float _particleTimer = 0f;
    private const float ParticleEmitInterval = 0.1f;
    private TorchSize _size;
    private float _scale;
    private Sprite2D _sprite;
    private Color _color;
    private bool _initialized = false;
    
    // Child effects
    private List<TorchParticleSEffect> _particlesS = new();
    private List<LightFlareSEffect> _flaresS = new();
    private List<TorchParticleMEffect> _particlesM = new();
    private List<LightFlareMEffect> _flaresM = new();
    private List<TorchParticleLEffect> _particlesL = new();
    private List<LightFlareLEffect> _flaresL = new();
    
    // Static flag for green rendering (shared across all torches)
    public static bool RenderGreen = false;
    
    public static InteractableTorchEffect Create(float x, float y, TorchSize size = TorchSize.M)
    {
        var effect = new InteractableTorchEffect();
        effect._x = x;
        effect._y = y;
        effect._size = size;
        return effect;
    }
    
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
    
        MouseFilter = MouseFilterEnum.Stop;
    
        var textureRegion = LibGdxAtlas.GetRegion(AtlasPath, "env/torch");
        if (textureRegion == null)
        {
            return;
        }
    
        _color = new Color(1f, 1f, 1f, 0.4f);
    
        switch (_size)
        {
            case TorchSize.S:
                _scale = 0.6f;
                break;
            case TorchSize.M:
                _scale = 1.0f;
                break;
            case TorchSize.L:
                _scale = 1.4f;
                break;
        }
    
        // Convert StS1 coordinates to centered Godot coordinates
        float halfWidth = 960f;
        float halfHeight = 568f;
        float torchX = _x - halfWidth - 23f;
        float torchY = halfHeight - _y;
    
        // Set Control size and position (for click detection)
        var clickSize = new Vector2(50f, 60f);
        Size = clickSize;
        Position = new Vector2(torchX - clickSize.X / 2f, torchY - clickSize.Y / 2f);
    
        // Create sprite as child, centered within the Control
        _sprite = new Sprite2D();
        _sprite.Texture = textureRegion.Value.Texture;
        _sprite.RegionEnabled = true;
        _sprite.RegionRect = textureRegion.Value.Region;
        _sprite.Centered = true;
        _sprite.Position = new Vector2(clickSize.X / 2f, clickSize.Y / 2f + 24f);
        _sprite.Scale = new Vector2(_scale, _scale);
        _sprite.Modulate = _color;
        AddChild(_sprite);
    
        // Connect signals
        GuiInput += OnGuiInput;
        GetTree().ProcessFrame += OnProcessFrame;
        
    }
    
    public override void _ExitTree()
    {
        if (_initialized)
        {
            GuiInput -= OnGuiInput;
            GetTree().ProcessFrame -= OnProcessFrame;
        }
        base._ExitTree();
    }
    
    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                _activated = !_activated;
            
                if (_activated)
                {
                    ModAudio.Play("general", "fire_ignite", -10f, 0.4f); // pitch variation like the original
                }
                else
                {
                    ModAudio.Play("general", "torch_extinguish", -10f);
                }
                
                GetViewport().SetInputAsHandled();
            }
        }
    }
    
    private void OnProcessFrame()
    {
        if (!_initialized || !IsInsideTree()) return;
        
        float delta = (float)GetProcessDeltaTime();
        
        UpdateParticles();
        
        if (_activated)
        {
            _particleTimer -= delta;
            if (_particleTimer < 0f)
            {
                _particleTimer = ParticleEmitInterval;
                SpawnParticles();
            }
        }
    }
    
    private void UpdateParticles()
    {
        // Small particles
        for (int i = _particlesS.Count - 1; i >= 0; i--)
        {
            var particle = _particlesS[i];
            if (particle.IsDone || !IsInstanceValid(particle))
            {
                if (IsInstanceValid(particle))
                    particle.QueueFree();
                _particlesS.RemoveAt(i);
            }
        }
    
        for (int i = _flaresS.Count - 1; i >= 0; i--)
        {
            var flare = _flaresS[i];
            if (flare.IsDone || !IsInstanceValid(flare))
            {
                if (IsInstanceValid(flare))
                    flare.QueueFree();
                _flaresS.RemoveAt(i);
            }
        }
    
        // Medium particles
        for (int i = _particlesM.Count - 1; i >= 0; i--)
        {
            var particle = _particlesM[i];
            if (particle.IsDone || !IsInstanceValid(particle))
            {
                if (IsInstanceValid(particle))
                    particle.QueueFree();
                _particlesM.RemoveAt(i);
            }
        }
    
        for (int i = _flaresM.Count - 1; i >= 0; i--)
        {
            var flare = _flaresM[i];
            if (flare.IsDone || !IsInstanceValid(flare))
            {
                if (IsInstanceValid(flare))
                    flare.QueueFree();
                _flaresM.RemoveAt(i);
            }
        }
    
        // Large particles
        for (int i = _particlesL.Count - 1; i >= 0; i--)
        {
            var particle = _particlesL[i];
            if (particle.IsDone || !IsInstanceValid(particle))
            {
                if (IsInstanceValid(particle))
                    particle.QueueFree();
                _particlesL.RemoveAt(i);
            }
        }
    
        for (int i = _flaresL.Count - 1; i >= 0; i--)
        {
            var flare = _flaresL[i];
            if (flare.IsDone || !IsInstanceValid(flare))
            {
                if (IsInstanceValid(flare))
                    flare.QueueFree();
                _flaresL.RemoveAt(i);
            }
        }
    }
    
    private void SpawnParticles()
    {
        float particleX = _x;
        float particleY = _y;
    
        switch (_size)
        {
            case TorchSize.S:
                particleY -= 10f;
                var particleS = TorchParticleSEffect.Create(particleX, particleY, RenderGreen);
                particleS.ZIndex = -1;
                GetParent().AddChild(particleS);
                _particlesS.Add(particleS);
            
                var flareS = LightFlareSEffect.Create(particleX, particleY, RenderGreen);
                flareS.ZIndex = -1;
                GetParent().AddChild(flareS);
                _flaresS.Add(flareS);
                break;
            
            case TorchSize.M:
                var particleM = TorchParticleMEffect.Create(particleX, particleY, RenderGreen);
                particleM.ZIndex = -1;
                GetParent().AddChild(particleM);
                _particlesM.Add(particleM);
            
                var flareM = LightFlareMEffect.Create(particleX, particleY, RenderGreen);
                flareM.ZIndex = -1;
                GetParent().AddChild(flareM);
                _flaresM.Add(flareM);
                break;
            
            case TorchSize.L:
                particleY += 14f;
                var particleL = TorchParticleLEffect.Create(particleX, particleY, RenderGreen);
                particleL.ZIndex = -1;
                GetParent().AddChild(particleL);
                _particlesL.Add(particleL);
            
                var flareL = LightFlareLEffect.Create(particleX, particleY, RenderGreen);
                flareL.ZIndex = -1;
                GetParent().AddChild(flareL);
                _flaresL.Add(flareL);
                break;
        }
    }
}