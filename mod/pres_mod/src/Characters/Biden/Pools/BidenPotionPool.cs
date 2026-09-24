using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using PresMod.Characters.Biden.Potions;

namespace PresMod.Characters.Biden.Pools;

/// <summary>Sleepy Joe's 3 potions (design.md §7).</summary>
public sealed class BidenPotionPool : PotionPoolModel
{
	public override string EnergyColorName => Biden.energyColorName;

	public override Color LabOutlineColor => new Color("4A55E6");

	protected override IEnumerable<PotionModel> GenerateAllPotions()
	{
		return new PotionModel[]
		{
			ModelDb.Potion<WarmMilk>(),
			ModelDb.Potion<EspressoShot>(),
			ModelDb.Potion<DarkRoast>()
		};
	}
}
