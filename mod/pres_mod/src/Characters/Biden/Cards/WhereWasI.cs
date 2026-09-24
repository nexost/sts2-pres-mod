using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Draw, then every Tangent in hand moves to its next line (the new ones too).</summary>
public sealed class WhereWasI : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(1) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Tangent };

	public WhereWasI()
		: base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
		foreach (TangentCard tangent in PileType.Hand.GetPile(Owner).Cards.OfType<TangentCard>().ToList())
		{
			await tangent.MoveToNextLine(choiceContext);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(1m);
	}
}
