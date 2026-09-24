using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using PresMod.Characters.Biden.Relics;

namespace PresMod.Characters.Biden.Pools;

public sealed class BidenRelicPool : RelicPoolModel
{
	public override string EnergyColorName => Biden.energyColorName;

	public override Color LabOutlineColor => new Color("4A55E6");

	protected override IEnumerable<RelicModel> GenerateAllRelics()
	{
		// The starter and its upgrade sit here like Burning Blood does: Starter rarity, never rolled as a reward.
		return new RelicModel[]
		{
			ModelDb.Relic<AviatorShades>(),
			ModelDb.Relic<DarkAviators>()
		};
	}
}
