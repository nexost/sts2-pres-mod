using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>8 plus 2 per Drowsy: best just under the line.</summary>
public sealed class SleepingGiant : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new CalculationBaseVar(8m),
		new ExtraDamageVar(2m),
		new CalculatedDamageVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => DrowsyCmd.GetDrowsy(card.Owner.Creature))
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Drowsy };

	public SleepingGiant()
		: base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this).Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_heavy_blunt", null, "heavy_attack.mp3")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.CalculationBase.UpgradeValueBy(2m);
	}
}
