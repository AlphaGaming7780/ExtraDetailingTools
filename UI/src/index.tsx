import { ModRegistrar } from "cs2/modding";
import { HelloWorldComponent } from "mods/hello-world";
import { TransformSection } from "./mods/TransformSection";
import { ToolOption } from "./mods/ToolOption";

const register: ModRegistrar = (moduleRegistry) => {

    moduleRegistry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx", 'selectedInfoSectionComponents', TransformSection)
    moduleRegistry.extend("game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", 'MouseToolOptions', ToolOption);

    moduleRegistry.append('Menu', HelloWorldComponent);
}

export default register;