using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class BronzeOrbSpawnAnimation
{
public static async Task Play(Creature creature)
{
    var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
    if (creatureNode == null)
        return;
    
    SfxCmd.Play("event:/sfx/enemy/enemy_attacks/obscura/obscura_buff");

    var visuals = creatureNode.Visuals;
    if (visuals == null)
        return;

    var sprite = visuals.GetNodeOrNull<Sprite2D>("Visuals");
    if (sprite == null)
        return;
    

    var shader = new Shader();
shader.Code = @"
shader_type canvas_item;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float hologram_strength : hint_range(0.0, 1.0) = 1.0;
uniform vec3 hologram_color : source_color = vec3(0.2, 0.45, 1.0);
uniform vec3 scanline_color : source_color = vec3(0.0, 1.0, 0.15);
uniform float scanline_width : hint_range(0.0, 0.2) = 0.06;
uniform float scanline_extend : hint_range(0.0, 0.5) = 0.15;

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    float y = 1.0 - UV.y;

    // Holographic horizontal scanlines
    float scan = sin(UV.y * 120.0 + TIME * 3.0) * 0.5 + 0.5;
    float scan_fine = sin(UV.y * 300.0 - TIME * 8.0) * 0.5 + 0.5;

    // Subtle holographic flicker
    float flicker = sin(TIME * 12.0) * 0.03 + 0.97;

    if (y > progress) {
        COLOR.a = 0.0;
    } else {
        // Base hologram tint
        vec3 tinted = mix(tex.rgb, hologram_color, hologram_strength * 0.65);

        // Add scanline interference pattern when holographic
        tinted += hologram_strength * vec3(0.05, 0.1, 0.2) * scan * 0.4;
        tinted += hologram_strength * vec3(0.02, 0.05, 0.1) * scan_fine * 0.3;

        // Flicker
        tinted *= mix(1.0, flicker, hologram_strength);

        // Slight edge glow — boost alpha near texture edges
        float edge = smoothstep(0.0, 0.15, tex.a) * smoothstep(1.0, 0.85, tex.a);
        tinted += hologram_strength * hologram_color * (1.0 - edge) * 0.3;

        COLOR.rgb = tinted;
        COLOR.a = tex.a;

// Green scanline at the progress boundary
    float dist = abs(y - progress);
    float top_fade = smoothstep(1.0, 0.85, progress);
    if (dist < scanline_width) {
        float glow = 1.0 - (dist / scanline_width);
        glow = glow * glow;
        glow *= top_fade;
        COLOR.rgb = mix(COLOR.rgb, scanline_color, glow * 0.9);
        COLOR.a = max(COLOR.a, glow * 0.7);
    }
    }

// Extend green scanline beyond texture horizontally
    float dist_to_line = abs((1.0 - UV.y) - progress);
    float top_fade_ext = smoothstep(1.0, 0.85, progress);
    if (dist_to_line < scanline_width && tex.a < 0.01) {
        float center_dist = abs(UV.x - 0.5);
        if (center_dist < 0.5 + scanline_extend) {
            float h_falloff = smoothstep(0.5 + scanline_extend, 0.35, center_dist);
            float v_glow = 1.0 - (dist_to_line / scanline_width);
            v_glow = v_glow * v_glow;
            v_glow *= top_fade_ext;
            COLOR.rgb = scanline_color;
            COLOR.a = v_glow * h_falloff * 0.6;
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