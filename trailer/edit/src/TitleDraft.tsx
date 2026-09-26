// Title card drafts (T5): the logo lockup over the key art, in three of the game's fonts. Rendered as stills to choose one.
import React from 'react';
import {AbsoluteFill, Img, staticFile} from 'remotion';
import {FONTS, loadFonts} from './fonts';

export type TitleDraftProps = {variant: 'spectral' | 'kreon' | 'fira'; art: string};

const GOLD = 'linear-gradient(180deg, #fff7d6 0%, #ffe08a 22%, #f0b83c 48%, #b9791b 72%, #ffe9a8 100%)';

const Star: React.FC<{size: number}> = ({size}) => (
	<svg width={size} height={size} viewBox="0 0 24 24" style={{display: 'block'}}>
		<path d="M12 1.8l3.1 6.6 7.2.9-5.3 5 1.4 7.1L12 18l-6.4 3.4 1.4-7.1-5.3-5 7.2-.9z" fill="#f3c65a" />
	</svg>
);

export const TitleDraft: React.FC<TitleDraftProps> = ({variant, art}) => {
	loadFonts();
	const family = FONTS[variant].family;
	const condensed = variant === 'fira';
	const main: React.CSSProperties = {
		fontFamily: family,
		fontSize: condensed ? 290 : 230,
		lineHeight: 0.92,
		letterSpacing: condensed ? 14 : 8,
		backgroundImage: GOLD,
		WebkitBackgroundClip: 'text',
		backgroundClip: 'text',
		color: 'transparent',
		WebkitTextStroke: '3px rgba(60, 30, 5, 0.9)',
		filter: 'drop-shadow(0 10px 0 rgba(40, 18, 0, 0.9)) drop-shadow(0 0 40px rgba(255, 190, 70, 0.45))',
	};
	return (
		<AbsoluteFill style={{backgroundColor: 'black'}}>
			<Img src={staticFile(art)} style={{width: '100%', height: '100%', objectFit: 'cover'}} />
			<AbsoluteFill style={{background: 'linear-gradient(180deg, rgba(0,0,0,0.25) 0%, rgba(0,0,0,0) 30%, rgba(0,0,0,0) 48%, rgba(0,0,0,0.72) 72%, rgba(0,0,0,0.92) 100%)'}} />
			{/* The logo sits low, over the ground: the key art has the presidents' faces in the upper half. */}
			<AbsoluteFill style={{alignItems: 'center', justifyContent: 'flex-end', paddingBottom: 90}}>
				<div style={{...main}}>PRESIDENTS</div>
				<div style={{display: 'flex', alignItems: 'center', gap: 34, marginTop: 18}}>
					<div style={{width: 240, height: 4, background: 'linear-gradient(90deg, rgba(243,198,90,0), #f3c65a)'}} />
					<Star size={54} />
					<div style={{...main, fontSize: condensed ? 120 : 96, letterSpacing: condensed ? 22 : 18, WebkitTextStroke: '2px rgba(60,30,5,0.9)'}}>OF THE SPIRE</div>
					<Star size={54} />
					<div style={{width: 240, height: 4, background: 'linear-gradient(90deg, #f3c65a, rgba(243,198,90,0))'}} />
				</div>
				<div style={{marginTop: 34, fontFamily: FONTS.kreon.family, fontSize: 44, letterSpacing: 10, color: '#f4ecd8', textShadow: '0 3px 12px rgba(0,0,0,0.9)'}}>
					A FREE MOD FOR SLAY THE SPIRE 2
				</div>
			</AbsoluteFill>
		</AbsoluteFill>
	);
};
