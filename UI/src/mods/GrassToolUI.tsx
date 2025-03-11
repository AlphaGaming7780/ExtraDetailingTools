import { ModuleRegistryExtend } from "cs2/modding";
import { PropsSection, Section } from "../../game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options";
import { PropsToolButton, ToolButton } from "../../game-ui/game/components/tool-options/tool-button/tool-button";
import { bindValue, trigger, useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";import { Tool, tool } from "cs2/bindings";

export const GrassToolUI: ModuleRegistryExtend = (Component: any) => {
	return (props) => {

		let activeTool: Tool = useValue(tool.activeTool$);
		const { translate } = useLocalization();

		// This defines aspects of the components.
		//const { children, ...otherProps } = props || {};

		// This gets the original component that we may alter and return.
		var result: JSX.Element = Component(props);

		//console.log(activeTool);

		if (activeTool.id === "Grass Tool") {

			result.props.children?.unshift(
				<div></div>
			);
		}

		//console.log(result)

		return result;
	};
}