import classNames from "classnames";
import { bindValue, trigger, useValue } from "cs2/api";
import { SelectedInfoSectionBase } from "cs2/bindings";
import { useLocalization } from "cs2/l10n";
import { Tooltip } from "cs2/ui";
import { ChangeEvent, MouseEvent, WheelEvent, useEffect, useState } from "react";
import { CollapsiblePanel } from "../../game-ui/common/panel/collapsible-panel";
import { EditorItemSCSS } from "../../game-ui/editor/widgets/item/editor-item.module.scss";
import { ActionButtonSCSS } from "../../game-ui/game/components/selected-info-panel/selected-info-sections/shared-sections/actions-section/action-button.module.scss";
import { InfoRowSCSS } from "../../game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss";
import { InfoSectionSCSS } from "../../game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.module.scss";
import TransfromSectionSCSS from "./Styles/TransformSection.module.scss";
import { PropsToolButton, ToolButton } from "../../game-ui/game/components/tool-options/tool-button/tool-button";
import { PropsSection, Section } from "../../game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options";

export interface Float3 {
	x: number,
	y: number,
	z: number
}

interface TransformSectionProps extends SelectedInfoSectionBase {
	AsSubBuilding?: boolean
    AllowScaling?: boolean
}

export const pos$ = bindValue<Float3>("edt", 'transformsection_pos');
export const rot$ = bindValue<Float3>("edt", 'transformsection_rot');
export const scale$ = bindValue<Float3>("edt", 'transformsection_scale');
export const incPos$ = bindValue<number>("edt", 'transformsection_incpos');
export const incRot$ = bindValue<number>("edt", 'transformsection_incrot');
export const incScale$ = bindValue<number>("edt", 'transformsection_incscale');
export const localAxis$ = bindValue<boolean>("edt", 'transformsection_localaxis');
export const moveSubBuildings$ = bindValue<boolean>("edt", 'transformsection_movesubbuildings');

export const canPastPos$ = bindValue<boolean>("edt", "transformsection_canpastpos");
export const canPastRot$ = bindValue<boolean>("edt", "transformsection_canpastrot");
export const canPastScale$ = bindValue<boolean>("edt", "transformsection_canpastscale");

