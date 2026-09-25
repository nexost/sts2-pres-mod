using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Tangent: (1) one hit, (2) two hits, (3) everything three times.</summary>
public sealed class OffTheCuff : TangentCard
{
	public override int LineCount => 3;

	protected override TargetType[] LineTargets => new[] { TargetType.AnyEnemy, TargetType.AnyEnemy, TargetType.AllEnemies };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(10m, ValueProp.Move),
		new DamageVar("TwiceDamage", 5m, ValueProp.Move),
		new DamageVar("AllDamage", 3m, ValueProp.Move)
	};

	public OffTheCuff()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task PlayLine(int line, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		switch (line)
		{
			case 1:
				if (EnemyFor(cardPlay) is not Creature enemy)
				{
					break;
				}
				await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(enemy)
					.WithHitFx("vfx/vfx_attack_blunt")
					.Execute(choiceContext);
				break;
			case 2:
				if (EnemyFor(cardPlay) is not Creature enemy2)
				{
					break;
				}
				await DamageCmd.Attack(DynamicVars["TwiceDamage"].BaseValue).WithHitCount(2).FromCard(this).Targeting(enemy2)
					.WithHitFx("vfx/vfx_attack_slash")
					.Execute(choiceContext);
				break;
			case 3:
				await DamageCmd.Attack(DynamicVars["AllDamage"].BaseValue).WithHitCount(3).FromCard(this).TargetingAllOpponents(CombatState!)
					.WithHitFx("vfx/vfx_attack_slash")
					.Execute(choiceContext);
				break;
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
		DynamicVars["TwiceDamage"].UpgradeValueBy(2m);
		DynamicVars["AllDamage"].UpgradeValueBy(1m);
	}
}
