import { ExtraPanelType } from "mods/ExtraPanelType";


export const TransformPanel = (ComponentList: { [x: string]: any; }): any => {

    return ComponentList["ExtraDetailingTools.Systems.UI.TransformPanel.TransformExtraPanel"] = (extraPanel: ExtraPanelType) => {

        return <div style={{ width: "1000rem" }}>
            Hello World
        </div>

    }
}