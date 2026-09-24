using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Dexterity whenever you nod off.</summary>
public sealed class WellRested : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<WellRestedPower>(1m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.NodOff, HoverTipFactory.FromPower<DexterityPower>() };

	public WellRested()
		: base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<WellRestedPower>(choiceContext, Owner.Creature, DynamicVars[nameof(WellRestedPower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Innate);
	}
}
