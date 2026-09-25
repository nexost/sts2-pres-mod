using System.Collections.Generic;
using Godot;

namespace PresMod.Framework;

/// <summary>
/// Recolours one instance of a game VFX scene (the Defect's hyperbeam into Sleepy Joe's red Laser Eyes, say) without
/// touching the game's own copies. Tinting the whole node multiplies every colour, so a cyan effect under a red tint
/// turns nearly black; this instead moves the hue of every coloured value and leaves white and grey alone, so hot
/// white cores stay white inside the new colour's glow.
///
/// Covered: node tints (Modulate, SelfModulate), Line2D colours, particle colours and colour ramps, and every Color or
/// gradient-texture parameter of shader materials (the game's flipbook and laser shaders colour through a gradient
/// "lut"). Materials, gradients and their textures are copied before they change: scene instances share them.
/// </summary>
public static class VfxRecolor
{
	/// <summary>Recolour a node and everything under it to <paramref name="hue"/> (0..1), pushing saturation up by <paramref name="saturationBoost"/>.</summary>
	public static void Apply(Node root, float hue, float saturationBoost = 1.5f)
	{
		var done = new Dictionary<ulong, Resource>();
		Visit(root, hue, saturationBoost, done);
	}

	/// <summary>A colour with its hue moved; greys and whites (low saturation) are kept as they are. Works on HDR values above 1.</summary>
	public static Color Map(Color c, float hue, float saturationBoost)
	{
		float max = Mathf.Max(c.R, Mathf.Max(c.G, c.B));
		float min = Mathf.Min(c.R, Mathf.Min(c.G, c.B));
		if (max <= 0f || (max - min) / max < 0.12f)
		{
			return c;
		}
		float s = Mathf.Min(1f, (max - min) / max * saturationBoost);
		// Color.FromHsv clamps nothing, so HDR brightness (value above 1) survives.
		return Color.FromHsv(hue, s, max, c.A);
	}

	private static void Visit(Node node, float hue, float boost, Dictionary<ulong, Resource> done)
	{
		if (node is CanvasItem item)
		{
			item.Modulate = Map(item.Modulate, hue, boost);
			item.SelfModulate = Map(item.SelfModulate, hue, boost);
			if (item.Material != null)
			{
				item.Material = (Material)Copy(item.Material, hue, boost, done);
			}
		}
		switch (node)
		{
			case GpuParticles2D gpu when gpu.ProcessMaterial != null:
				gpu.ProcessMaterial = (Material)Copy(gpu.ProcessMaterial, hue, boost, done);
				break;
			case CpuParticles2D cpu:
				cpu.Color = Map(cpu.Color, hue, boost);
				if (cpu.ColorRamp != null)
				{
					cpu.ColorRamp = (Gradient)Copy(cpu.ColorRamp, hue, boost, done);
				}
				if (cpu.ColorInitialRamp != null)
				{
					cpu.ColorInitialRamp = (Gradient)Copy(cpu.ColorInitialRamp, hue, boost, done);
				}
				break;
			case Line2D line:
				line.DefaultColor = Map(line.DefaultColor, hue, boost);
				if (line.Gradient != null)
				{
					line.Gradient = (Gradient)Copy(line.Gradient, hue, boost, done);
				}
				break;
		}
		foreach (Node child in node.GetChildren())
		{
			Visit(child, hue, boost, done);
		}
	}

	/// <summary>A recoloured copy of a resource (each original copied once per effect, then reused).</summary>
	private static Resource Copy(Resource original, float hue, float boost, Dictionary<ulong, Resource> done)
	{
		ulong id = original.GetInstanceId();
		if (done.TryGetValue(id, out Resource? copy))
		{
			return copy;
		}
		copy = (Resource)original.Duplicate();
		done[id] = copy;
		switch (copy)
		{
			case Gradient gradient:
				Color[] colors = gradient.Colors;
				for (int i = 0; i < colors.Length; i++)
				{
					colors[i] = Map(colors[i], hue, boost);
				}
				gradient.Colors = colors;
				break;
			case GradientTexture1D tex1 when tex1.Gradient != null:
				tex1.Gradient = (Gradient)Copy(tex1.Gradient, hue, boost, done);
				break;
			case GradientTexture2D tex2 when tex2.Gradient != null:
				tex2.Gradient = (Gradient)Copy(tex2.Gradient, hue, boost, done);
				break;
			case ParticleProcessMaterial particles:
				particles.Color = Map(particles.Color, hue, boost);
				if (particles.ColorRamp != null)
				{
					particles.ColorRamp = (Texture2D)Copy(particles.ColorRamp, hue, boost, done);
				}
				if (particles.ColorInitialRamp != null)
				{
					particles.ColorInitialRamp = (Texture2D)Copy(particles.ColorInitialRamp, hue, boost, done);
				}
				break;
			case ShaderMaterial shader when shader.Shader != null:
				foreach (Godot.Collections.Dictionary uniform in shader.Shader.GetShaderUniformList())
				{
					StringName name = uniform["name"].AsStringName();
					Variant value = shader.GetShaderParameter(name);
					if (value.VariantType == Variant.Type.Color)
					{
						shader.SetShaderParameter(name, Map(value.AsColor(), hue, boost));
					}
					else if (value.VariantType == Variant.Type.Object && value.AsGodotObject() is GradientTexture1D or GradientTexture2D)
					{
						shader.SetShaderParameter(name, Copy((Resource)value.AsGodotObject(), hue, boost, done));
					}
				}
				break;
		}
		return copy;
	}
}
