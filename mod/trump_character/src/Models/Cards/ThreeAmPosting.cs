using TrumpMod.Models.Powers;

namespace TrumpMod.Models.Cards;

public sealed class ThreeAmPosting : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<ThreeAmPostingPower>(2m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Tweet };

	public ThreeAmPosting()
		: base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<ThreeAmPostingPower>(choiceContext, Owner.Creature, DynamicVars[nameof(ThreeAmPostingPower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(ThreeAmPostingPower)].UpgradeValueBy(1m);
	}
}
