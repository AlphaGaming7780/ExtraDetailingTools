import { InfoSectionSCSS } from "../../game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.module.scss";
import { InfoRowSCSS } from "../../game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss";
import { EditorItemSCSS } from "../../game-ui/editor/widgets/item/editor-item.module.scss";
import TransfromSectionSCSS from "./Styles/TransformSection.module.scss";
import { trigger, useValue } from "cs2/api";
import { bindValue } from "cs2/api";
import { FormEvent, MouseEvent, ReactElement, WheelEvent } from "react";
import { ActionButtonSCSS } from "../../game-ui/game/components/selected-info-panel/selected-info-sections/shared-sections/actions-section/action-button.module.scss";
import { useLocalization } from "cs2/l10n";
import { Tooltip } from "cs2/ui";
import classNames from "classnames";

export interface Float3 {
	x: number,
	y: number,
	z: number
}


export const pos$ = bindValue<Float3>("edt", 'transformsection_pos');
export const rot$ = bindValue<Float3>("edt", 'transformsection_rot');
export const scale$ = bindValue<Float3>("edt", 'transformsection_scale');
export const incPos$ = bindValue<number>("edt", 'transformsection_incpos');
export const incRot$ = bindValue<number>("edt", 'transformsection_incrot');
export const localAxis$ = bindValue<boolean>("edt", 'transformsection_localaxis');

