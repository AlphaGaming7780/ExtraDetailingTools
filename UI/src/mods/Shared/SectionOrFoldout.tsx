import { Fragment, ReactNode, isValidElement } from "react";
import { Section } from "../../../game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options";
import { SectionFoldout, SectionFoldoutProps } from "./SectionFoldout";

// Whether the children render nothing (falsy or empty Fragment, recursively).
function isEmptyChildren(node: ReactNode): boolean {
    if (node === null || node === undefined || node === false || node === true || node === "") {
        return true;
    }
    if (Array.isArray(node)) {
        return node.every(isEmptyChildren);
    }
    if (isValidElement(node) && node.type === Fragment) {
        return isEmptyChildren((node.props as { children?: ReactNode }).children);
    }
    return false;
}

export interface SectionOrFoldoutProps extends Omit<SectionFoldoutProps, "title"> {
    title: string | null;
}

// Plain Section when empty, SectionFoldout otherwise.
export function SectionOrFoldout(props: SectionOrFoldoutProps): JSX.Element {
    const { title, children, headerRight } = props;

    if (isEmptyChildren(children)) {
        return <Section title={title}>{headerRight as string | JSX.Element | JSX.Element[]}</Section>;
    }

    return <SectionFoldout {...props} />;
}
