using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Tangent: (1) a huge hit if it's the first thing he says, (2) nothing, (3) everything.</summary>
public sealed class BigDeal : TangentCard
{
	public override int LineCount => 3;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(22m, ValueProp.Move),
		new DamageVar("AllDamage", 14m, ValueProp.Move)
	};

	public BigDeal()
		: base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
	{
	}

	protected override async Task PlayLine(int line, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		switch (line)
		{
			case 1:
				ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
				await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
					.WithHitFx("vfx/vfx_heavy_blunt", null, "heavy_attack.mp3")
					.Execute(choiceContext);
				break;
			case 3:
				await DamageCmd.Attack(DynamicVars["AllDamage"].BaseValue).FromCard(this).TargetingAllOpponents(CombatState!)
					.WithHitFx("vfx/vfx_giant_horizontal_slash")
					.Execute(choiceContext);
				break;
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(6m);
		DynamicVars["AllDamage"].UpgradeValueBy(4m);
	}
}
