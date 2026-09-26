// The video thumbnail (T7): the back-to-back key art with the title on the suits, clear of both faces and of the corner
// where YouTube prints the running time. Rendered as a still at 1920x1080 (and scaled to 1280x720 for upload).
import React from 'react';
import {AbsoluteFill, Img, staticFile, useVideoConfig} from 'remotion';
import {FONTS, loadFonts} from '../fonts';
import {foil} from './Overlays';

export type ThumbnailProps = {art: string};

const Star: React.FC<{size: number}> = ({size}) => (
	<svg width={size} height={size} viewBox="0 0 24 24" style={{display: 'block'}}>
		<path d="M12 1.8l3.1 6.6 7.2.9-5.3 5 1.4 7.1L12 18l-6.4 3.4 1.4-7.1-5.3-5 7.2-.9z" fill="#f3c65a" stroke="rgba(60,30,5,0.9)" strokeWidth="0.8" />
	</svg>
);

export const Thumbnail: React.FC<ThumbnailProps> = ({art}) => {
	loadFonts();
	const {width, height} = useVideoConfig();
	const main = width * 0.118;
	return (
		<AbsoluteFill style={{backgroundColor: 'black'}}>
			<Img src={staticFile(art)} style={{width: '100%', height: '100%', objectFit: 'cover', objectPosition: '50% 30%', filter: 'saturate(1.12) contrast(1.06)'}} />
			<AbsoluteFill style={{background: 'radial-gradient(ellipse at 50% 40%, rgba(0,0,0,0) 50%, rgba(0,0,0,0.45) 100%)'}} />
			<AbsoluteFill style={{background: 'linear-gradient(180deg, rgba(0,0,0,0) 52%, rgba(0,0,0,0.72) 74%, rgba(0,0,0,0.9) 100%)'}} />
			{/* Top-left ribbon: what it is, at a glance. */}
			<div
				style={{
					position: 'absolute',
					left: 0,
					top: height * 0.06,
					padding: `${height * 0.016}px ${width * 0.022}px ${height * 0.016}px ${width * 0.03}px`,
					background: 'linear-gradient(90deg, #8e1414, #c42424)',
					borderTop: '4px solid #f3c65a',
					borderBottom: '4px solid #f3c65a',
					boxShadow: '0 8px 24px rgba(0,0,0,0.6)',
					fontFamily: FONTS.kreon.family,
					fontSize: width * 0.03,
					letterSpacing: width * 0.003,
					color: '#fff6e0',
					textShadow: '0 3px 0 rgba(0,0,0,0.5)',
				}}
			>
				SLAY THE SPIRE 2 · FREE MOD
			</div>
			<AbsoluteFill style={{alignItems: 'center', justifyContent: 'flex-end', paddingBottom: height * 0.05, paddingRight: width * 0.04}}>
				<div
					style={{
						fontFamily: FONTS.spectral.family,
						...foil(main),
						letterSpacing: main * 0.03,
						filter: `drop-shadow(0 ${main * 0.05}px 0 rgba(40,18,0,0.95)) drop-shadow(0 0 ${main * 0.3}px rgba(0,0,0,0.8))`,
					}}
				>
					PRESIDENTS
				</div>
				<div style={{display: 'flex', alignItems: 'center', gap: width * 0.014, marginTop: height * 0.012}}>
					<Star size={width * 0.03} />
					<div style={{fontFamily: FONTS.spectral.family, ...foil(width * 0.05), letterSpacing: width * 0.009, filter: 'drop-shadow(0 5px 0 rgba(40,18,0,0.95))'}}>OF THE SPIRE</div>
					<Star size={width * 0.03} />
				</div>
			</AbsoluteFill>
		</AbsoluteFill>
	);
};
