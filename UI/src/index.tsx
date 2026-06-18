import { ModRegistrar, ModuleRegistry } from "cs2/modding";
import { HelloWorldComponent } from "mods/hello-world";
import { TransformSection } from "./mods/TransformSection/TransformSection";
import { ToolOption } from "./mods/ToolOption";
import { GrassToolUI } from "./mods/GrassToolUI";
import { TransformGizmosToolButton, TransformGizmoTool } from "mods/TransformGizmosTool/TransformGizmoTool";
import { TransformExtraPanel } from "mods/TransformExtraPanel/TransformExtraPanel";
import { RegisterTransformPanel } from "mods/TransformPanel/RegisterTransformPanel";

export var registry: ModuleRegistry;

const register: ModRegistrar = (moduleRegistry) => {

    console.log("Extra Detailing Tools UI Mod loading...");
    registry = moduleRegistry;
    moduleRegistry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx", 'selectedInfoSectionComponents', TransformSection)
    moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', ToolOption);
    moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', TransformGizmoTool);
    moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', GrassToolUI);

    moduleRegistry.append('UniversalModMenu', TransformGizmosToolButton);
    // moduleRegistry.append('Menu', HelloWorldComponent);
    moduleRegistry.append('Game', RegisterTransformPanel);
    moduleRegistry.append('Editor', RegisterTransformPanel);
    console.log("Extra Detailing Tools UI Mod loaded.");
}

export default register;