import { getModule } from "cs2/modding"

const path$ = "game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.module.scss"

export type PropsMouseToolOptionsSCSS = {
	item: string
	label: string
	content: string
	numberField: string
	numberInputField: string
	startButton: string
	endButton: string
	dropdownToggle: string
	slider: string
}

export const MouseToolOptionsSCSS: PropsMouseToolOptionsSCSS = getModule(path$, "classes")
