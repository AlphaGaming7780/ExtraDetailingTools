export const CustomizeTabSections = (sections: Set<String>): Set<String> => {

	console.log("CUSTOMIZE_TAB_SECTIONS:", [...sections]);

	sections.add("ExtraDetailingTools.Systems.UI.TransformSection");

	return sections;
};
