import { ExtraPanelType } from "../ExtraPanelType";


export const BetterInfoPanel = (ComponentList: { [x: string]: any; }): any => {

    return ComponentList["ExtraDetailingTools.Systems.UI.BetterInfoPanel.BetterInfoPanelUISystem"] = (extraPanel: ExtraPanelType) => {

        return <div style={{ width: "1000rem" }}>
            Hello World
        </div>

    }
}