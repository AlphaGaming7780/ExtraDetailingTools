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
import { AnarchyButtons } from "../AnarchyButtons/AnarchyButtons";
import { kTransformGizmoToolId } from "../../BindingConst";
import { InfoRowSCSS } from "../../../game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss";
import { InfoSectionFoldout } from "../../../game-ui/game/components/selected-info-panel/shared-components/info-section/info-section-foldout";
import { InfoSectionSCSS } from "../../../game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.module.scss";
import { ActionButtonSCSS } from "../../../game-ui/game/components/selected-info-panel/selected-info-sections/shared-sections/actions-section/action-button.module.scss";
import { StepInput } from "../Shared/StepInput";
import { MouseToolOptionsSCSS } from "../../../game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.module.scss";

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
	Water = 16,
	Net = 32,
}

const toolMode$ = bindValue<number>(kGroupName, `${kTransformGizmoToolId}.ToolMode`, 0);
const pos$ = bindValue<Float3>(kGroupName, `${kTransformGizmoToolId}.Position`, {x: 0, y: 0, z:0});
const rot$ = bindValue<Float3>(kGroupName, `${kTransformGizmoToolId}.Rotation`, {x: 0, y: 0, z:0});
// const scale$ = bindValue<Float3>(kGroupName, `${kToolId}.Scale`, {x: 0, y: 0, z:0});
const localAxis$ = bindValue<boolean>(kGroupName, `${kTransformGizmoToolId}.LocalAxis`, false);
const xzHandleMode$ = bindValue<number>(kGroupName, `${kTransformGizmoToolId}.XZHandleMode`, 0);
const snapToSurface$ = bindValue<boolean>(kGroupName, `${kTransformGizmoToolId}.SnapToSurface`, false);
const moveSubBuildings$ = bindValue<boolean>(kGroupName, `${kTransformGizmoToolId}.MoveSubBuildings`, false);
const haSubBuildings$ = bindValue<boolean>(kGroupName, `${kTransformGizmoToolId}.HasSubBuildings`, false);
const raycastFilter$ = bindValue<number>(kGroupName, `${kTransformGizmoToolId}.RaycastFilter`, 0);

