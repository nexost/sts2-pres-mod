using MegaCrit.Sts2.Core.Models.Powers;

namespace TrumpMod.Models.Cards;

/// <summary>Hits twice if the enemy is Weak: pairs with Sad! and Fake News.</summary>
public sealed class WitchHunt : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(8m, ValueProp.Move) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<WeakPower>() };

	protected override bool ShouldGlowGoldInternal => IsMutable && CombatState != null
		&& CombatState.HittableEnemies.Any(e => e.GetPowerAmount<WeakPower>() > 0);

	public WitchHunt()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		int hits = cardPlay.Target.GetPowerAmount<WeakPower>() > 0 ? 2 : 1;
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(hits).FromCard(this).Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(2m);
	}
}
