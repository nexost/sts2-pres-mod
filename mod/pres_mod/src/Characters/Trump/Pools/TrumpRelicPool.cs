using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using PresMod.Characters.Trump.Relics;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Pools;

public sealed class TrumpRelicPool : RelicPoolModel
{
	public override string EnergyColorName => Trump.energyColorName;

	public override Color LabOutlineColor => new Color("F2B92E");

	protected override IEnumerable<RelicModel> GenerateAllRelics()
	{
		// Diamond Shovel sits here like Burning Blood does: Starter rarity, never rolled as a reward.
		return new RelicModel[]
		{
			ModelDb.Relic<GoldenShovel>(),
			ModelDb.Relic<DiamondShovel>(),
			ModelDb.Relic<HardHat>(),
			ModelDb.Relic<LongRedTie>(),
			ModelDb.Relic<GoldPlatedBricks>(),
			ModelDb.Relic<RubberStamp>(),
			ModelDb.Relic<PermanentStructure>(),
			ModelDb.Relic<LateNightPhone>(),
			ModelDb.Relic<GoldPlatedToilet>()
		};
	}
}
