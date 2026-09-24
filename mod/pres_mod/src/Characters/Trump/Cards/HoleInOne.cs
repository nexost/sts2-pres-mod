using MegaCrit.Sts2.Core.Combat.History.Entries;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Cards;

/// <summary>0 cost and big, but only as the first card you play each turn.</summary>
public sealed class HoleInOne : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(14m, ValueProp.Move) };

	protected override bool IsPlayable => IsCanonical || CombatState == null || !PlayedAnythingThisTurn();

	protected override bool ShouldGlowGoldInternal => IsMutable && CombatState != null && !PlayedAnythingThisTurn();

	public HoleInOne()
		: base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(6m);
	}

	private bool PlayedAnythingThisTurn()
	{
		return CombatManager.Instance.History.CardPlaysFinished.Any((CardPlayFinishedEntry e) => e.HappenedThisTurn(CombatState) && e.CardPlay.Card.Owner == Owner);
	}
}
