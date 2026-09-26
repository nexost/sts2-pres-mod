// The README's banner: the walk key art with the title lockup, wide for a repo page. A still:
//   npx remotion still src/index.ts Banner out/banner.png
import React from 'react';
import {AbsoluteFill, Img, staticFile, useVideoConfig} from 'remotion';
import {FONTS, loadFonts} from '../fonts';
import {foil} from './Overlays';

export type BannerProps = {art: string};

const Star: React.FC<{size: number}> = ({size}) => (
	<svg width={size} height={size} viewBox="0 0 24 24" style={{display: 'block'}}>
		<path d="M12 1.8l3.1 6.6 7.2.9-5.3 5 1.4 7.1L12 18l-6.4 3.4 1.4-7.1-5.3-5 7.2-.9z" fill="#f3c65a" stroke="rgba(60,30,5,0.9)" strokeWidth="0.8" />
	</svg>
);

export const Banner: React.FC<BannerProps> = ({art}) => {
	loadFonts();
	const {width, height} = useVideoConfig();
	const main = height * 0.21;
	return (
		<AbsoluteFill style={{backgroundColor: 'black'}}>
			<Img src={staticFile(art)} style={{width: '100%', height: '100%', objectFit: 'cover', objectPosition: '50% 28%', filter: 'saturate(1.1) contrast(1.05)'}} />
			<AbsoluteFill style={{background: 'linear-gradient(180deg, rgba(0,0,0,0.15) 0%, rgba(0,0,0,0) 30%, rgba(0,0,0,0.25) 55%, rgba(0,0,0,0.85) 100%)'}} />
			<AbsoluteFill style={{alignItems: 'center', justifyContent: 'flex-end', paddingBottom: height * 0.07}}>
				<div style={{fontFamily: FONTS.spectral.family, ...foil(main), letterSpacing: main * 0.035, filter: `drop-shadow(0 ${main * 0.05}px 0 rgba(40,18,0,0.95)) drop-shadow(0 0 ${main * 0.3}px rgba(0,0,0,0.7))`}}>
					PRESIDENTS
				</div>
				<div style={{display: 'flex', alignItems: 'center', gap: width * 0.012, marginTop: height * 0.012}}>
					<div style={{width: width * 0.07, height: 4, background: 'linear-gradient(90deg, rgba(243,198,90,0), #f3c65a)'}} />
					<Star size={height * 0.045} />
					<div style={{fontFamily: FONTS.spectral.family, ...foil(height * 0.075), letterSpacing: height * 0.014, filter: 'drop-shadow(0 4px 0 rgba(40,18,0,0.95))'}}>OF THE SPIRE</div>
					<Star size={height * 0.045} />
					<div style={{width: width * 0.07, height: 4, background: 'linear-gradient(90deg, #f3c65a, rgba(243,198,90,0))'}} />
				</div>
				<div style={{marginTop: height * 0.03, fontFamily: FONTS.kreon.family, fontSize: height * 0.042, letterSpacing: height * 0.008, color: '#f4ecd8', textShadow: '0 3px 12px rgba(0,0,0,0.9)'}}>
					A FREE MOD FOR SLAY THE SPIRE 2
				</div>
			</AbsoluteFill>
		</AbsoluteFill>
	);
};
