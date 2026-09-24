using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using PresMod.Characters.{{Class}}.Relics;

namespace PresMod.Characters.{{Class}}.Pools;

public sealed class {{Class}}RelicPool : RelicPoolModel
{
	public override string EnergyColorName => {{Class}}.energyColorName;

	public override Color LabOutlineColor => new Color("{{primary}}");

	protected override IEnumerable<RelicModel> GenerateAllRelics()
	{
		// The starter and its upgrade sit here like Burning Blood does: Starter rarity, never rolled as a reward.
		return new RelicModel[]
		{
			ModelDb.Relic<{{Starter}}>()
		};
	}
}
