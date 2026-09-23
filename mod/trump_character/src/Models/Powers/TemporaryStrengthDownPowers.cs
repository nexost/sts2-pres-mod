using MegaCrit.Sts2.Core.Models.Powers;
using TrumpMod.Models.Cards;

namespace TrumpMod.Models.Powers;

/// <summary>Sharpie: the target loses Strength this turn (same machinery as Dark Shackles; the title comes from the card).</summary>
public sealed class SharpiePower : TemporaryStrengthPower
{
	public override AbstractModel OriginModel => ModelDb.Card<Sharpie>();

	protected override bool IsPositive => false;
}

/// <summary>Low Energy: every enemy loses Strength this turn (Piercing Wail's machinery).</summary>
public sealed class LowEnergyPower : TemporaryStrengthPower
{
	public override AbstractModel OriginModel => ModelDb.Card<LowEnergy>();

	protected override bool IsPositive => false;
}
