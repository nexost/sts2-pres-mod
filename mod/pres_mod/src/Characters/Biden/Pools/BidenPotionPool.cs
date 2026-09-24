using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace PresMod.Characters.Biden.Pools;

/// <summary>Empty until the character potions are designed (the base game also returns empty pools while locked).</summary>
public sealed class BidenPotionPool : PotionPoolModel
{
	public override string EnergyColorName => Biden.energyColorName;

	public override Color LabOutlineColor => new Color("4A55E6");

	protected override IEnumerable<PotionModel> GenerateAllPotions()
	{
		return new PotionModel[]
		{
		};
	}
}
