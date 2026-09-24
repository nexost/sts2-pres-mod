using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Draw first, then Doze (so a nap doesn't cost the draw).</summary>
public sealed class CountingSheep : SleepyCard
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(2), new DynamicVar(CardVars.Doze, 2m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Doze, BidenHoverTips.Drowsy };

	public CountingSheep()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
		await Doze(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(1m);
	}
}
