import { getModule } from "cs2/modding"

const path$ = "game-ui/game/components/tool-options/tool-button/tool-button.module.scss"

export type PropsToolButtonSCSS = {
	button: string
	icon: string
	multiSelect: string
	iconButton: string
	badge: string
	indicator: string
}

export const ToolButtonSCSS: PropsToolButtonSCSS = getModule(path$, "classes")
