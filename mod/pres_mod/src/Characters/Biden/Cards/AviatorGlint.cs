using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Damage and Weak on one enemy; as Dark Brandon the glare hits them all.</summary>
public sealed class AviatorGlint : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(7m, ValueProp.Move), new PowerVar<WeakPower>(1m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<WeakPower>(), BidenHoverTips.DarkBrandon };

	protected override bool ShouldGlowGoldInternal => this.IsAwake();

	public AviatorGlint()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		decimal weak = DynamicVars[nameof(WeakPower)].BaseValue;
		if (this.IsAwake())
		{
			await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState!)
				.WithHitFx("vfx/vfx_attack_slash")
				.Execute(choiceContext);
			await PowerCmd.Apply<WeakPower>(choiceContext, CombatState!.HittableEnemies, weak, Owner.Creature, this);
			return;
		}
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
		await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, weak, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(2m);
		DynamicVars[nameof(WeakPower)].UpgradeValueBy(1m);
	}
}
