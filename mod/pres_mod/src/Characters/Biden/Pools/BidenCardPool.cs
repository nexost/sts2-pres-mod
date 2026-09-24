using Godot;
using MegaCrit.Sts2.Core.Models;
using PresMod.Characters.Biden.Cards;

namespace PresMod.Characters.Biden.Pools;

public sealed class BidenCardPool : CardPoolModel
{
	/// <summary>Also the portrait folder: images/packed/card_portraits/biden/&lt;card&gt;.png</summary>
	public override string Title => "biden";

	public override string EnergyColorName => Biden.energyColorName;

	/// <summary>res://materials/cards/frames/card_frame_biden_mat.tres (a hue shift of the shared frame, character.json frame_hsv).</summary>
	public override string CardFrameMaterialPath => "card_frame_biden";

	public override Color DeckEntryCardColor => new Color("434CCF");

	public override Color EnergyOutlineColor => new Color("252A73");

	public override bool IsColorless => false;

	/// <summary>Every card from characters/biden/design/cards.json (tokens that only borrow the frame stay out).</summary>
	protected override CardModel[] GenerateAllCards()
	{
		return new CardModel[]
		{
			ModelDb.Card<StrikeBiden>(),
			ModelDb.Card<DefendBiden>(),
			// Stubs so card rewards (3 different cards) and the shop (Attacks, Skills, a Power) work from day one.
			// Remove them once the real cards are in.
			ModelDb.Card<JabBiden>(),
			ModelDb.Card<OneTwoBiden>(),
			ModelDb.Card<BraceBiden>(),
			ModelDb.Card<RegroupBiden>(),
			ModelDb.Card<ResolveBiden>()
		};
	}
}
