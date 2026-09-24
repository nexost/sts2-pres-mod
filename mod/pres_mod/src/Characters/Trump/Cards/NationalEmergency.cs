using MegaCrit.Sts2.Core.MonsterMoves.Intents;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Cards;

/// <summary>Build equal to the total damage enemies intend to deal this turn (read from their attack intents).</summary>
public sealed class NationalEmergency : CardModel
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Build, TrumpHoverTips.Wall };

	protected override bool ShouldGlowGoldInternal => IsMutable && CombatState != null && IncomingDamage(Owner.Creature) > 0;

	public NationalEmergency()
		: base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await WallCmd.Build(choiceContext, Owner.Creature, IncomingDamage(Owner.Creature), this);
	}

	protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
	}

	public static int IncomingDamage(Creature player)
	{
		ICombatState? combat = player.CombatState;
		if (combat == null)
		{
			return 0;
		}
		var targets = new[] { player };
		return combat.HittableEnemies.Sum(enemy =>
			enemy.Monster?.NextMove?.Intents?.OfType<AttackIntent>().Sum(intent => intent.GetTotalDamage(targets, enemy)) ?? 0);
	}
}
