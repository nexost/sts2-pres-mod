using PresMod.Characters.Trump.Powers;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Cards;

/// <summary>Accuracy for Tweets.</summary>
public sealed class VerifiedAccount : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<VerifiedAccountPower>(3m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Tweet };

	public VerifiedAccount()
		: base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<VerifiedAccountPower>(choiceContext, Owner.Creature, DynamicVars[nameof(VerifiedAccountPower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(VerifiedAccountPower)].UpgradeValueBy(2m);
	}
}
