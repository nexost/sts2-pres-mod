using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Everything twice; three times as Dark Brandon.</summary>
public sealed class AirForceOne : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(6m, ValueProp.Move) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.DarkBrandon };

	protected override bool ShouldGlowGoldInternal => this.IsAwake();

	public AirForceOne()
		: base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await BidenVfx.AirForceOne(Owner.Creature);
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(this.IsAwake() ? 3 : 2).FromCard(this).TargetingAllOpponents(CombatState!)
			.WithHitFx("vfx/vfx_giant_horizontal_slash")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(2m);
	}
}
