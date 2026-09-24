using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Every Tangent in hand jumps to a random line; then draw.</summary>
public sealed class BrainFreeze : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(1) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Tangent };

	public BrainFreeze()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		foreach (TangentCard tangent in PileType.Hand.GetPile(Owner).Cards.OfType<TangentCard>().ToList())
		{
			tangent.SetLine(Owner.RunState.Rng.CombatCardSelection.NextInt(1, tangent.LineCount + 1));
		}
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(1m);
	}
}
