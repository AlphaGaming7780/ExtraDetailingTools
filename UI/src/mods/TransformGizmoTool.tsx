import { ModuleRegistryExtend } from "cs2/modding";
import { PropsSection, Section } from "../../game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options";
import { PropsToolButton, ToolButton, ValueToolButton } from "../../game-ui/game/components/tool-options/tool-button/tool-button";
import { bindValue, trigger, useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";import { Tool, tool } from "cs2/bindings";
import { Tooltip } from "cs2/ui";
import { kGroupName } from "BindingConst";

enum Mode {
	Default = 0,
	Move = 1,
	Rotate = 2,
	Scale = 3,
}

const kToolId = "TransformGizmoTool";
const localAxis$ = bindValue<boolean>(kGroupName, `${kToolId}.LocalAxis`, false);
const moveSubBuildings$ = bindValue<boolean>(kGroupName, `${kToolId}.MoveSubBuildings`, false);
const haSubBuildings$ = bindValue<boolean>(kGroupName, `${kToolId}.HasSubBuildings`, false);

export const TransformGizmoTool: ModuleRegistryExtend = (Component: any) => {
	return (props) => {

		let activeTool: Tool = useValue(tool.activeTool$);
		let useLocalAxis : boolean = useValue(localAxis$);
		let moveSubBuildings : boolean = useValue(moveSubBuildings$);
		let haSubBuildings : boolean = useValue(haSubBuildings$);
		let currentMode : Number = activeTool.modeIndex;

		const { translate } = useLocalization();

		const setMode = (mode : Number) =>
		{
			trigger(kGroupName, `${kToolId}.SelectMode`, mode);
		}

		const LocalAxis = (enable : boolean) =>
		{
			trigger(kGroupName, `${kToolId}.LocalAxis`, enable);
		}

		const MoveSubBuildings = (enable : boolean) =>
		{
			trigger(kGroupName, `${kToolId}.MoveSubBuildings`, enable);
		}

		// This defines aspects of the components.
		const { children, ...otherProps } = props || {};

		var result: JSX.Element = Component();


		if (activeTool.id !== kToolId) return result;

		console.log(props);

		result.props.children?.unshift(
			<>
			<Section
				title={"Tool mods:"}
			>
				<ValueToolButton<Number>
				value={Mode.Default}
				selected={currentMode === Mode.Default}
				onSelect={(v) => setMode(v)}
				/>

				<ValueToolButton<Number>
				value={Mode.Move}
				selected={currentMode === Mode.Move}
				onSelect={(v) => setMode(v)}
				/>

				<ValueToolButton<Number>
				value={Mode.Rotate}
				selected={currentMode === Mode.Rotate}
				onSelect={(v) => setMode(v)}
				/>

				<ValueToolButton<Number>
				value={Mode.Scale}
				selected={currentMode === Mode.Scale}
				onSelect={(v) => setMode(v)}
				/>

			</Section>
			<Section
				title={"Tool settings:"}
			>
				
				<ToolButton
					tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.LOCALAXIS")}
					src="Media/Tools/Snap Options/All.svg"
					selected={useLocalAxis}
					onSelect={() => LocalAxis(!useLocalAxis)}
				/>

				{ haSubBuildings ? 
					<ToolButton
						tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.MoveSubBuildings.tooltip")}
						src="Media/Tools/Snap Options/All.svg"
						selected={moveSubBuildings}
						onSelect={() => MoveSubBuildings(!moveSubBuildings)}
					/> : <></>
				}
				
			</Section>
			</>
		)

		return result;
	};
}