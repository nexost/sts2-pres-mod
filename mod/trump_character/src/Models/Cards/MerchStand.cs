using TrumpMod.Models.Powers;

namespace TrumpMod.Models.Cards;

/// <summary>Half of the Regent's Rare Royalties (30).</summary>
public sealed class MerchStand : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<MerchStandPower>(15m) };

	public MerchStand()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<MerchStandPower>(choiceContext, Owner.Creature, DynamicVars[nameof(MerchStandPower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(MerchStandPower)].UpgradeValueBy(7m);
	}
}
