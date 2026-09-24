using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Tangent: (1) a solid single hit, (2) a small hit on everything.</summary>
public sealed class LongStoryShort : TangentCard
{
	public override int LineCount => 2;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(9m, ValueProp.Move),
		new DamageVar("AllDamage", 4m, ValueProp.Move)
	};

	public LongStoryShort()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override async Task PlayLine(int line, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (line == 1)
		{
			ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
			await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
				.WithHitFx("vfx/vfx_attack_slash")
				.Execute(choiceContext);
			return;
		}
		await DamageCmd.Attack(DynamicVars["AllDamage"].BaseValue).FromCard(this).TargetingAllOpponents(CombatState!)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
		DynamicVars["AllDamage"].UpgradeValueBy(2m);
	}
}
