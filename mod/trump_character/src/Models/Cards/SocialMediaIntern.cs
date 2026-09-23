using TrumpMod.Models.Powers;

namespace TrumpMod.Models.Cards;

/// <summary>Infinite Blades with Tweets.</summary>
public sealed class SocialMediaIntern : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<SocialMediaInternPower>(1m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Tweet };

	public SocialMediaIntern()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<SocialMediaInternPower>(choiceContext, Owner.Creature, DynamicVars[nameof(SocialMediaInternPower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Innate);
	}
}
