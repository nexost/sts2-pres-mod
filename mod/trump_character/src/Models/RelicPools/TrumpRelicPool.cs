using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using TrumpMod.Models.Relics;

namespace TrumpMod.Models.RelicPools;

public sealed class TrumpRelicPool : RelicPoolModel
{
	public override string EnergyColorName => Characters.Trump.energyColorName;

	public override Color LabOutlineColor => new Color("F2B92E");

	protected override IEnumerable<RelicModel> GenerateAllRelics()
	{
		return new RelicModel[] { ModelDb.Relic<PlaceholderRelicTrump>() };
	}
}
