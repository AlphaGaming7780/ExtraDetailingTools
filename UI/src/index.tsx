import { ModRegistrar } from "cs2/modding";
import { HelloWorldComponent } from "mods/hello-world";
import { TransformSection } from "./mods/TransformSection";
import { ExtraDetailingDetails, ExtraDetailingMenu } from "./mods/ExtraDetailingMenu";

const register: ModRegistrar = (moduleRegistry) => {

    moduleRegistry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx", 'selectedInfoSectionComponents', TransformSection)
    moduleRegistry.extend("game-ui/game/components/asset-menu/asset-category-tab-bar/asset-category-tab-bar.tsx", 'AssetCategoryTabBar', ExtraDetailingMenu)
    //moduleRegistry.extend("game-ui/game/components/asset-menu/asset-detail-panel/asset-detail-panel.tsx", 'AssetDetailPanel', ExtraDetailingDetails)

    moduleRegistry.append('Menu', HelloWorldComponent);
}

export default register;