const gridEnabled$ = bindValue<boolean>(kGroupName, `${kTransformGizmoToolId}.GridEnabled`, false);
const posOffset$ = bindValue<number>(kGroupName, `${kTransformGizmoToolId}.PosOffset`, 0);
const rotOffset$ = bindValue<number>(kGroupName, `${kTransformGizmoToolId}.RotOffset`, 0);

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
		const currentMode : number = useValue(toolMode$);
		const currentXZHandleMode : number = useValue(xzHandleMode$);
		const raycastFilter : number = useValue(raycastFilter$);
		const gridEnabled : boolean = useValue(gridEnabled$);
		const posOffset : number = useValue(posOffset$);
		const rotOffset : number = useValue(rotOffset$);

		const { translate } = useLocalization();

		const hasFlag = (flag: RaycastFilter) => ((raycastFilter as number) & flag) !== 0;

		const toggleFlag = (flag: RaycastFilter) => {
			let current = raycastFilter as number;
			current = (current & flag) !== 0 ? current & ~flag : current | flag;
			trigger(kGroupName, `${kTransformGizmoToolId}.RaycastFilter`, current);
		};

		const setMode = (mode : number) =>
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

		const Duplicate = () =>
		{
			trigger(kGroupName, `${kTransformGizmoToolId}.Duplicate`);
		}
		
		const SetXZHandleMode = (xzHandleMode : number) =>
		{
			trigger(kGroupName, `${kTransformGizmoToolId}.XZHandleMode`, xzHandleMode);
		}

		const SetGridEnabled = (enabled : boolean) =>
		{
			trigger(kGroupName, `${kTransformGizmoToolId}.GridEnabled`, enabled);
		}

		const SetPosOffset = (value : number) =>
		{
			trigger(kGroupName, `${kTransformGizmoToolId}.PosOffset`, value);
		}

		const SetRotOffset = (value : number) =>
		{
			trigger(kGroupName, `${kTransformGizmoToolId}.RotOffset`, value);
		}

		// Single-value scroll/drag/edit row used for the grid's position and rotation step. Thin wrapper
		// around the shared StepInput. StepInput is rendered as a real JSX element (not called as a plain
		// function) so its hooks live on their own fiber: this row sits after the early "tool not active"
		// return below, and calling a hook-bearing function directly from there would make this render's
		// hook count vary with that condition (React error #300, "rendered fewer hooks than expected").
		function GridOffsetRow(id: string, labelKey: string, tooltipKey: string, value: number, min: number, onCommit: (value: number) => void): JSX.Element {
			return <div className={classNames(InfoRowSCSS.infoRow, styles.gridRow)}>
				<StepInput
					id={id}
					value={value}
					min={min}
					onCommit={onCommit}
					label={translate(labelKey)}
					tooltip={translate(tooltipKey)}
					layout="inline"
				/>
			</div>;
		}

		// This defines aspects of the components.
		const { children, ...otherProps } = props || {};

		var result: JSX.Element = Component();

		if (activeTool.id !== kTransformGizmoToolId) return result;

		result.props.children?.unshift(
			<>
			<Section
				title={translate("Tool.TransformGizmoTool.Modes", "Modes")}
			>
				<Tooltip tooltip={translate("Tool.TransformGizmoTool.Default.Tooltip", "Tool.TransformGizmoTool.Default.Tooltip")}>
					<ValueToolButton<number>
						focusKey={FOCUS_DISABLED$}
						value={Mode.Default}
						selected={currentMode === Mode.Default}
						onSelect={(v) => setMode(v)}
						src="coui://extradetailingtools/Icons/TransformGizmosTool/Default.svg"
					/>
				</Tooltip>

				<Tooltip tooltip={translate("Tool.TransformGizmoTool.Move.Tooltip", "Tool.TransformGizmoTool.Move.Tooltip")}>
					<ValueToolButton<number>
						focusKey={FOCUS_DISABLED$}
						value={Mode.Move}
						selected={currentMode === Mode.Move}
						onSelect={(v) => setMode(v)}
						src="coui://extradetailingtools/Icons/TransformGizmosTool/Move.svg"
					/>
				</Tooltip>

				<Tooltip tooltip={translate("Tool.TransformGizmoTool.Rotate.Tooltip", "Tool.TransformGizmoTool.Rotate.Tooltip")}>
					<ValueToolButton<number>
						focusKey={FOCUS_DISABLED$}
						value={Mode.Rotate}
						selected={currentMode === Mode.Rotate}
						onSelect={(v) => setMode(v)}
						src="coui://extradetailingtools/Icons/TransformGizmosTool/Rotate.svg"
					/>
				</Tooltip>

				{/* <ValueToolButton<number>
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

				<AnarchyButtons />

			</Section>

			{ currentMode === Mode.Default &&
				<Section
					title={translate("Tool.TransformGizmoTool.RaycastFilter", "Raycast Filter")}
				>
					<ToolButton
						focusKey={FOCUS_DISABLED$}
						tooltip={translate("Tool.TransformGizmoTool.RaycastFilter.StaticObject.Tooltip", "Static Objects")}
						src="Media/Game/Icons/Props.svg"
						selected={!hasFlag(RaycastFilter.StaticObject)}
						onSelect={() => toggleFlag(RaycastFilter.StaticObject)}
					/>

					<ToolButton
						focusKey={FOCUS_DISABLED$}
						tooltip={translate("Tool.TransformGizmoTool.RaycastFilter.Decals.Tooltip", "Decals")}
						src="Media/Game/Icons/PropsDecals.svg"
						selected={!hasFlag(RaycastFilter.StaticObject) && !hasFlag(RaycastFilter.Decals)}
						disabled={hasFlag(RaycastFilter.StaticObject)}
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
						selected={!hasFlag(RaycastFilter.MovingObject)}
						onSelect={() => toggleFlag(RaycastFilter.MovingObject)}
					/>
				</Section>
			}

			{ currentMode === Mode.Move &&
				<>
					<Section
						title={translate("Tool.TransformGizmoTool.XZHandleModes", "Sphere Handle Modes")}
					>
						<Tooltip tooltip={translate("Tool.TransformGizmoTool.XZHandleModes.FollowSurface.Tooltip", "Follow Surface")}>
							<ValueToolButton<number>
								focusKey={FOCUS_DISABLED$}
								value={XZHandleMode.FollowSurface}
								selected={currentXZHandleMode === XZHandleMode.FollowSurface}
								onSelect={(v) => SetXZHandleMode(v)}
								src="Media/Tools/Snap Options/ObjectSurface.svg"
							/>
						</Tooltip>

						<Tooltip tooltip={translate("Tool.TransformGizmoTool.XZHandleModes.FixedX.Tooltip", "Fixed X")}>
							<ValueToolButton<number>
								focusKey={FOCUS_DISABLED$}
								value={XZHandleMode.FixedX}
								selected={currentXZHandleMode === XZHandleMode.FixedX}
								onSelect={(v) => SetXZHandleMode(v)}
								src="coui://extradetailingtools/Icons/TransformGizmosTool/FixedX.svg"
							/>
						</Tooltip>

						<Tooltip tooltip={translate("Tool.TransformGizmoTool.XZHandleModes.FixedY.Tooltip", "Fixed Y")}>
							<ValueToolButton<number>
								focusKey={FOCUS_DISABLED$}
								value={XZHandleMode.FixedY}
								selected={currentXZHandleMode === XZHandleMode.FixedY}
								onSelect={(v) => SetXZHandleMode(v)}
								src="coui://extradetailingtools/Icons/TransformGizmosTool/FixedY.svg"
							/>
						</Tooltip>

						<Tooltip tooltip={translate("Tool.TransformGizmoTool.XZHandleModes.FixedZ.Tooltip", "Fixed Z")}>
							<ValueToolButton<number>
								focusKey={FOCUS_DISABLED$}
								value={XZHandleMode.FixedZ}
								selected={currentXZHandleMode === XZHandleMode.FixedZ}
								onSelect={(v) => SetXZHandleMode(v)}
								src="coui://extradetailingtools/Icons/TransformGizmosTool/FixedZ.svg"
							/>
						</Tooltip>
					</Section>
					{ currentXZHandleMode === XZHandleMode.FollowSurface &&
						<Section
							title={translate("Tool.TransformGizmoTool.RaycastFilter", "Raycast Filter")}
						>
							<ToolButton
								focusKey={FOCUS_DISABLED$}
								tooltip={translate("Tool.TransformGizmoTool.RaycastFilter.StaticObject.Tooltip", "Static Objects")}
								src="Media/Game/Icons/Props.svg"
								selected={!hasFlag(RaycastFilter.StaticObject)}
								onSelect={() => toggleFlag(RaycastFilter.StaticObject)}
							/>

							{/* <ToolButton
								focusKey={FOCUS_DISABLED$}
								tooltip={translate("Tool.TransformGizmoTool.RaycastFilter.Decals.Tooltip", "Decals")}
								src="Media/Game/Icons/PropsDecals.svg"
								selected={!hasFlag(RaycastFilter.StaticObject) && !hasFlag(RaycastFilter.Decals)}
								disabled={hasFlag(RaycastFilter.StaticObject)}
								onSelect={() => toggleFlag(RaycastFilter.Decals)}
							/> */}

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
								selected={!hasFlag(RaycastFilter.MovingObject)}
								onSelect={() => toggleFlag(RaycastFilter.MovingObject)}
							/>

							<ToolButton
								focusKey={FOCUS_DISABLED$}
								tooltip={translate("Tool.TransformGizmoTool.RaycastFilter.Net.Tooltip", "Networks")}
								src="Media/Game/Icons/Roads.svg"
								selected={!hasFlag(RaycastFilter.Net)}
								onSelect={() => toggleFlag(RaycastFilter.Net)}
							/>

							<ToolButton
								focusKey={FOCUS_DISABLED$}
								tooltip={translate("Tool.TransformGizmoTool.RaycastFilter.Water.Tooltip", "Water")}
								src="Media/Game/Icons/Water.svg"
								selected={!hasFlag(RaycastFilter.Water)}
								onSelect={() => toggleFlag(RaycastFilter.Water)}
							/>
						</Section>
					}
				</>

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

				<ToolButton
					focusKey={FOCUS_DISABLED$}
					tooltip={translate("Tool.TransformGizmoTool.Duplicate.tooltip")}
					src="coui://extralib/Icons/Misc/Copy.svg"
					onSelect={() => Duplicate()}
				/>
			</Section>

			<InfoSectionFoldout
				header={
					<div className={InfoRowSCSS.infoRow} style={{ paddingLeft: "0rem" }}>
						<div className={classNames(MouseToolOptionsSCSS.label, InfoRowSCSS.left)} style={{ flexGrow: 1 }}>
							{translate("Tool.TransformGizmoTool.Grid", "Grid")}
						</div>
						<Tooltip tooltip={translate("Tool.TransformGizmoTool.GridEnable.Tooltip", "Snap the object's position and rotation to a fixed grid.")} className={InfoRowSCSS.right}>
							<ToolButton
								selected={gridEnabled}
								onSelect={() => SetGridEnabled(!gridEnabled)}
								src="Media/Tools/Snap Options/ZoneGrid.svg"
							/>
						</Tooltip>
					</div>
				}
				initialExpanded={false}
				expandFromContent={false}
				focusKey={FOCUS_AUTO}
				className={classNames(MouseToolOptionsSCSS.item, styles.gridSection)}
			>
				<div className={classNames(InfoSectionSCSS.content, InfoSectionSCSS.disableFocusHighlight, styles.gridContent)}>
					{GridOffsetRow("GridPosOffset", "Tool.TransformGizmoTool.Grid.PosOffset", "Tool.TransformGizmoTool.Grid.PosOffset.Tooltip", posOffset, 0.001, SetPosOffset)}
					{GridOffsetRow("GridRotOffset", "Tool.TransformGizmoTool.Grid.RotOffset", "Tool.TransformGizmoTool.Grid.RotOffset.Tooltip", rotOffset, 0.001, SetRotOffset)}
				</div>
			</InfoSectionFoldout>


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