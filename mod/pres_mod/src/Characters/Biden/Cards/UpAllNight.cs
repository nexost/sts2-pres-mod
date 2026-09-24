using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Energy, but only while he's Dark Brandon.</summary>
public sealed class UpAllNight : CardModel
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new EnergyVar(2) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.DarkBrandon, HoverTipFactory.ForEnergy(this) };

	protected override bool IsPlayable => IsCanonical || CombatState == null || this.IsAwake();

	protected override bool ShouldGlowGoldInternal => this.IsAwake();

	public UpAllNight()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Energy.UpgradeValueBy(1m);
	}
}
