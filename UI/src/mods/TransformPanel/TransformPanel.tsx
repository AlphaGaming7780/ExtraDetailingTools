import classNames from "classnames";
import { bindValue, trigger, useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import { Tooltip } from "cs2/ui";
import { ChangeEvent, ChangeEventHandler, MouseEvent as ReactMouseEvent, WheelEvent, WheelEventHandler, useEffect, useState } from "react";
import { EditorItemSCSS } from "../../../game-ui/editor/widgets/item/editor-item.module.scss";
import { ActionButtonSCSS } from "../../../game-ui/game/components/selected-info-panel/selected-info-sections/shared-sections/actions-section/action-button.module.scss";
import { InfoRowSCSS } from "../../../game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss";
import { ToolButton } from "../../../game-ui/game/components/tool-options/tool-button/tool-button";
import TransformPanelSCSS from "./TransformPanel.module.scss";
import { remToPx } from "../RemHelper";
import { StepInput } from "../Shared/StepInput";

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

// A real component (called as <Inputs .../>, not Inputs(...) as a plain function) - it was
// previously the latter, with useState/useEffect calls inside it that, since a plain function call
// doesn't give React its own Fiber/hook list, got silently folded into TransformPanel's own hook
// sequence instead. That's only safe if the exact same set of hooks fires in the exact same order
// on every render of TransformPanel - which held by luck while the SCALE section was permanently
// hidden (allowScaling always false, see TransformPanel.SCALE's own conditional render), but was
// one allowScaling flip away from a Rules-of-Hooks violation (a hook count that changes between
// renders), which React does not fail loudly for - it can silently hand back a stale/wrong value at
// a shifted hook slot, matching an "undefined has no property x" crash with no obvious cause in this
// component's own code. Giving Inputs its own component identity (a stable module-level reference,
// so React doesn't remount it on every parent render either - see why this isn't just inlined back
// into TransformPanel) fixes this structurally, not just for the current always-false case.
function Inputs({
	id, inputValue, useIncrement, increment = 0, canPast = true,
	onCommitValue, onScroll, onTriggerPos, onTriggerRot, onTriggerScale, onIncrementCommit, onPast
}: InputsProps): JSX.Element {
	const { translate } = useLocalization();

	const [X, setX] = useState(inputValue.x.toString())
	const [Y, setY] = useState(inputValue.y.toString())
	const [Z, setZ] = useState(inputValue.z.toString())
	const [editing, setEditing] = useState<string | null>(null)

	useEffect(() => {
		if (editing !== "X") setX(inputValue.x.toString())
		if (editing !== "Y") setY(inputValue.y.toString())
		if (editing !== "Z") setZ(inputValue.z.toString())
	}, [inputValue])

	function onInputChange(event: ChangeEvent<HTMLInputElement>, setter: any) {
		setter(event.target.value)
	}

	function onInputBlur(e: React.FocusEvent<HTMLInputElement>) {
		setEditing(null);
		onCommitValue(e.target.id, e.target.value);
	}

	function onInputKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
		// cohtml doesn't remap e.key per the OS keyboard layout, so "a" only matches on QWERTY. AZERTY
		// swaps A/Q, so also accept "q" for the same physical select-all key.
		if ((e.ctrlKey || e.metaKey) && (e.key.toLowerCase() === "a" || e.key.toLowerCase() === "q")) {
			e.stopPropagation();
			e.currentTarget.select();
			return;
		}
		if (e.key === "Enter") {
			onCommitValue((e.target as HTMLInputElement).id, (e.target as HTMLInputElement).value);
			(e.target as HTMLInputElement).blur();
		}
	}

	function onInputWheel(e: WheelEvent) {
		setEditing(null);
		onScroll(e);
	}

	function onLabelMouseDown(axis: string, e: ReactMouseEvent) {
		e.preventDefault();
		let lastX = e.clientX;
		const startValue = axis === "X" ? inputValue.x : axis === "Y" ? inputValue.y : inputValue.z;
		let accumulatedDelta = 0;
		const pixelsPerStep = remToPx(100);
		let lastSoundTime = 0;
		const soundThrottleMs = 80;

		const onMouseMove = (moveEvent: globalThis.MouseEvent) => {
			const deltaX = moveEvent.clientX - lastX;
			if (deltaX === 0) return;
			lastX = moveEvent.clientX;
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

			const now = Date.now();
			if (now - lastSoundTime > soundThrottleMs) {
				trigger("audio", "playSound", deltaX > 0 ? "increase-elevation" : "decrease-elevation", 1);
				lastSoundTime = now;
			}
		};

		const onMouseUp = () => {
			document.removeEventListener('mousemove', onMouseMove);
			document.removeEventListener('mouseup', onMouseUp);
			document.body.style.cursor = '';
		};

		document.body.style.cursor = 'url(cursor://horizontal-can-resize)';
		document.addEventListener('mousemove', onMouseMove);
		document.addEventListener('mouseup', onMouseUp);
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
			{canPast ? PastButton(translate, id, "X", canPast, onPast) : <></>}
			<Tooltip tooltip={translate(`TransformPanel.${id}_X`)}>
				<div>
					<span className={TransformPanelSCSS.draggableLabel} onMouseDown={(e) => onLabelMouseDown("X", e)}>X</span>
					<span>
						<input id={`${id}X`} value={X} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={(event) => onInputChange(event, setX)} onFocus={() => setEditing("X")} onBlur={onInputBlur} onKeyDown={onInputKeyDown} onWheel={onInputWheel} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
					</span>
				</div>
			</Tooltip>
			{canPast ? PastButton(translate, id, "Y", canPast, onPast) : <></>}
			<Tooltip tooltip={translate(`TransformPanel.${id}_Y`)}>
				<div>
					<span className={TransformPanelSCSS.draggableLabel} onMouseDown={(e) => onLabelMouseDown("Y", e)}>Y</span>
					<span>
						<input id={`${id}Y`} value={Y} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={(event) => onInputChange(event, setY)} onFocus={() => setEditing("Y")} onBlur={onInputBlur} onKeyDown={onInputKeyDown} onWheel={onInputWheel} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
					</span>
				</div>
			</Tooltip>
			{canPast ? PastButton(translate, id, "Z", canPast, onPast) : <></>}
			<Tooltip tooltip={translate(`TransformPanel.${id}_Z`)}>
				<div>
					<span className={TransformPanelSCSS.draggableLabel} onMouseDown={(e) => onLabelMouseDown("Z", e)}>Z</span>
					<span>
						<input id={`${id}Z`} value={Z} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={(event) => onInputChange(event, setZ)} onFocus={() => setEditing("Z")} onBlur={onInputBlur} onKeyDown={onInputKeyDown} onWheel={onInputWheel} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
					</span>
				</div>
			</Tooltip>
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