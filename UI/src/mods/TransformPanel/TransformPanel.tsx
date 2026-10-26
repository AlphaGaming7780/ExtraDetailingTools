import classNames from "classnames";
import { bindValue, trigger, useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import { Tooltip } from "cs2/ui";
import { MouseEvent as ReactMouseEvent, WheelEvent } from "react";
import { ActionButtonSCSS } from "../../../game-ui/game/components/selected-info-panel/selected-info-sections/shared-sections/actions-section/action-button.module.scss";
import { InfoRowSCSS } from "../../../game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss";
import { ToolButton } from "../../../game-ui/game/components/tool-options/tool-button/tool-button";
import TransformPanelSCSS from "./TransformPanel.module.scss";
import { remToPx } from "../RemHelper";
import { StepInput } from "../Shared/StepInput";
import { NumberInput, startHorizontalDrag } from "../Shared/NumberInput";

export interface Float3 {
	x: number,
	y: number,
	z: number
}

export const kTransformPanel$ = "TransformPanel";

export const pos$ = bindValue<Float3>("EDT", 'TransformPanel.pos', { x: 0, y: 0, z: 0 });
export const rot$ = bindValue<Float3>("EDT", 'TransformPanel.rot', { x: 0, y: 0, z: 0 });
export const scale$ = bindValue<Float3>("EDT", 'TransformPanel.scale', { x: 0, y: 0, z: 0 });
export const incPos$ = bindValue<number>("EDT", 'TransformPanel.incpos', 0);
export const incRot$ = bindValue<number>("EDT", 'TransformPanel.incrot', 0);
export const incScale$ = bindValue<number>("EDT", 'TransformPanel.incscale', 0);
export const localAxis$ = bindValue<boolean>("EDT", 'TransformGizmoTool.LocalAxis', false);
export const moveSubBuildings$ = bindValue<boolean>("EDT", 'TransformGizmoTool.MoveSubBuildings', false);

export const canPastPos$ = bindValue<boolean>("EDT", "TransformPanel.canpastpos", false);
export const canPastRot$ = bindValue<boolean>("EDT", "TransformPanel.canpastrot", false);
export const canPastScale$ = bindValue<boolean>("EDT", "TransformPanel.canpastscale", false);

export const asSubBuilding$ = bindValue<boolean>("EDT", "TransformPanel.assubbuilding", false);
export const allowScaling$ = bindValue<boolean>("EDT", "TransformPanel.allowscaling", false);

function PastButton(
	translate: (id: string, fallback?: string | null) => string | null,
	id: string, axis: string, canPast: boolean, onPast: (id: string, axis: string) => void
): JSX.Element {
	return canPast ? <>
		<Tooltip tooltip={translate(`TransformPanel.PAST_${id}_${axis}`)}>
			<button className={classNames(ActionButtonSCSS.button)} onClick={() => { onPast(id, axis) }}>
				<img className={classNames(ActionButtonSCSS.icon)} src="coui://extralib/Icons/Misc/Past.svg"></img>
			</button>
		</Tooltip>
	</> : <></>
}

interface InputsProps {
	id: string;
	inputValue: Float3;
	useIncrement: boolean;
	increment?: number;
	canPast?: boolean;
	onCommitValue: (inputId: string, value: string) => void;
	onScroll: (e: WheelEvent) => void;
	onTriggerPos: (x: number, y: number, z: number) => void;
	onTriggerRot: (x: number, y: number, z: number) => void;
	onTriggerScale: (scale: Float3) => void;
	onIncrementCommit: (v: number) => void;
	onPast: (id: string, axis: string) => void;
}

function Inputs({
	id, inputValue, useIncrement, increment = 0, canPast = true,
	onCommitValue, onScroll, onTriggerPos, onTriggerRot, onTriggerScale, onIncrementCommit, onPast
}: InputsProps): JSX.Element {
	const { translate } = useLocalization();

	function onLabelMouseDown(axis: string, e: ReactMouseEvent) {
		const startValue = axis === "X" ? inputValue.x : axis === "Y" ? inputValue.y : inputValue.z;
		let accumulatedDelta = 0;
		const pixelsPerStep = remToPx(100);

		startHorizontalDrag(e, (deltaX) => {
			const valueDelta = (deltaX / pixelsPerStep) * increment;
			accumulatedDelta += valueDelta;

			if (id === "SCALE") {
				const newScale: Float3 = { ...inputValue };
				if (axis === "X") newScale.x = startValue + accumulatedDelta;
				else if (axis === "Y") newScale.y = startValue + accumulatedDelta;
				else newScale.z = startValue + accumulatedDelta;
				onTriggerScale(newScale);
			} else {
				const x = axis === "X" ? valueDelta : 0;
				const y = axis === "Y" ? valueDelta : 0;
				const z = axis === "Z" ? valueDelta : 0;
				if (id === "POS") onTriggerPos(x, y, z);
				else if (id === "ROT") onTriggerRot(x, y, z);
			}
			return deltaX;
		});
	}

	// Called as a function: the inputs keep their place in the tree.
	function AxisInput(axis: "X" | "Y" | "Z", value: number): JSX.Element {
		return <>
			{canPast ? PastButton(translate, id, axis, canPast, onPast) : <></>}
			<Tooltip tooltip={translate(`TransformPanel.${id}_${axis}`)}>
				<div>
					<span className={TransformPanelSCSS.draggableLabel} onMouseDown={(e) => onLabelMouseDown(axis, e)}>{axis}</span>
					<span>
						<NumberInput id={`${id}${axis}`} value={value} onCommit={(v) => onCommitValue(`${id}${axis}`, v.toString())} onWheel={onScroll} />
					</span>
				</div>
			</Tooltip>
		</>;
	}

	return <>
		<div className={classNames(InfoRowSCSS.right, TransformPanelSCSS.TransfromSectionInputs)}>
			{useIncrement ?
				<StepInput
					id={`${id}I`}
					value={increment}
					onCommit={onIncrementCommit}
					label={translate(`TransformPanel.step`)}
					tooltip={translate(`TransformPanel.${id}_I`)}
					showHandle
				/>
				: <></>
			}
			{AxisInput("X", inputValue.x)}
			{AxisInput("Y", inputValue.y)}
			{AxisInput("Z", inputValue.z)}
		</div>
	</>;
}

export const TransformPanel = () => {
	const pos: Float3 = useValue(pos$);
	const rot: Float3 = useValue(rot$);
	const scale: Float3 = useValue(scale$);
	var PositionIncrement: number = useValue(incPos$);
	var RotationIncrement: number = useValue(incRot$);
	var ScaleIncrement: number = useValue(incScale$);
	var localAxis: boolean = useValue(localAxis$);
	var moveSubBuildings: boolean = useValue(moveSubBuildings$);

	var canPastPos: boolean = useValue(canPastPos$);
	var canPastRot: boolean = useValue(canPastRot$);
	var canPastScale: boolean = useValue(canPastScale$);

	var asSubBuilding: boolean = useValue(asSubBuilding$);
	var allowScaling: boolean = useValue(allowScaling$);

	const { translate } = useLocalization();

	const MoveSubBuildingsButton = ToolButton({
		selected: moveSubBuildings,
		tooltip: translate("TransformPanel.MoveSubBuildings.tooltip"),
		src: moveSubBuildings ? "coui://extradetailingtools/Icons/TransformGizmosTool/Building_V.svg" : "coui://extradetailingtools/Icons/TransformGizmosTool/Building_X.svg",
		onSelect: () => { trigger("EDT", "TransformGizmoTool.MoveSubBuildings", !moveSubBuildings) }
	})

	function commitValue(inputId: string, value: string) {
		let number = parseFloat(value);
		if (Number.isNaN(number)) return;
		switch (inputId) {
			case "POSX": triggerAbsPos(number, pos.y, pos.z); break;
			case "POSY": triggerAbsPos(pos.x, number, pos.z); break;
			case "POSZ": triggerAbsPos(pos.x, pos.y, number); break;
			case "ROTX": triggerAbsRot(number, rot.y, rot.z); break;
			case "ROTY": triggerAbsRot(rot.x, number, rot.z); break;
			case "ROTZ": triggerAbsRot(rot.x, rot.y, number); break;
			case "SCALEX": scale.x = number; triggerScale(scale); break;
			case "SCALEY": scale.y = number; triggerScale(scale); break;
			case "SCALEZ": scale.z = number; triggerScale(scale); break;
		}
		trigger("audio", "playSound", "hover-item", 1)
	}

	function OnScroll(event: WheelEvent) {

		if (!(event.target instanceof HTMLInputElement)) return;

		event.stopPropagation()
		event.preventDefault();
		let posValue: number = - Math.sign(event.deltaY) * PositionIncrement;
		let rotValue: number = - Math.sign(event.deltaY) * RotationIncrement;
		let scaleVelue: number = - Math.sign(event.deltaY) * ScaleIncrement;
		switch (event.target.id) {
			case "POSX": triggerPos(posValue,0,0); break;
			case "POSY": triggerPos(0, posValue,0); break;
			case "POSZ": triggerPos(0, 0, posValue); break;

			case "ROTX": triggerRot(rotValue , 0 ,0); break;
			case "ROTY": triggerRot(0, rotValue, 0); break;
			case "ROTZ": triggerRot(0, 0, rotValue); break;

			case "SCALEX": scale.x += scaleVelue; triggerScale(scale); break;
			case "SCALEY": scale.y += scaleVelue; triggerScale(scale); break;
			case "SCALEZ": scale.z += scaleVelue; triggerScale(scale); break;
		}
		if (event.deltaY < 0) trigger("audio", "playSound", "increase-elevation", 1);
		else trigger("audio", "playSound", "decrease-elevation", 1);

	}

	function triggerPos(x: number, y: number, z: number) {
		let flaot: Float3 = {x,y,z}
		trigger("EDT", "TransformPanel.pos", flaot )
	}

	function triggerAbsPos(x: number, y: number, z: number) {
		trigger("EDT", "TransformPanel.abspos", { x, y, z } as Float3)
	}

	function triggerRot(x: number, y: number, z: number) {
		let flaot: Float3 = { x, y, z }
		trigger("EDT", "TransformPanel.rot", flaot)
	}

	function triggerAbsRot(x: number, y: number, z: number) {
		trigger("EDT", "TransformPanel.absrot", { x, y, z } as Float3)
	}

	function triggerIncPos() {
		trigger("EDT", "TransformPanel.incpos", PositionIncrement)
	}

	function triggerIncRot() {
		trigger("EDT", "TransformPanel.incrot", RotationIncrement)
	}

	function triggerScale(scale : Float3) {
		trigger("EDT", "TransformPanel.scale", scale)
	}

	function triggerIncScale() {
		trigger("EDT", "TransformPanel.incscale", ScaleIncrement)
	}

	function triggerCopy(id: string) {
		trigger("EDT", "TransformPanel.copy", id)
	}

	function triggerPast(id: string, axis: string = "all") {
		trigger("EDT", "TransformPanel.past", id, axis)
	}

	function CopyButton(id: string): JSX.Element {
		return <>
			<Tooltip tooltip={translate(`TransformPanel.COPY_${id}`)}>
				<button className={classNames(ActionButtonSCSS.button)} onClick={() => { triggerCopy(id) }}>
					<img className={classNames(ActionButtonSCSS.icon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
				</button>
			</Tooltip>
		</>
	}

	return <>
		<div className={classNames(InfoRowSCSS.infoRow, InfoRowSCSS.subRow, InfoRowSCSS.link, TransformPanelSCSS.TransfromSection)} >
			<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.link)} style={{ width: "100%" }}>

				{translate("PhotoMode.PROPERTY_TITLE[Position]")}
				<Tooltip tooltip={translate("TransformPanel.COPY_POS")}>
					<button className={classNames(ActionButtonSCSS.button, TransformPanelSCSS.TransfromSectionTinyButton)} onClick={() => { triggerCopy("POS") }}>
						<img className={classNames(ActionButtonSCSS.icon, TransformPanelSCSS.TransfromSectionButtonTinyIcon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
					</button>
				</Tooltip>
				{
					canPastPos ?
						<Tooltip tooltip={translate("TransformPanel.PAST_POS")}>
							<button className={classNames(ActionButtonSCSS.button, TransformPanelSCSS.TransfromSectionTinyButton)} onClick={() => { triggerPast("POS") }}>
								<img className={classNames(ActionButtonSCSS.icon, TransformPanelSCSS.TransfromSectionButtonTinyIcon)} src="coui://extralib/Icons/Misc/Past.svg"></img>
							</button>
						</Tooltip>
						: <></>
				}
				<Tooltip tooltip={translate("TransformPanel.LOCALAXIS")}>
					<button className={classNames({ [TransformPanelSCSS.TransfromSectionButtonSelected]: localAxis }, ActionButtonSCSS.button, TransformPanelSCSS.TransfromSectionTinyButton)} onClick={() => { trigger("EDT", "TransformGizmoTool.LocalAxis", !localAxis) }}>
						<img className={classNames(ActionButtonSCSS.icon, TransformPanelSCSS.TransfromSectionButtonTinyIcon)} src="coui://extradetailingtools/Icons/TransformGizmosTool/Axis.svg"></img>
					</button>
				</Tooltip>

				<div className={classNames(TransformPanelSCSS.TransformSectionToolOption, InfoRowSCSS.left, InfoRowSCSS.link)}>
					<Tooltip tooltip={translate("TransformPanel.COPY_POS_ROT.tooltip")}>
						<button className={classNames(ActionButtonSCSS.button, TransformPanelSCSS.TransfromSectionTinyButton)} onClick={() => { triggerCopy("POS"); triggerCopy("ROT") }}>
							<img className={classNames(ActionButtonSCSS.icon, TransformPanelSCSS.TransfromSectionButtonTinyIcon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
						</button>
					</Tooltip>
					{canPastPos && canPastRot ?
						<Tooltip tooltip={translate("TransformPanel.PASTE_POS_ROT.tooltip")}>
							<button className={classNames(ActionButtonSCSS.button, TransformPanelSCSS.TransfromSectionTinyButton)} onClick={() => { triggerPast("POS"); triggerPast("ROT") }}>
								<img className={classNames(ActionButtonSCSS.icon, TransformPanelSCSS.TransfromSectionButtonTinyIcon)} src="coui://extralib/Icons/Misc/Past.svg"></img>
							</button>
						</Tooltip>
						: <></>}

					{asSubBuilding ?
						MoveSubBuildingsButton
						: <></>}
				</div>

			</div>

			<Inputs
				id="POS" inputValue={pos} useIncrement increment={PositionIncrement} canPast={canPastPos}
				onCommitValue={commitValue} onScroll={OnScroll}
				onTriggerPos={triggerPos} onTriggerRot={triggerRot} onTriggerScale={triggerScale}
				onIncrementCommit={(v) => { PositionIncrement = v; triggerIncPos(); }}
				onPast={triggerPast}
			/>

			<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.link)} style={{ width: "100%" }}>
				{translate("PhotoMode.PROPERTY_TITLE[Rotation]")}
				<Tooltip tooltip={translate("TransformPanel.COPY_ROT")}>
					<button className={classNames(ActionButtonSCSS.button, TransformPanelSCSS.TransfromSectionTinyButton)} onClick={() => { triggerCopy("ROT") }}>
						<img className={classNames(ActionButtonSCSS.icon, TransformPanelSCSS.TransfromSectionButtonTinyIcon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
					</button>
				</Tooltip>
				{
					canPastRot ?
						<Tooltip tooltip={translate("TransformPanel.PAST_ROT")}>
							<button className={classNames(ActionButtonSCSS.button, TransformPanelSCSS.TransfromSectionTinyButton)} onClick={() => { triggerPast("ROT") }}>
								<img className={classNames(ActionButtonSCSS.icon, TransformPanelSCSS.TransfromSectionButtonTinyIcon)} src="coui://extralib/Icons/Misc/Past.svg"></img>
							</button>
						</Tooltip>
						: <></>
				}
			</div>

			<Inputs
				id="ROT" inputValue={rot} useIncrement increment={RotationIncrement} canPast={canPastRot}
				onCommitValue={commitValue} onScroll={OnScroll}
				onTriggerPos={triggerPos} onTriggerRot={triggerRot} onTriggerScale={triggerScale}
				onIncrementCommit={(v) => { RotationIncrement = v; triggerIncRot(); }}
				onPast={triggerPast}
			/>

			{allowScaling ?
				<>
					<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.link)} style={{ width: "100%" }}>
						{translate("TransformPanel.SCALE")}
						<Tooltip tooltip={translate("TransformPanel.COPY_SCALE")}>
							<button className={classNames(ActionButtonSCSS.button, TransformPanelSCSS.TransfromSectionTinyButton)} onClick={() => { triggerCopy("SCALE") }}>
								<img className={classNames(ActionButtonSCSS.icon, TransformPanelSCSS.TransfromSectionButtonTinyIcon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
							</button>
						</Tooltip>
						{
							canPastScale ?
								<Tooltip tooltip={translate("TransformPanel.PAST_SCALE")}>
									<button className={classNames(ActionButtonSCSS.button, TransformPanelSCSS.TransfromSectionTinyButton)} onClick={() => { triggerPast("SCALE") }}>
										<img className={classNames(ActionButtonSCSS.icon, TransformPanelSCSS.TransfromSectionButtonTinyIcon)} src="coui://extralib/Icons/Misc/Past.svg"></img>
									</button>
								</Tooltip>
								: <></>
						}
					</div>
					<Inputs
						id="SCALE" inputValue={scale} useIncrement increment={ScaleIncrement} canPast={canPastScale}
						onCommitValue={commitValue} onScroll={OnScroll}
						onTriggerPos={triggerPos} onTriggerRot={triggerRot} onTriggerScale={triggerScale}
						onIncrementCommit={(v) => { ScaleIncrement = v; triggerIncScale(); }}
						onPast={triggerPast}
					/>
				</>
			: <></>}
		</div>
	</>;
};