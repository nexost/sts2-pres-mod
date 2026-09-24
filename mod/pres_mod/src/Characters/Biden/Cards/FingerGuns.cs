using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Twin Strike that doubles its hits as Dark Brandon (multi-hit, so Strength counts twice).</summary>
public sealed class FingerGuns : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(4m, ValueProp.Move) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.DarkBrandon };

	protected override bool ShouldGlowGoldInternal => this.IsAwake();

	public FingerGuns()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(this.IsAwake() ? 4 : 2).FromCard(this).Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_blunt")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(1m);
	}
}
