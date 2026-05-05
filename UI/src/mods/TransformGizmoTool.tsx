import { ModuleRegistryExtend } from "cs2/modding";
import { PropsSection, Section } from "../../game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options";
import { PropsToolButton, ToolButton } from "../../game-ui/game/components/tool-options/tool-button/tool-button";
import { bindValue, trigger, useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";import { Tool, tool } from "cs2/bindings";

enum Mode
{
	Default,
	Move,
	Rotate,
	Scale,
}

const kToolId = "TransformGizmoTool";

export const TransformGizmoTool: ModuleRegistryExtend = (Component: any) => {
	return (props) => {

		let activeTool: Tool = useValue(tool.activeTool$);

		const { translate } = useLocalization();

		// let modesSelectorSection: PropsSection = {
		// 	title: translate("Toolbar.SHOWMARKER"),//"Show Marker",
		// 	children: 
		// }

		// This defines aspects of the components.
		const { children, ...otherProps } = props || {};

		// This gets the original component that we may alter and return.
		var result: JSX.Element = Component();

		// var EDTTool = Section(ShowMarkerProps)

		if (activeTool.id !== kToolId) return result;



		return result;
	};
}