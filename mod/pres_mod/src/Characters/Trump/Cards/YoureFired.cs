using PresMod.Framework.Patches;
using PresMod.Characters.Trump.Patches;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Cards;

/// <summary>
/// Pay Gold to skip the rest of a non-boss fight's worth of one enemy. Immune bosses can't be targeted (TargetFilterPatch),
/// so the Gold is never wasted on them. Still no Gold from that enemy.
/// </summary>
public sealed class YoureFired : PayGoldCard, ITargetFilter
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar(GoldCostVar, 99m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.PayGold, TrumpHoverTips.Deport };

	protected override bool IsPlayable => base.IsPlayable && (IsCanonical || CombatState == null || CombatState.HittableEnemies.Any(CanTarget));

	public YoureFired()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
	{
	}

	public bool CanTarget(Creature target) => !DeportCmd.IsImmune(target);

	protected override async Task OnPaidPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await TrumpVfx.YoureFired();
		await DeportCmd.ForceDeport(choiceContext, cardPlay.Target, Owner);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[GoldCostVar].UpgradeValueBy(-24m);
	}
}
