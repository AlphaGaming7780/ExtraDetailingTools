import classNames from "classnames";
import { bindValue, trigger, useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import { Tooltip } from "cs2/ui";
import { ChangeEvent, ChangeEventHandler, WheelEvent, WheelEventHandler, useEffect, useState } from "react";
import { EditorItemSCSS } from "../../../game-ui/editor/widgets/item/editor-item.module.scss";
import { ActionButtonSCSS } from "../../../game-ui/game/components/selected-info-panel/selected-info-sections/shared-sections/actions-section/action-button.module.scss";
import { InfoRowSCSS } from "../../../game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss";
import { ToolButton } from "../../../game-ui/game/components/tool-options/tool-button/tool-button";
import TransformPanelSCSS from "./TransformPanel.module.scss";

export interface Float3 {
	x: number,
	y: number,
	z: number
}

export const kTransformPanel$ = "TransformPanel";

export const pos$ = bindValue<Float3>("EDT", 'TransformPanel.pos');
export const rot$ = bindValue<Float3>("EDT", 'TransformPanel.rot');
export const scale$ = bindValue<Float3>("EDT", 'TransformPanel.scale');
export const incPos$ = bindValue<number>("EDT", 'TransformPanel.incpos');
export const incRot$ = bindValue<number>("EDT", 'TransformPanel.incrot');
export const incScale$ = bindValue<number>("EDT", 'TransformPanel.incscale');
export const localAxis$ = bindValue<boolean>("EDT", 'TransformGizmoTool.LocalAxis');
export const moveSubBuildings$ = bindValue<boolean>("EDT", 'TransformGizmoTool.MoveSubBuildings');

export const canPastPos$ = bindValue<boolean>("EDT", "TransformPanel.canpastpos");
export const canPastRot$ = bindValue<boolean>("EDT", "TransformPanel.canpastrot");
export const canPastScale$ = bindValue<boolean>("EDT", "TransformPanel.canpastscale");

export const asSubBuilding$ = bindValue<boolean>("EDT", "TransformPanel.assubbuilding");
export const allowScaling$ = bindValue<boolean>("EDT", "TransformPanel.allowscaling");

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
		tooltip: translate("SelectedInfoPanel.TRANSFORMTOOL.MoveSubBuildings.tooltip"),
		src: moveSubBuildings ? "coui://extradetailingtools/Icons/TransformGizmosTool/Building_V.svg" : "coui://extradetailingtools/Icons/TransformGizmosTool/Building_X.svg",
		onSelect: () => { trigger("EDT", "TransformGizmoTool.MoveSubBuildings", !moveSubBuildings) }
	})

	function OnChange(event: ChangeEvent<HTMLInputElement>) {

		let number = parseFloat(event.target.value);
		if (Number.isNaN(number)) return;
		switch (event.target.id) {
			case "POSI": PositionIncrement = number; triggerIncPos(); break;
			case "POSX": triggerPos(number - pos.x, 0, 0); break;
			case "POSY": triggerPos(0, number - pos.y, 0); break;
			case "POSZ": triggerPos(0, 0, number - pos.z); break;
			case "ROTI": RotationIncrement = number; triggerIncRot(); break;
			case "ROTX": triggerRot(number - rot.x, 0, 0); break;
			case "ROTY": triggerRot(0, number - rot.y, 0); break;
			case "ROTZ": triggerRot(0, 0, number - rot.z); break;
			case "SCALEI": ScaleIncrement = number; triggerIncScale(); break;
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
			case "POSI":
				if (event.deltaY < 0) {
					if (parseFloat(event.target.value) >= 1) { event.target.value = (parseFloat(event.target.value) + 1).toString() }
					else { event.target.value = (parseFloat(event.target.value) * 10).toString() }
				} else {
					if (parseFloat(event.target.value) > 1) { event.target.value = (parseFloat(event.target.value) - 1).toString() }
					else if (parseFloat(event.target.value) > 0.001) { event.target.value = (parseFloat(event.target.value) / 10).toString() }
				}
				PositionIncrement = parseFloat(event.target.value)
				triggerIncPos();
				break;
			case "POSX": triggerPos(posValue,0,0); break;
			case "POSY": triggerPos(0, posValue,0); break;
			case "POSZ": triggerPos(0, 0, posValue); break;

			case "ROTI":
				if (event.deltaY < 0) {
					if (parseFloat(event.target.value) >= 1) { event.target.value = (parseFloat(event.target.value) + 1).toString() }
					else { event.target.value = (parseFloat(event.target.value) * 10).toString() }
				} else {
					if (parseFloat(event.target.value) > 1) { event.target.value = (parseFloat(event.target.value) - 1).toString() }
					else if (parseFloat(event.target.value) > 0.001) { event.target.value = (parseFloat(event.target.value) / 10).toString() }
				}
				RotationIncrement = parseFloat(event.target.value)
				triggerIncRot();
				break;
			case "ROTX": triggerRot(rotValue , 0 ,0); break;
			case "ROTY": triggerRot(0, rotValue, 0); break;
			case "ROTZ": triggerRot(0, 0, rotValue); break;

			case "SCALEI":
				if (event.deltaY < 0) {
					if (parseFloat(event.target.value) >= 1) { event.target.value = (parseFloat(event.target.value) + 1).toString() }
					else { event.target.value = (parseFloat(event.target.value) * 10).toString() }
				} else {
					if (parseFloat(event.target.value) > 1) { event.target.value = (parseFloat(event.target.value) - 1).toString() }
					else if (parseFloat(event.target.value) > 0.001) { event.target.value = (parseFloat(event.target.value) / 10).toString() }
				}
				ScaleIncrement = parseFloat(event.target.value)
				triggerIncScale();
				break;
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

	function triggerRot(x: number, y: number, z: number) {
		let flaot: Float3 = { x, y, z }
		trigger("EDT", "TransformPanel.rot", flaot)
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
			<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.COPY_${id}`)}>
				<button className={classNames(ActionButtonSCSS.button)} onClick={() => { triggerCopy(id) }}>
					<img className={classNames(ActionButtonSCSS.icon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
				</button>
			</Tooltip>
		</>
	}

	function PastButton(id: string, axis: string = "all", canPast: boolean = true): JSX.Element {
		return canPast ? <>
			<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.PAST_${id}_${axis}`)}>
				<button className={classNames(ActionButtonSCSS.button)} onClick={() => { triggerPast(id, axis)}}>
					<img className={classNames(ActionButtonSCSS.icon)} src="coui://extralib/Icons/Misc/Past.svg"></img>
				</button>
			</Tooltip>
		</> : <></>
	}

	function Inputs(id: string, inputValue: Float3, useIncrement: Boolean, increment: number = 0, canPast: boolean = true): JSX.Element {

		const [X, setX] = useState(inputValue.x.toString())
		const [Y, setY] = useState(inputValue.y.toString())
		const [Z, setZ] = useState(inputValue.z.toString())
		const [incrementValue, setIncrementValue] = useState(increment)

		function UpdateValue(event: ChangeEvent<HTMLInputElement>, fucntion : any)
		{
			let number = parseFloat(event.target.value);
			let newValue: string = ""
			if (!Number.isNaN(number)) newValue = number.toString();
			fucntion(newValue)
		}

		useEffect(() => {
			setX(inputValue.x.toString())
			setY(inputValue.y.toString())
			setZ(inputValue.z.toString())
			setIncrementValue(increment)
		}, [inputValue, increment])

		return <>
			<div className={classNames(InfoRowSCSS.right, TransformPanelSCSS.TransfromSectionInputs)}>
				{useIncrement ?
					<>
						<span>↕</span>
						<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.${id}_I`)}>
							<div>
								<span>{translate(`SelectedInfoPanel.TRANSFORMTOOL.step`)}</span>
								<span>
									<input id={`${id}I`} value={incrementValue} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={(event) => { UpdateValue(event, setIncrementValue); OnChange(event); }} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
								</span>
							</div>
						</Tooltip>
					</> : <></>
				}
				{canPast ? PastButton(id, "X", canPast) : <></>}
				<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.${id}_X`)}>
					<div>
						<span>X</span>
						<span>
							<input id={`${id}X`} value={X} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={(event) => { UpdateValue(event, setX); OnChange(event); }} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
						</span>
					</div>
				</Tooltip>
				{canPast ? PastButton(id, "Y", canPast) : <></>}
				<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.${id}_Y`)}>
					<div>
						<span>Y</span>
						<span>
							<input id={`${id}Y`} value={Y} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={(event) => { UpdateValue(event, setY); OnChange(event); }} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
						</span>
					</div>
				</Tooltip>
				{canPast ? PastButton(id, "Z", canPast) : <></>}
				<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.${id}_Z`)}>
					<div>
						<span>Z</span>
						<span>
							<input id={`${id}Z`} value={Z} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={(event) => { UpdateValue(event, setZ); OnChange(event); }} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
						</span>
					</div>
				</Tooltip>
			</div>
		</>;
	}

	return <>
		<div className={classNames(InfoRowSCSS.infoRow, InfoRowSCSS.subRow, InfoRowSCSS.link, TransformPanelSCSS.TransfromSection)} >
			<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.link)} style={{ width: "100%" }}>

				{translate("PhotoMode.PROPERTY_TITLE[Position]")}
				<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.COPY_POS")}>
					<button className={classNames(ActionButtonSCSS.button, TransformPanelSCSS.TransfromSectionTinyButton)} onClick={() => { triggerCopy("POS") }}>
						<img className={classNames(ActionButtonSCSS.icon, TransformPanelSCSS.TransfromSectionButtonTinyIcon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
					</button>
				</Tooltip>
				{
					canPastPos ?
						<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.PAST_POS")}>
							<button className={classNames(ActionButtonSCSS.button, TransformPanelSCSS.TransfromSectionTinyButton)} onClick={() => { triggerPast("POS") }}>
								<img className={classNames(ActionButtonSCSS.icon, TransformPanelSCSS.TransfromSectionButtonTinyIcon)} src="coui://extralib/Icons/Misc/Past.svg"></img>
							</button>
						</Tooltip>
						: <></>
				}
				<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.LOCALAXIS")}>
					<button className={classNames({ [TransformPanelSCSS.TransfromSectionButtonSelected]: localAxis }, ActionButtonSCSS.button, TransformPanelSCSS.TransfromSectionTinyButton)} onClick={() => { trigger("EDT", "TransformGizmoTool.LocalAxis", !localAxis) }}>
						<img className={classNames(ActionButtonSCSS.icon, TransformPanelSCSS.TransfromSectionButtonTinyIcon)} src="coui://extradetailingtools/Icons/TransformGizmosTool/Axis.svg"></img>
					</button>
				</Tooltip>

				<div className={classNames(TransformPanelSCSS.TransformSectionToolOption, InfoRowSCSS.left, InfoRowSCSS.link)}>
					<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.COPY_POS_ROT.tooltip")}>
						<button className={classNames(ActionButtonSCSS.button, TransformPanelSCSS.TransfromSectionTinyButton)} onClick={() => { triggerCopy("POS"); triggerCopy("ROT") }}>
							<img className={classNames(ActionButtonSCSS.icon, TransformPanelSCSS.TransfromSectionButtonTinyIcon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
						</button>
					</Tooltip>
					{canPastPos && canPastRot ?
						<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.PAST_POS_ROT.tooltip")}>
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

			{Inputs("POS", pos, true, PositionIncrement, canPastPos)}

			<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.link)} style={{ width: "100%" }}>
				{translate("PhotoMode.PROPERTY_TITLE[Rotation]")}
				<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.COPY_ROT")}>
					<button className={classNames(ActionButtonSCSS.button, TransformPanelSCSS.TransfromSectionTinyButton)} onClick={() => { triggerCopy("ROT") }}>
						<img className={classNames(ActionButtonSCSS.icon, TransformPanelSCSS.TransfromSectionButtonTinyIcon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
					</button>
				</Tooltip>
				{
					canPastRot ?
						<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.PAST_ROT")}>
							<button className={classNames(ActionButtonSCSS.button, TransformPanelSCSS.TransfromSectionTinyButton)} onClick={() => { triggerPast("ROT") }}>
								<img className={classNames(ActionButtonSCSS.icon, TransformPanelSCSS.TransfromSectionButtonTinyIcon)} src="coui://extralib/Icons/Misc/Past.svg"></img>
							</button>
						</Tooltip>
						: <></>
				}
			</div>

			{Inputs("ROT", rot, true, RotationIncrement, canPastRot)}

			{allowScaling ?
				<>
					<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.link)} style={{ width: "100%" }}>
						{translate("SelectedInfoPanel.TRANSFORMTOOL.SCALE")}
						<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.COPY_SCALE")}>
							<button className={classNames(ActionButtonSCSS.button, TransformPanelSCSS.TransfromSectionTinyButton)} onClick={() => { triggerCopy("SCALE") }}>
								<img className={classNames(ActionButtonSCSS.icon, TransformPanelSCSS.TransfromSectionButtonTinyIcon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
							</button>
						</Tooltip>
						{
							canPastScale ?
								<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.PAST_SCALE")}>
									<button className={classNames(ActionButtonSCSS.button, TransformPanelSCSS.TransfromSectionTinyButton)} onClick={() => { triggerPast("SCALE") }}>
										<img className={classNames(ActionButtonSCSS.icon, TransformPanelSCSS.TransfromSectionButtonTinyIcon)} src="coui://extralib/Icons/Misc/Past.svg"></img>
									</button>
								</Tooltip>
								: <></>
						}
					</div>
					{Inputs("SCALE", scale, true, ScaleIncrement, canPastScale)}
				</>
			: <></>}
		</div>
	</>;
};


export function TransformInputs(translate: any, id: string, inputValue: Float3, useIncrement: Boolean, increment: number = 0, OnChange : ChangeEventHandler = () => {}, OnScroll : WheelEventHandler = () => {} ): JSX.Element
{

	const [X, setX] = useState(inputValue.x.toString())
	const [Y, setY] = useState(inputValue.y.toString())
	const [Z, setZ] = useState(inputValue.z.toString())
	const [incrementValue, setIncrementValue] = useState(increment)

	function UpdateValue(event: ChangeEvent<HTMLInputElement>, fucntion : any)
	{
		let number = parseFloat(event.target.value);
		let newValue: string = ""
		if (!Number.isNaN(number)) newValue = number.toString();
		fucntion(newValue)
	}

	useEffect(() => {
		setX(inputValue.x.toString())
		setY(inputValue.y.toString())
		setZ(inputValue.z.toString())
		setIncrementValue(increment)
	}, [inputValue, increment])

	return <>
		<div className={classNames(InfoRowSCSS.right, TransformPanelSCSS.TransfromSectionInputs)}>
			{useIncrement ?
				<>
					<span>↕</span>
					<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.${id}_I`)}>
						<div>
							<span>{translate(`SelectedInfoPanel.TRANSFORMTOOL.step`)}</span>
							<span>
								<input id={`${id}I`} value={incrementValue} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={(event) => { UpdateValue(event, setIncrementValue); OnChange(event); }} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							</span>
						</div>
					</Tooltip>
				</> : <></>
			}
			<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.${id}_X`)}>
				<div>
					<span>X</span>
					<span>
						<input id={`${id}X`} value={X} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={(event) => { UpdateValue(event, setX); OnChange(event); }} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
					</span>
				</div>
			</Tooltip>
			<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.${id}_Y`)}>
				<div>
					<span>Y</span>
					<span>
						<input id={`${id}Y`} value={Y} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={(event) => { UpdateValue(event, setY); OnChange(event); }} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
					</span>
				</div>
			</Tooltip>
			<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.${id}_Z`)}>
				<div>
					<span>Z</span>
					<span>
						<input id={`${id}Z`} value={Z} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={(event) => { UpdateValue(event, setZ); OnChange(event); }} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
					</span>
				</div>
			</Tooltip>
		</div>
	</>;
}
