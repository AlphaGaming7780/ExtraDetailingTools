import { ModRegistrar } from "cs2/modding";
import { HelloWorldComponent } from "mods/hello-world";
import { TransformSection } from "./mods/TransformSection/TransformSection";
import { ToolOption } from "./mods/ToolOption";
import { GrassToolUI } from "./mods/GrassToolUI";
import { TransformGizmosToolButton, TransformGizmoTool } from "mods/TransformGizmosTool/TransformGizmoTool";
import { TransformExtraPanel } from "mods/TransformExtraPanel/TransformExtraPanel";

const register: ModRegistrar = (moduleRegistry) => {

    console.log("Extra Detailing Tools UI Mod loading...");
    moduleRegistry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx", 'selectedInfoSectionComponents', TransformSection)
    moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', ToolOption);
    moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', TransformGizmoTool);
    moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', GrassToolUI);

    if(moduleRegistry.registry.has("ExtraLib/ExtraPanels/ExtraPanelsRoot/ExtraPanelsRoot"))
        moduleRegistry.extend("ExtraLib/ExtraPanels/ExtraPanelsRoot/ExtraPanelsRoot", "extraPanelsComponents", TransformExtraPanel)
    else
        console.warn("ExtraPanelsRoot not found, TransformExtraPanel will not be loaded");

    moduleRegistry.append('Menu', HelloWorldComponent);
    moduleRegistry.append('UniversalModMenu', TransformGizmosToolButton);
    console.log("Extra Detailing Tools UI Mod loaded.");
}

export default register;