using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using PresMod.Characters.Trump.Pools;
using PresMod.Characters.Trump.Cards;
using PresMod.Characters.Trump.Relics;

using PresMod.Characters.Trump.Mechanics;
using PresMod.Framework;

namespace PresMod.Characters.Trump;

/// <summary>
/// The playable character. Id entry is TRUMP, so the game looks for assets named "trump"
/// (scenes/creature_visuals/trump.tscn, images/ui/top_panel/character_icon_trump.png, ...).
/// Stats and deck from the design (characters/trump/design/design.md §1): 70 HP, 99 Gold, Golden Shovel.
/// </summary>
public sealed class Trump : CharacterModel, IModCharacter
{
	public const string energyColorName = "trump";

	/// <summary>Borrow Ironclad's FMOD events until we have our own sounds.</summary>
	public string SfxProxyEntry => "ironclad";

	public override CharacterGender Gender => CharacterGender.Masculine;

	protected override CharacterModel? UnlocksAfterRunAs => null;

	public override Color NameColor => new Color("F2B92E");

	public override int StartingHp => 70;

	public override int StartingGold => 99;

	public override CardPoolModel CardPool => ModelDb.CardPool<TrumpCardPool>();

	public override PotionPoolModel PotionPool => ModelDb.PotionPool<TrumpPotionPool>();

	public override RelicPoolModel RelicPool => ModelDb.RelicPool<TrumpRelicPool>();

	public override IEnumerable<CardModel> StartingDeck => new CardModel[]
	{
		ModelDb.Card<StrikeTrump>(),
		ModelDb.Card<StrikeTrump>(),
		ModelDb.Card<StrikeTrump>(),
		ModelDb.Card<StrikeTrump>(),
		ModelDb.Card<DefendTrump>(),
		ModelDb.Card<DefendTrump>(),
		ModelDb.Card<DefendTrump>(),
		ModelDb.Card<DefendTrump>(),
		ModelDb.Card<BuildTheWall>(),
		ModelDb.Card<Deport>()
	};

	public override IReadOnlyList<RelicModel> StartingRelics => new RelicModel[] { ModelDb.Relic<GoldenShovel>() };

	public override float AttackAnimDelay => 0.15f;

	public override float CastAnimDelay => 0.25f;

	public override Color EnergyLabelOutlineColor => new Color("7A5200FF");

	public override Color DialogueColor => new Color("3A2A00");

	public override VfxColor SpeechBubbleColor => VfxColor.Gold;

	public override Color MapDrawingColor => new Color("C8961E");

	public override Color RemoteTargetingLineColor => new Color("F2B92EFF");

	public override Color RemoteTargetingLineOutline => new Color("7A5200FF");

	public override string CharacterSelectSfx => "event:/sfx/characters/ironclad/ironclad_select";

	public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_ironclad";

	public override List<string> GetArchitectAttackVfx()
	{
		return new List<string> { "vfx/vfx_attack_blunt", "vfx/vfx_heavy_blunt", "vfx/vfx_attack_slash" };
	}
}
