using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>1 Energy and Doze 4. Exhausts until upgraded.</summary>
public sealed class FortyWinks : SleepyCard
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new EnergyVar(1), new DynamicVar(CardVars.Doze, 4m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.ForEnergy(this), BidenHoverTips.Doze, BidenHoverTips.Drowsy };

	public FortyWinks()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
		await Doze(choiceContext);
	}

	protected override void OnUpgrade()
	{
		RemoveKeyword(CardKeyword.Exhaust);
	}
}
