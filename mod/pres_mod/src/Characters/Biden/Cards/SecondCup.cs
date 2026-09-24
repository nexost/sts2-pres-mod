using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Awake: Energy. Asleep: Wake Up.</summary>
public sealed class SecondCup : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new EnergyVar(2) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.DarkBrandon, BidenHoverTips.WakeUp, HoverTipFactory.ForEnergy(this) };

	protected override bool ShouldGlowGoldInternal => this.IsAwake();

	public SecondCup()
		: base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (this.IsAwake())
		{
			await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
			return;
		}
		await DrowsyCmd.WakeUp(choiceContext, Owner.Creature);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Energy.UpgradeValueBy(1m);
	}
}
