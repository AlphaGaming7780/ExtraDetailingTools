import { ModuleRegistryExtend } from "cs2/modding";
import { PropsSection, Section } from "../../../game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options";
import { PropsToolButton, ToolButton, ValueToolButton } from "../../../game-ui/game/components/tool-options/tool-button/tool-button";
import { bindValue, trigger, useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import { Tool, tool } from "cs2/bindings";
import { Button, FOCUS_AUTO, FOCUS_DISABLED, Tooltip } from "cs2/ui";
import { kGroupName } from "../../BindingConst";
import { Float3 } from "../TransformPanel/TransformPanel";
import styles from "./TransformGizmoToolStyle.module.scss";
import classNames from "classnames";
import { FOCUS_DISABLED$ } from "../../../game-ui/common/focus/focus-key";
import { kTransformSection$ } from "../TransformSection/TransformSection";

enum Mode {
	Default = 0,
	Move = 1,
	Rotate = 2,
	Scale = 3,
}

enum XZHandleMode {
	FollowSurface = 0,
	FixedX,
	FixedY,
	FixedZ,
}

enum RaycastFilter {
	None = 0,
	StaticObject = 1,
	Decals = 2,
	Buildings = 4,
	MovingObject = 8,
}

export const kTransformGizmoToolId = "TransformGizmoTool";
const toolMode$ = bindValue<Number>(kGroupName, `${kTransformGizmoToolId}.ToolMode`, 0);
const pos$ = bindValue<Float3>(kGroupName, `${kTransformGizmoToolId}.Position`, {x: 0, y: 0, z:0});
const rot$ = bindValue<Float3>(kGroupName, `${kTransformGizmoToolId}.Rotation`, {x: 0, y: 0, z:0});
// const scale$ = bindValue<Float3>(kGroupName, `${kToolId}.Scale`, {x: 0, y: 0, z:0});
const localAxis$ = bindValue<boolean>(kGroupName, `${kTransformGizmoToolId}.LocalAxis`, false);
const xzHandleMode$ = bindValue<Number>(kGroupName, `${kTransformGizmoToolId}.XZHandleMode`, 0);
const snapToSurface$ = bindValue<boolean>(kGroupName, `${kTransformGizmoToolId}.SnapToSurface`, false);
const moveSubBuildings$ = bindValue<boolean>(kGroupName, `${kTransformGizmoToolId}.MoveSubBuildings`, false);
const haSubBuildings$ = bindValue<boolean>(kGroupName, `${kTransformGizmoToolId}.HasSubBuildings`, false);
const raycastFilter$ = bindValue<Number>(kGroupName, `${kTransformGizmoToolId}.RaycastFilter`, 0xFFFFFFFF);

export const TransformGizmoTool: ModuleRegistryExtend = (Component: any) => {
	return (props) => {

		const pos: Float3 = useValue(pos$);
		const rot: Float3 = useValue(rot$);
		// const scale: Float3 = useValue(scale$);
		const activeTool: Tool = useValue(tool.activeTool$);
		const useLocalAxis : boolean = useValue(localAxis$);
		const useSnapToSurface : boolean = useValue(snapToSurface$);
		const moveSubBuildings : boolean = useValue(moveSubBuildings$);
		const haSubBuildings : boolean = useValue(haSubBuildings$);
		const currentMode : Number = useValue(toolMode$);
		const currentXZHandleMode : Number = useValue(xzHandleMode$);
		const raycastFilter : Number = useValue(raycastFilter$);

		const { translate } = useLocalization();

		const hasFlag = (flag: RaycastFilter) => ((raycastFilter as number) & flag) !== 0;

		const toggleFlag = (flag: RaycastFilter) => {
			let current = raycastFilter as number;
			current = (current & flag) !== 0 ? current & ~flag : current | flag;
			trigger(kGroupName, `${kTransformGizmoToolId}.RaycastFilter`, current);
		};

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
		
		const SetXZHandleMode = (xzHandleMode : Number) =>
		{
			trigger(kGroupName, `${kTransformGizmoToolId}.XZHandleMode`, xzHandleMode);
		}

		// This defines aspects of the components.
		const { children, ...otherProps } = props || {};

		var result: JSX.Element = Component();

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
					tooltip={translate("TransformPanel.LOCALAXIS")}
					src="coui://extradetailingtools/Icons/TransformGizmosTool/Axis.svg"
					selected={useLocalAxis}
					onSelect={() => LocalAxis(!useLocalAxis)}
				/>

				{ haSubBuildings ? 
					<ToolButton
						focusKey={FOCUS_DISABLED$}
						tooltip={translate("TransformPanel.MoveSubBuildings.tooltip")}
						src={moveSubBuildings ? "coui://extradetailingtools/Icons/TransformGizmosTool/Building_V.svg" : "coui://extradetailingtools/Icons/TransformGizmosTool/Building_X.svg"}
						selected={moveSubBuildings}
						onSelect={() => MoveSubBuildings(!moveSubBuildings)}
					/> : <></>
				}
				
			</Section>

			{ currentMode === Mode.Default &&
				<Section
					title={translate("Tool.TransformGizmoTool.RaycastFilter", "Raycast Filter")}
				>
					<ToolButton
						focusKey={FOCUS_DISABLED$}
						tooltip={translate("Tool.TransformGizmoTool.RaycastFilter.StaticObject.Tooltip", "Static Objects")}
						src="Media/Game/Icons/Props.svg"
						selected={hasFlag(RaycastFilter.StaticObject)}
						onSelect={() => toggleFlag(RaycastFilter.StaticObject)}
					/>

					<ToolButton
						focusKey={FOCUS_DISABLED$}
						tooltip={translate("Tool.TransformGizmoTool.RaycastFilter.Decals.Tooltip", "Decals")}
						src="Media/Game/Icons/PropsDecals.svg"
						selected={hasFlag(RaycastFilter.StaticObject) && hasFlag(RaycastFilter.Decals)}
						disabled={!hasFlag(RaycastFilter.StaticObject)}
						onSelect={() => toggleFlag(RaycastFilter.Decals)}
					/>

					{/* <ToolButton
						focusKey={FOCUS_DISABLED$}
						tooltip={translate("Tool.TransformGizmoTool.RaycastFilter.Buildings.Tooltip", "Buildings")}
						src="Media/Editor/Thumbnails/Fallback_BuildingPrefab.svg"
						selected={hasFlag(RaycastFilter.StaticObject) && hasFlag(RaycastFilter.Buildings)}
						disabled={!hasFlag(RaycastFilter.StaticObject)}
						onSelect={() => toggleFlag(RaycastFilter.Buildings)}
					/> */}

					<ToolButton
						focusKey={FOCUS_DISABLED$}
						tooltip={translate("Tool.TransformGizmoTool.RaycastFilter.MovingObject.Tooltip", "Moving Objects")}
						src="Media/Game/Icons/Traffic.svg"
						selected={hasFlag(RaycastFilter.MovingObject)}
						onSelect={() => toggleFlag(RaycastFilter.MovingObject)}
					/>
				</Section>
			}

			{ currentMode === Mode.Move &&
				<Section
					title={translate("Tool.TransformGizmoTool.XZHandleMode", "XZ Handle Mode")}
				>
					<Tooltip tooltip={translate("Tool.TransformGizmoTool.XZHandleMode.FollowSurface.Tooltip", "Follow Surface")}>
						<ValueToolButton<Number>
							focusKey={FOCUS_DISABLED$}
							value={XZHandleMode.FollowSurface}
							selected={currentXZHandleMode === XZHandleMode.FollowSurface}
							onSelect={(v) => SetXZHandleMode(v)}
							src="Media/Tools/Snap Options/ObjectSurface.svg"
						/>
					</Tooltip>

					<Tooltip tooltip={translate("Tool.TransformGizmoTool.XZHandleMode.FixedX.Tooltip", "Fixed X")}>
						<ValueToolButton<Number>
							focusKey={FOCUS_DISABLED$}
							value={XZHandleMode.FixedX}
							selected={currentXZHandleMode === XZHandleMode.FixedX}
							onSelect={(v) => SetXZHandleMode(v)}
							src="coui://extradetailingtools/Icons/TransformGizmosTool/FixedX.svg"
						/>
					</Tooltip>

					<Tooltip tooltip={translate("Tool.TransformGizmoTool.XZHandleMode.FixedY.Tooltip", "Fixed Y")}>
						<ValueToolButton<Number>
							focusKey={FOCUS_DISABLED$}
							value={XZHandleMode.FixedY}
							selected={currentXZHandleMode === XZHandleMode.FixedY}
							onSelect={(v) => SetXZHandleMode(v)}
							src="coui://extradetailingtools/Icons/TransformGizmosTool/FixedY.svg"
						/>
					</Tooltip>

					<Tooltip tooltip={translate("Tool.TransformGizmoTool.XZHandleMode.FixedZ.Tooltip", "Fixed Z")}>
						<ValueToolButton<Number>
							focusKey={FOCUS_DISABLED$}
							value={XZHandleMode.FixedZ}
							selected={currentXZHandleMode === XZHandleMode.FixedZ}
							onSelect={(v) => SetXZHandleMode(v)}
							src="coui://extradetailingtools/Icons/TransformGizmosTool/FixedZ.svg"
						/>
					</Tooltip>
				</Section>
			}

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
						: trigger("EDT",  `${kTransformGizmoToolId}.SelectTransformGizmosTool`)
				}
			/>
		</Tooltip>
	</>
}