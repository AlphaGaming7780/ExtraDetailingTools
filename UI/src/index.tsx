import { ModRegistrar } from "cs2/modding";
import { HelloWorldComponent } from "mods/hello-world";
import { TransformSection } from "./mods/TransformSection";
import { ToolOption } from "./mods/ToolOption";
import { GrassToolUI } from "./mods/GrassToolUI";
import { TransformGizmosToolButton, TransformGizmoTool } from "mods/TransformGizmosTool/TransformGizmoTool";
import { TransformExtraPanel } from "mods/TransformExtraPanel/TransformExtraPanel";

const register: ModRegistrar = (moduleRegistry) => {

    // moduleRegistry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx", 'selectedInfoSectionComponents', TransformSection)
    moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', ToolOption);
    moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', TransformGizmoTool);
    moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', GrassToolUI);

    // while (!moduleRegistry.registry.has("ExtraLib/ExtraPanels/ExtraPanelsRoot/ExtraPanelsRoot")) {
    //     console.log("Waiting for ExtraPanelsRoot to load...");
    // }

    if(moduleRegistry.registry.has("ExtraLib/ExtraPanels/ExtraPanelsRoot/ExtraPanelsRoot"))
        moduleRegistry.extend("ExtraLib/ExtraPanels/ExtraPanelsRoot/ExtraPanelsRoot", "extraPanelsComponents", TransformExtraPanel)
    else
        console.warn("ExtraPanelsRoot not found, TransformExtraPanel will not be loaded");

    moduleRegistry.append('Menu', HelloWorldComponent);
    moduleRegistry.append('UniversalModMenu', TransformGizmosToolButton);
}

export default register;