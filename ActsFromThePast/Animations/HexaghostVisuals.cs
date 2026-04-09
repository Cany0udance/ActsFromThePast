using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ActsFromThePast;
public class HexaghostVisuals : IDisposable
{
    private readonly Creature _creature;
    private readonly NCreature _creatureNode;
    
    // Plasma layers
    private Sprite2D _plasma1;
    private Sprite2D _plasma2;
    private Sprite2D _plasma3;
    private Sprite2D _shadow;
    
    // Rotation
    private float _rotationSpeed = 1.0f;
    private float _targetRotationSpeed = 30.0f;
    private float _plasma1Angle;
    private float _plasma2Angle;
    private float _plasma3Angle;
    
    // Bob effect
    private float _bobTimer;
    private float _bobOffset;
    private const float BobSpeed = 0.75f;
    private const float BobAmount = 5.0f;
    
    // Orbs
    private readonly HexaghostOrbVisual[] _orbs = new HexaghostOrbVisual[6];
    
    // Orb positions from StS1 (relative to center)
    private static readonly Vector2[] OrbPositions = new[]
    {
        new Vector2(-90f, -370f),
        new Vector2(90f, -370f),
        new Vector2(160f, -240f),
        new Vector2(90f, -110f),
        new Vector2(-90f, -110f),
        new Vector2(-160f, -240f)
    };
    
    private const float BodyOffsetY = -225f; // Match the core's Y position in the scene
    
    public HexaghostVisuals(Creature creature, NCreature creatureNode)
    {
        _creature = creature;
        _creatureNode = creatureNode;
        
        CreatePlasmaLayers();
        CreateOrbs();
        
        // Start update loop
        TaskHelper.RunSafely(UpdateLoop());
    }
    
    private void CreatePlasmaLayers()
    {
        var visualsNode = _creatureNode.Visuals;
    
        // Create plasma layers (rendered behind the core)
        _plasma3 = new Sprite2D();
        _plasma3.Texture = PreloadManager.Cache.GetTexture2D("res://ActsFromThePast/monsters/hexaghost/plasma3.png");
        _plasma3.ZIndex = -3;
        visualsNode.AddChild(_plasma3);
    
        _plasma2 = new Sprite2D();
        _plasma2.Texture = PreloadManager.Cache.GetTexture2D("res://ActsFromThePast/monsters/hexaghost/plasma2.png");
        _plasma2.ZIndex = -2;
        visualsNode.AddChild(_plasma2);
    
        _plasma1 = new Sprite2D();
        _plasma1.Texture = PreloadManager.Cache.GetTexture2D("res://ActsFromThePast/monsters/hexaghost/plasma1.png");
        _plasma1.ZIndex = -1;
        visualsNode.AddChild(_plasma1);
    
        _shadow = new Sprite2D();
        _shadow.Texture = PreloadManager.Cache.GetTexture2D("res://ActsFromThePast/monsters/hexaghost/shadow.png");
        _shadow.ZIndex = -4;
        visualsNode.AddChild(_shadow);
    }
    
    private void CreateOrbs()
    {
        for (int i = 0; i < 6; i++)
        {
            var orb = new HexaghostOrbVisual(i, OrbPositions[i]);
            orb.SetParentNode(_creatureNode.Visuals);
            _orbs[i] = orb;
        }
    }
    
    private async Task UpdateLoop()
    {
        while (GodotObject.IsInstanceValid(_creatureNode) && _creature.IsAlive)
        {
            float delta = (float)_creatureNode.GetProcessDeltaTime();
            Update(delta);
            await _creatureNode.ToSignal(_creatureNode.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }
    
    private void Update(float delta)
    {
        // Update rotation speed (lerp towards target)
        _rotationSpeed = Mathf.Lerp(_rotationSpeed, _targetRotationSpeed, delta * 5f);
    
        // Update plasma rotation angles
        _plasma1Angle -= _rotationSpeed * delta;
        _plasma2Angle -= _rotationSpeed / 2f * delta;
        _plasma3Angle -= _rotationSpeed / 3f * delta;
    
        // Update bob effect
        _bobTimer += BobSpeed * delta;
        _bobOffset = Mathf.Sin(_bobTimer) * BobAmount;
    
        // Apply transforms to plasma layers
        // In StS1, positive Y offset moves up. In Godot, negative Y moves up.
        _plasma1.Rotation = Mathf.DegToRad(_plasma1Angle);
        _plasma1.Position = new Vector2(0, -_bobOffset * 0.5f + BodyOffsetY);
    
        _plasma2.Rotation = Mathf.DegToRad(_plasma2Angle);
        _plasma2.Position = new Vector2(6f, -_bobOffset + BodyOffsetY);
    
        _plasma3.Rotation = Mathf.DegToRad(_plasma3Angle);
        _plasma3.Scale = Vector2.One * 0.95f;
        _plasma3.Position = new Vector2(12f, -_bobOffset * 2f + BodyOffsetY);
    
        _shadow.Position = new Vector2(12f, -_bobOffset / 4f - 15f + BodyOffsetY);
    
        // Update orbs - pass the parent's global position
        var parentGlobalPos = _creatureNode.Visuals.GlobalPosition;
        foreach (var orb in _orbs)
        {
            orb.Update(delta, parentGlobalPos);
        }
    }
    
    public void SetTargetRotationSpeed(float speed)
    {
        _targetRotationSpeed = speed;
    }
    
    public void ActivateAllOrbs()
    {
        for (int i = 0; i < 6; i++)
        {
            _orbs[i].Activate();
        }
    }
    
    public void ActivateNextOrb()
    {
        for (int i = 0; i < 6; i++)
        {
            if (!_orbs[i].IsActivated)
            {
                _orbs[i].Activate(immediate: true);
                return;
            }
        }
    }
    
    public void DeactivateAllOrbs()
    {
        foreach (var orb in _orbs)
        {
            orb.Deactivate();
        }
    }
    
    public void HideAllOrbs()
    {
        foreach (var orb in _orbs)
        {
            orb.Hide();
        }
    }
    
    public void Dispose()
    {
        SafeFree(_plasma1);
        SafeFree(_plasma2);
        SafeFree(_plasma3);
        SafeFree(_shadow);

        foreach (var orb in _orbs)
        {
            orb?.Dispose();
        }
    }

    private static void SafeFree(Node node)
    {
        if (node != null && GodotObject.IsInstanceValid(node))
            node.QueueFree();
    }
}