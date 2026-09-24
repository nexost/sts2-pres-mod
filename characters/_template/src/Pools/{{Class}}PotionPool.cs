using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace PresMod.Characters.{{Class}}.Pools;

/// <summary>Empty until the character potions are designed (the base game also returns empty pools while locked).</summary>
public sealed class {{Class}}PotionPool : PotionPoolModel
{
	public override string EnergyColorName => {{Class}}.energyColorName;

	public override Color LabOutlineColor => new Color("{{primary}}");

	protected override IEnumerable<PotionModel> GenerateAllPotions()
	{
		return new PotionModel[]
		{
		};
	}
}