export const TransformSection = (componentList: {[x: string]: any; }): any => {

	componentList["ExtraDetailingTools.Systems.UI.TransformSection"] = (e: TransformSectionProps) => {
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

		const { translate } = useLocalization();

		console.log(e)

		let PropsToolButton: PropsToolButton = {
			selected: moveSubBuildings,
			tooltip: translate("SelectedInfoPanel.TRANSFORMTOOL.MoveSubBuildings.tooltip"),
			src: "Media/Tools/Snap Options/All.svg",
			onSelect: () => { trigger("edt", "transformsection_movesubbuildings") }
		}

		let MoveSubbuildingsProps: PropsSection = {
			title: translate("SelectedInfoPanel.TRANSFORMTOOL.MoveSubBuildings"), 
			children: ToolButton(PropsToolButton)
		}

		function OnChange(event: ChangeEvent<HTMLInputElement>) {

			let number = parseFloat(event.target.value);
			if (Number.isNaN(number)) return;
			//event.target.value = number.toString();
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
				case "SCALEX": scale.x = number; triggerScale(); break;
				case "SCALEY": scale.y = number; triggerScale(); break;
				case "SCALEZ": scale.z = number; triggerScale(); break;
			}
			trigger("audio", "playSound", "hover-item", 1)

		}

		function OnScroll(event: WheelEvent) {

			if (event.target instanceof HTMLInputElement) {
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
					case "POSX":
						triggerPos(posValue,0,0);
						break;
					case "POSY":
						triggerPos(0, posValue,0);
						break;
					case "POSZ":
						triggerPos(0, 0, posValue);
						break;

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
					case "cX":
						triggerRot(rotValue , 0 ,0)
						break;
					case "ROTY":
						triggerRot(0, rotValue, 0)
						break;
					case "ROTZ":
						triggerRot(0, 0, rotValue)
						break;

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
					case "SCALEX":
						scale.x += scaleVelue;
						triggerScale()
						break;
					case "SCALEY":
						scale.y += scaleVelue;
						triggerScale()
						break;
					case "SCALEZ":
						scale.z += scaleVelue;
						triggerScale()
						break;
				}
				if (event.deltaY < 0) trigger("audio", "playSound", "increase-elevation", 1);
				else trigger("audio", "playSound", "decrease-elevation", 1);
			}
		}

		function triggerPos(x: number, y: number, z: number) {
			let flaot: Float3 = {x,y,z}
			trigger("edt", "transformsection_pos", flaot )
		}

		function triggerRot(x: number, y: number, z: number) {
			let flaot: Float3 = { x, y, z }
			trigger("edt", "transformsection_rot", flaot)
		}
		function triggerIncPos() {
			trigger("edt", "transformsection_incpos", PositionIncrement)
		}

		function triggerIncRot() {
			trigger("edt", "transformsection_incrot", RotationIncrement)
		}

		function triggerScale() {
			trigger("edt", "transformsection_scale", scale)
		}

		function triggerIncScale() {
			trigger("edt", "transformsection_incscale", ScaleIncrement)
		}

		function triggerCopy(id: string) {
			trigger("edt", "transformsection_copy", id)
		}

		function triggerPast(id: string, axis: string = "all") {
			trigger("edt", "transformsection_past", id, axis)
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


		function CopyAndPastButton(id: string, axis: string = "all", canPast: boolean = true) : JSX.Element {
			return <>
				<div>
					{CopyButton(id)}
					{PastButton(id, axis, canPast)}
				</div>
			</>
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

				<div className={classNames(InfoRowSCSS.right, useIncrement ? TransfromSectionSCSS.TransfromSectionInputs : TransfromSectionSCSS.TransfromSectionInputsWithoutIncrement)}>
					<div>
						{useIncrement ?
							<div>
								<span>↕</span>
								<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.${id}_I`)}>
									<input id={`${id}I`} value={incrementValue} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={(event) => { UpdateValue(event, setIncrementValue); OnChange(event); } } onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
								</Tooltip>
							</div> : <></>
						}
						<div>
							<span>X</span>
							<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.${id}_X`)}>
								<input id={`${id}X`} value={X} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={(event) => { UpdateValue(event, setX); OnChange(event); }} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							</Tooltip>
						</div> 
						<div>
							<span>Y</span>
							<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.${id}_Y`)}>
								<input id={`${id}Y`} value={Y} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={(event) => { UpdateValue(event, setY); OnChange(event); }} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							</Tooltip>
						</div>
						<div>
							<span>Z</span>
							<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.${id}_Z`)}>
								<input id={`${id}Z`} value={Z} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={(event) => { UpdateValue(event, setZ); OnChange(event); }} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							</Tooltip>
						</div>
					</div>
					{ useIncrement ?
						<div>
							{useIncrement ? <div></div> : <></>}
							<div>{PastButton(id, "X", canPast)}</div>
							<div>{PastButton(id, "Y", canPast)}</div>
							<div>{PastButton(id, "Z", canPast)}</div>
						</div>
						: <></>
					}
				</div>	

			</>;
		}

		return <>
			<div className={classNames(InfoSectionSCSS.infoSection)} onMouseEnter={(e: MouseEvent) => { trigger("edt", "showhighlight", true) }} onMouseLeave={(e: MouseEvent) => { trigger("edt", "showhighlight", false) } }>
				<div className={classNames(InfoSectionSCSS.content, InfoSectionSCSS.disableFocusHighlight)}>
					<div className={InfoRowSCSS.infoRow}>
						<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.uppercase)}>{e.group}</div>
					</div>
					<div className={classNames(InfoRowSCSS.infoRow, InfoRowSCSS.subRow, InfoRowSCSS.link, TransfromSectionSCSS.TransfromSection)} >

						<CollapsiblePanel expanded={false} className={TransfromSectionSCSS.TransfromSectionCollapsiblePanel } headerText={translate("Editor.POSITION")} >
							<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.link)}>
								<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.COPY_POS")}>
									<button className={classNames(ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { triggerCopy("POS") }}>
										<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
									</button>
								</Tooltip>
								{
									canPastPos ?
										<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.PAST_POS")}>
											<button className={classNames(ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { triggerPast("POS") }}>
												<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="coui://extralib/Icons/Misc/Past.svg"></img>
											</button>
										</Tooltip>
										: <></>
								}
								<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.LOCALAXIS")}>
									<button className={classNames({ [TransfromSectionSCSS.TransfromSectionButtonSelected]: localAxis }, ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { trigger("edt", "transformsection_localaxis") }}>
										<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="Media/Tools/Snap Options/All.svg"></img>
									</button>
								</Tooltip>
							</div>

							{Inputs("POS", pos, true, PositionIncrement, canPastPos)}
							{e.AsSubBuilding ? Section(MoveSubbuildingsProps) : <></> }

						</CollapsiblePanel>

						<CollapsiblePanel expanded={false} className={TransfromSectionSCSS.TransfromSectionCollapsiblePanel} headerText={translate("PhotoMode.PROPERTY_TITLE[Rotation]")} >
							<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.link)} style={{ width: "100%" }}>
								<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.COPY_ROT")}>
									<button className={classNames(ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { triggerCopy("ROT") }}>
										<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
									</button>
								</Tooltip>
								{
									canPastRot ?
										<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.PAST_ROT")}>
											<button className={classNames(ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { triggerPast("ROT") }}>
												<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="coui://extralib/Icons/Misc/Past.svg"></img>
											</button>
										</Tooltip>
										: <></>
								}

							</div>

							{Inputs("ROT", rot, true, RotationIncrement, canPastRot)}
							{e.AsSubBuilding ? Section(MoveSubbuildingsProps) : <></>}

						</CollapsiblePanel>

                        {e.AllowScaling ? 

							<CollapsiblePanel expanded={false} className={TransfromSectionSCSS.TransfromSectionCollapsiblePanel} headerText={translate("SelectedInfoPanel.TRANSFORMTOOL.SCALE")} >

								<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.link)} style={{ width: "100%" }}>
									<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.COPY_SCALE")}>
										<button className={classNames(ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { triggerCopy("SCALE") }}>
											<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
										</button>
									</Tooltip>
									{
										canPastScale ?
											<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.PAST_SCALE")}>
												<button className={classNames(ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { triggerPast("SCALE") }}>
													<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="coui://extralib/Icons/Misc/Past.svg"></img>
												</button>
											</Tooltip>
											: <></>
									}
								</div>
								{Inputs("SCALE", scale, true, ScaleIncrement, canPastScale)}

								</CollapsiblePanel>

							: <></>}

					</div>
				</div>
			</div>
		</>;
	};
	return componentList as any;
};
