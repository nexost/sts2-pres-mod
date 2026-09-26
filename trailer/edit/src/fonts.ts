// The game's own open-licensed fonts (SIL OFL), copied from the game's files into build/trailer/media/fonts/.
import {continueRender, delayRender, staticFile} from 'remotion';

export const FONTS = {
	kreon: {family: 'TrailerKreon', file: 'fonts/kreon_bold.ttf'},
	spectral: {family: 'TrailerSpectral', file: 'fonts/spectral_bold.ttf'},
	fira: {family: 'TrailerFira', file: 'fonts/FiraSansExtraCondensed-Bold.ttf'},
} as const;

let loaded = false;

/** Registers every font once, holding the render until they are ready. */
export const loadFonts = () => {
	if (loaded || typeof document === 'undefined') {
		return;
	}
	loaded = true;
	const handle = delayRender('Loading fonts');
	Promise.all(
		Object.values(FONTS).map(async ({family, file}) => {
			const face = new FontFace(family, `url(${staticFile(file)})`);
			await face.load();
			document.fonts.add(face);
		}),
	)
		.then(() => continueRender(handle))
		.catch((err) => {
			console.error(err);
			continueRender(handle);
		});
};
