using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;

using PresMod.Characters.Trump.Potions;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Pools;

/// <summary>Empty until the character potions are designed (the base game also returns empty pools while locked).</summary>
public sealed class TrumpPotionPool : PotionPoolModel
{
	public override string EnergyColorName => Trump.energyColorName;

	public override Color LabOutlineColor => new Color("F2B92E");

	protected override IEnumerable<PotionModel> GenerateAllPotions()
	{
		return new PotionModel[]
		{
			ModelDb.Potion<QuickDryConcrete>(),
			ModelDb.Potion<CovfefePotion>(),
			ModelDb.Potion<DeportationDraught>()
		};
	}
}
