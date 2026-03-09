using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class BronzeOrbSpawnAnimationAlternate
{
    // a more simple spawn animation
public static async Task Play(Creature creature)
{
    var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
    if (creatureNode == null)
        return;

    var visuals = creatureNode.Visuals;
    if (visuals == null)
        return;

    var sprite = visuals.GetNodeOrNull<Sprite2D>("Visuals");
    if (sprite == null)
        return;

    // Immediately hide until shader takes over
    sprite.Visible = false;

    var shader = new Shader();
    shader.Code = @"
shader_type canvas_item;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float hologram_strength : hint_range(0.0, 1.0) = 1.0;
uniform vec3 hologram_color : source_color = vec3(0.3, 0.5, 1.0);
uniform vec3 scanline_color : source_color = vec3(0.2, 1.0, 0.4);
uniform float scanline_width : hint_range(0.0, 0.2) = 0.06;

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    float y = 1.0 - UV.y;

    if (y > progress) {
        COLOR.a = 0.0;
    } else {
        vec3 tinted = mix(tex.rgb, hologram_color, hologram_strength * 0.6);
        COLOR.rgb = tinted;
        COLOR.a = tex.a;

        float dist = abs(y - progress);
        if (dist < scanline_width) {
            float glow = 1.0 - (dist / scanline_width);
            COLOR.rgb = mix(COLOR.rgb, scanline_color, glow * 0.8);
        }
    }
}
";

    var material = new ShaderMaterial { Shader = shader };
    material.SetShaderParameter("progress", 0.0f);
    material.SetShaderParameter("hologram_strength", 1.0f);
    sprite.Material = material;
    sprite.Visible = true;

    // Phase 1: Scan line bottom to top
    var tween = creatureNode.CreateTween();
    tween.TweenMethod(
        Callable.From<float>(p => material.SetShaderParameter("progress", p)),
        0.0f,
        1.0f,
        1.5f
    ).SetTrans(Tween.TransitionType.Linear);

    await Cmd.Wait(1.5f);

    // Phase 2: Fade hologram tint
    var fadeTween = creatureNode.CreateTween();
    fadeTween.TweenMethod(
        Callable.From<float>(h => material.SetShaderParameter("hologram_strength", h)),
        1.0f,
        0.0f,
        0.5f
    ).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);

    await Cmd.Wait(0.5f);

    sprite.Material = null;
}
}