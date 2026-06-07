import { ModuleRegistryExtend } from "cs2/modding";
import { PropsSection, Section } from "../../../game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options";
import { PropsToolButton, ToolButton, ValueToolButton } from "../../../game-ui/game/components/tool-options/tool-button/tool-button";
import { bindValue, trigger, useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import { Tool, tool } from "cs2/bindings";
import { Button, FOCUS_AUTO, FOCUS_DISABLED, Tooltip } from "cs2/ui";
import { kGroupName } from "BindingConst";
import { Float3, TransformInputs } from "../TransformSection";
import styles from "./TransformGizmoToolStyle.module.scss";
import classNames from "classnames";
import { FOCUS_DISABLED$ } from "../../../game-ui/common/focus/focus-key";

enum Mode {
	Default = 0,
	Move = 1,
	Rotate = 2,
	Scale = 3,
}

export const kTransformGizmoToolId = "TransformGizmoTool";
const toolMode$ = bindValue<Number>(kGroupName, `${kTransformGizmoToolId}.ToolMode`, 0);
const pos$ = bindValue<Float3>(kGroupName, `${kTransformGizmoToolId}.Position`, {x: 0, y: 0, z:0});
const rot$ = bindValue<Float3>(kGroupName, `${kTransformGizmoToolId}.Rotation`, {x: 0, y: 0, z:0});
// const scale$ = bindValue<Float3>(kGroupName, `${kToolId}.Scale`, {x: 0, y: 0, z:0});
const localAxis$ = bindValue<boolean>(kGroupName, `${kTransformGizmoToolId}.LocalAxis`, false);
const followGround$ = bindValue<boolean>(kGroupName, `${kTransformGizmoToolId}.FollowGround`, false);
const moveSubBuildings$ = bindValue<boolean>(kGroupName, `${kTransformGizmoToolId}.MoveSubBuildings`, false);
const haSubBuildings$ = bindValue<boolean>(kGroupName, `${kTransformGizmoToolId}.HasSubBuildings`, false);

export const TransformGizmoTool: ModuleRegistryExtend = (Component: any) => {
	return (props) => {

		const pos: Float3 = useValue(pos$);
		const rot: Float3 = useValue(rot$);
		// const scale: Float3 = useValue(scale$);
		const activeTool: Tool = useValue(tool.activeTool$);
		const useLocalAxis : boolean = useValue(localAxis$);
		const useFollowGround : boolean = useValue(followGround$);
		const moveSubBuildings : boolean = useValue(moveSubBuildings$);
		const haSubBuildings : boolean = useValue(haSubBuildings$);
		const currentMode : Number = useValue(toolMode$);

		const { translate } = useLocalization();

		const setMode = (mode : Number) =>
		{
			trigger(kGroupName, `${kTransformGizmoToolId}.ToolMode`, mode);
		}

		const LocalAxis = (enable : boolean) =>
		{
			trigger(kGroupName, `${kTransformGizmoToolId}.LocalAxis`, enable);
		}

		const MoveSubBuildings = (enable : boolean) =>
		{
			trigger(kGroupName, `${kTransformGizmoToolId}.MoveSubBuildings`, enable);
		}

		const SnapOnGround = () =>
		{
			trigger(kGroupName, `${kTransformGizmoToolId}.SnapOnGround`);
		}

		const FollowGround = (enable : boolean) =>
		{
			trigger(kGroupName, `${kTransformGizmoToolId}.FollowGround`, enable);
		}

		// This defines aspects of the components.
		const { children, ...otherProps } = props || {};

		var result: JSX.Element = Component();

		const posTransformInputs = TransformInputs(translate, "POS", pos, false)
		const rotTransformInputs = TransformInputs(translate, "ROT", rot, false)
		// const scaleTransformInputs = TransformInputs(translate, "SCALE", scale, false)

		if (activeTool.id !== kTransformGizmoToolId) return result;

		result.props.children?.unshift(
			<>
			<Section
				title={translate("Tool.TransformGizmoTool.Mods", "Mods")}
			>
				<Tooltip tooltip={translate("Tool.TransformGizmoTool.Default.Tooltip", "Tool.TransformGizmoTool.Default.Tooltip")}>
					<ValueToolButton<Number>
						focusKey={FOCUS_DISABLED$}
						value={Mode.Default}
						selected={currentMode === Mode.Default}
						onSelect={(v) => setMode(v)}
						src="coui://extradetailingtools/Icons/TransformGizmosTool/Default.svg"
					/>
				</Tooltip>

				<Tooltip tooltip={translate("Tool.TransformGizmoTool.Move.Tooltip", "Tool.TransformGizmoTool.Move.Tooltip")}>
					<ValueToolButton<Number>
						focusKey={FOCUS_DISABLED$}
						value={Mode.Move}
						selected={currentMode === Mode.Move}
						onSelect={(v) => setMode(v)}
						src="coui://extradetailingtools/Icons/TransformGizmosTool/Move.svg"
					/>
				</Tooltip>

				<Tooltip tooltip={translate("Tool.TransformGizmoTool.Rotate.Tooltip", "Tool.TransformGizmoTool.Rotate.Tooltip")}>
					<ValueToolButton<Number>
						focusKey={FOCUS_DISABLED$}
						value={Mode.Rotate}
						selected={currentMode === Mode.Rotate}
						onSelect={(v) => setMode(v)}
						src="coui://extradetailingtools/Icons/TransformGizmosTool/Rotate.svg"
					/>
				</Tooltip>

				{/* <ValueToolButton<Number>
				value={Mode.Scale}
				selected={currentMode === Mode.Scale}
				onSelect={(v) => setMode(v)}
				/> */}

			</Section>
			<Section
				title={translate("Tool.TransformGizmoTool.Settings", "Settings")}
			>
				
				<ToolButton
					focusKey={FOCUS_DISABLED$}
					tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.LOCALAXIS")}
					src="coui://extradetailingtools/Icons/TransformGizmosTool/Axis.svg"
					selected={useLocalAxis}
					onSelect={() => LocalAxis(!useLocalAxis)}
				/>

				<ToolButton
					focusKey={FOCUS_DISABLED$}
					tooltip={translate("Tool.TransformGizmoTool.FollowGround.tooltip")}
					src="coui://extradetailingtools/Icons/TransformGizmosTool/FollowGround.svg"
					selected={useFollowGround}
					onSelect={() => FollowGround(!useFollowGround)}
				/>

				{ haSubBuildings ? 
					<ToolButton
						focusKey={FOCUS_DISABLED$}
						tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.MoveSubBuildings.tooltip")}
						src={moveSubBuildings ? "coui://extradetailingtools/Icons/TransformGizmosTool/Building_V.svg" : "coui://extradetailingtools/Icons/TransformGizmosTool/Building_X.svg"}
						selected={moveSubBuildings}
						onSelect={() => MoveSubBuildings(!moveSubBuildings)}
					/> : <></>
				}
				
			</Section>

			<Section
				title={translate("Tool.TransformGizmoTool.QuickActions", "Quick Actions") }
			>
				<ToolButton
					focusKey={FOCUS_DISABLED$}
					tooltip={translate("Tool.TransformGizmoTool.SnapOnGround.tooltip")}
					src="coui://extradetailingtools/Icons/TransformGizmosTool/SnapOnGround.svg"
					onSelect={() => SnapOnGround()}
				/>
				
			</Section>

			{/* <Section
			title={translate("PhotoMode.PROPERTY_TITLE[Position]")}
			>
				{posTransformInputs}
			</Section>
			<Section
			title={translate("PhotoMode.PROPERTY_TITLE[Rotation]")}
			>
				{rotTransformInputs}
			</Section> */}
			{/* <Section
			title={translate("SelectedInfoPanel.TRANSFORMTOOL.SCALE")}
			>
				{scaleTransformInputs}
			</Section> */}
			</>
		)

		return result;
	};
}

export const TransformGizmosToolButton = () => 
{
	const activeTool: Tool = useValue(tool.activeTool$);
	const active = activeTool.id == kTransformGizmoToolId

	const { translate } = useLocalization();

	return <>
		<Tooltip tooltip={translate("Tool.TransformGizmoTool.Tooltip", "Tool.TransformGizmoTool.Tooltip")}>
			<Button
				variant="floating"
				src="coui://extradetailingtools/Icons/TransformGizmosTool/Icon.svg"
				tooltipLabel={translate("Tool.TransformGizmoTool.Tooltip", "Tool.TransformGizmoTool.Tooltip")}
				className={classNames(
					styles.panelButtonUM,
					(active) && styles.active
				)}
				selected={active}
				onClick={() =>
					active
						? tool.selectTool("Default Tool")
						: trigger("edt", "selectTransformGizmosTool")
				}
			/>
		</Tooltip>
	</>
}