export const TransformSection = (componentList: any): any => {

	interface InfoSection {
		group: string;
		tooltipKeys: Array<string>;
		tooltipTags: Array<string>;
	}

	componentList["ExtraDetailingTools.TransformSection"] = (e: InfoSection) => {
		const pos: Float3 = useValue(pos$);
		const rot: Float3 = useValue(rot$);
		const scale: Float3 = useValue(scale$);
		var PositionIncrement: number = useValue(incPos$);
		var RotationIncrement: number = useValue(incRot$);
		var localAxis: boolean = useValue(localAxis$);
		const { translate } = useLocalization();

		function OnChange(event: FormEvent<HTMLInputElement>) {
			if (event.target instanceof HTMLInputElement) {
				let number = parseFloat(event.target.value);
				if (Number.isNaN(number)) return;
				event.target.value = number.toString();
				switch (event.target.id) {
					case "POSI": PositionIncrement = number; triggerIncPos(); break;
					//case "pX": pos.x = number; triggerPos(); break;
					//case "pY": pos.y = number; triggerPos(); break;
					//case "pZ": pos.z = number; triggerPos(); break;
					case "ROTI": RotationIncrement = number; triggerIncRot(); break;
					//case "rX": rot.x = number; triggerRot(); break;
					//case "rY": rot.x = number; triggerRot(); break;
					//case "rZ": rot.x = number; triggerRot(); break;
					case "SCALEX": scale.x = number; triggerScale(); break;
					case "SCALEY": scale.y = number; triggerScale(); break;
					case "SCALEZ": scale.z = number, triggerScale(); break;
				}
				trigger("audio", "playSound", "hover-item", 1)
			}
		}

		function OnScroll(event: WheelEvent) {

			if (event.target instanceof HTMLInputElement) {
				let posValue: number = - Math.sign(event.deltaY) * PositionIncrement;
				let rotValue: number = - Math.sign(event.deltaY) * RotationIncrement;
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
					case "ROTX":
						triggerRot(rotValue , 0 ,0)
						break;
					case "ROTY":
						triggerRot(0, rotValue, 0)
						break;
					case "ROTZ":
						triggerRot(0, 0, rotValue)
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

		function CopyButton(id: string, id2: string): JSX.Element {
			return <>
				<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.COPY${id}${id2}`)}>
					<button className={classNames(ActionButtonSCSS.button)} onClick={() => { trigger("edt", `transformsection_copy${id.toLowerCase()}${id2}`) }}>
						<img className={classNames(ActionButtonSCSS.icon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
					</button>
				</Tooltip>
			</>
		}

		function PastButton(id: string, id2: string): JSX.Element {
			return <>
				<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.PAST${id}${id2}`)}>
					<button className={classNames(ActionButtonSCSS.button)} onClick={() => { trigger("edt", `transformsection_past${id.toLowerCase()}${id2}`) }}>
						<img className={classNames(ActionButtonSCSS.icon)} src="coui://extralib/Icons/Misc/Past.svg"></img>
					</button>
				</Tooltip>
			</>
		}


		function CopyAndPastButton(id: string, id2: string) : JSX.Element {
			return <>
				<div>
					{CopyButton(id, id2)}
					{PastButton(id, id2)}
				</div>
			</>
		}

		function Inputs(id: string, inputValue: Float3, useIncrement: Boolean, increment : number = 0): JSX.Element {
			return <>

				<div className={classNames(InfoRowSCSS.right, useIncrement ? TransfromSectionSCSS.TransfromSectionInputs : TransfromSectionSCSS.TransfromSectionInputsWithoutIncrement)}>
					<div>
						{useIncrement ?
							<div>
								<span>↕</span>
								<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.${id}I`)}>
									<input id={`${id}I`} value={increment} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
								</Tooltip>
							</div> : <></>
						}
						<div>
							<span>X</span>
							<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.${id}X`)}>
								<input id={`${id}X`} value={inputValue.x.toString()} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							</Tooltip>
						</div> 
						<div>
							<span>Y</span>
							<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.${id}Y`)}>
								<input id={`${id}Y`} value={inputValue.y.toString()} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							</Tooltip>
						</div>
						<div>
							<span>Z</span>
							<Tooltip tooltip={translate(`SelectedInfoPanel.TRANSFORMTOOL.${id}Z`)}>
								<input id={`${id}Z`} value={inputValue.z.toString()} multiple={false} className={classNames(EditorItemSCSS.input)} onChange={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							</Tooltip>
						</div>
					</div>
					{ useIncrement ?
						<div>
							{useIncrement ? <div></div> : <></>}
							<div>{PastButton(id, "_x")}</div>
							<div>{PastButton(id, "_y")}</div>
							<div>{PastButton(id, "_z")}</div>
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
					<div className={classNames(InfoRowSCSS.infoRow, InfoRowSCSS.subRow, InfoRowSCSS.link)} >


						<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.link)} style={{width:"100%"} }>
							{translate("Editor.POSITION") + " "}
							<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.COPYPOS")}>
								<button className={classNames(ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { trigger("edt", "transformsection_copypos") }}>
									<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
								</button>
							</Tooltip>
							<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.PASTPOS")}>
								<button className={classNames(ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { trigger("edt", "transformsection_pastpos") }}>
									<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="coui://extralib/Icons/Misc/Past.svg"></img>
								</button>
							</Tooltip>
							<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.LOCALAXIS")}>
								<button className={classNames({ [TransfromSectionSCSS.TransfromSectionButtonSelected]: localAxis }, ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { trigger("edt", "transformsection_localaxis") }}>
									<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="Media/Tools/Snap Options/All.svg"></img>
								</button>
							</Tooltip>
						</div>
						{/*<div className={classNames(InfoRowSCSS.right, TransfromSectionSCSS.TransfromSectionInputs)}>*/}
						{/*	<div className= "inputs">*/}
						{/*		↕<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.POSI")}>*/}
						{/*			<input id="pI" type="number" value={PositionIncrement} multiple={false} className={classNames(EditorItemSCSS.input)} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />*/}
						{/*		</Tooltip>*/}
						{/*		X<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.POSX")}>*/}
						{/*			<input id="pX" type="number" value={pos.x.toString()} multiple={false} className={classNames(EditorItemSCSS.input)} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />*/}
						{/*		</Tooltip>*/}
						{/*		Y<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.POSY")}>*/}
						{/*			<input id="pY" type="number" value={pos.y.toString()} multiple={false} className={classNames(EditorItemSCSS.input)} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />*/}
						{/*		</Tooltip>*/}
						{/*		Z<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.POSZ")}>*/}
						{/*			<input id="pZ" type="number" value={pos.z.toString()} multiple={false} className={classNames(EditorItemSCSS.input)} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />*/}
						{/*		</Tooltip>*/}
						{/*	</div>*/}
						{/*</div>*/}

						{Inputs("POS", pos, true, PositionIncrement)}


						<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.link)} style={{width:"100%"} }>
							{translate("PhotoMode.PROPERTY_TITLE[Rotation]") + " "}
							<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.COPYROT")}>
								<button className={classNames(ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { trigger("edt", "transformsection_copyrot") }}>
									<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
								</button>
							</Tooltip>
							<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.PASTROT")}>
								<button className={classNames(ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { trigger("edt", "transformsection_pastrot") }}>
									<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="coui://extralib/Icons/Misc/Past.svg"></img>
								</button>
							</Tooltip>
						</div>

						{Inputs("ROT", rot, true, RotationIncrement) }

						{/*<div className={classNames(InfoRowSCSS.right, TransfromSectionSCSS.TransfromSectionInputs)}>*/}
						{/*	↕<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.ROTI")}>*/}
						{/*		<input id="rI" type="number" value={RotationIncrement} multiple={false} className={classNames(EditorItemSCSS.input)} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />*/}
						{/*	</Tooltip>*/}
						{/*	X<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.ROTX")}>*/}
						{/*		<input id="rX" type="number" value={rot.x.toString()} multiple={false} className={classNames(EditorItemSCSS.input)} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />*/}
						{/*	</Tooltip>*/}
						{/*	Y<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.ROTY")}>*/}
						{/*		<input id="rY" type="number" value={rot.y.toString()} multiple={false} className={classNames(EditorItemSCSS.input)} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />*/}
						{/*	</Tooltip>*/}
						{/*	Z<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.ROTZ")}>*/}
						{/*		<input id="rZ" type="number" value={rot.z.toString()} multiple={false} className={classNames(EditorItemSCSS.input)} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />*/}
						{/*	</Tooltip>*/}
						{/*</div>*/}


						<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.link)} style={{ width: "100%" }}>
							{translate("SelectedInfoPanel.TRANSFORMTOOL.SCALE") + " "}
							{/*<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.COPYROT")}>*/}
							{/*	<button className={classNames(ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { trigger("edt", "transformsection_copyrot") }}>*/}
							{/*		<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>*/}
							{/*	</button>*/}
							{/*</Tooltip>*/}
							{/*<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.PASTROT")}>*/}
							{/*	<button className={classNames(ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { trigger("edt", "transformsection_pastrot") }}>*/}
							{/*		<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="coui://extralib/Icons/Misc/Past.svg"></img>*/}
							{/*	</button>*/}
							{/*</Tooltip>*/}
						</div>
						{/*<div className={classNames(InfoRowSCSS.right, TransfromSectionSCSS.TransfromSectionScaleInputs)}>*/}
						{/*	X<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.SCALEX")}>*/}
						{/*		<input id="sX" type="number" value={scale.x.toString()} multiple={false} className={classNames(EditorItemSCSS.input)} onInput={OnChange} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />*/}
						{/*	</Tooltip>*/}
						{/*	Y<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.SCALEY")}>*/}
						{/*		<input id="sY" type="number" value={scale.y.toString()} multiple={false} className={classNames(EditorItemSCSS.input)} onInput={OnChange} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />*/}
						{/*	</Tooltip>*/}
						{/*	Z<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.SCALEZ")}>*/}
						{/*		<input id="sZ" type="number" value={scale.z.toString()} multiple={false} className={classNames(EditorItemSCSS.input)} onInput={OnChange} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />*/}
						{/*	</Tooltip>*/}
						{/*</div>*/}

						{Inputs("SCALE", scale, false) }

					</div>
				</div>
			</div>
		</>;
	};
	return componentList as any;
};
