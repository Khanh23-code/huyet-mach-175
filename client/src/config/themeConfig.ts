import { theme, type ThemeConfig } from 'antd'
import type { ThemeMode } from './themeStore'

const FONT_SANS = "'Be Vietnam Pro', system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif"
const FONT_MONO = 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace'

const LIGHT_PALETTE = {
    primary: '#1A7534',
    primaryHover: '#125A26',
    primarySoft: '#E9F5ED',
    primaryBorder: '#B7DFC2',
    success: '#15803D',
    warning: '#B45309',
    error: '#C4261B',
    info: '#1F5FA6',
    bgLayout: '#F3F7F4',
    bgContainer: '#FFFFFF',
    bgElevated: '#FFFFFF',
    bgMuted: '#F6F9F7',
    bgHover: '#F4FAF6',
    border: '#DCE6DF',
    borderSubtle: '#EAF1EC',
    text: '#0F1F16',
    textSecondary: '#475569',
    textTertiary: '#64748B',
    textPlaceholder: '#94A3B8',
    focusRing: '0 0 0 3px rgba(26, 117, 52, 0.14)',
} as const

const DARK_PALETTE = {
    primary: '#3FAE62',
    primaryHover: '#4CC072',
    primarySoft: 'rgba(63, 174, 98, 0.16)',
    primaryBorder: 'rgba(63, 174, 98, 0.35)',
    success: '#4ADE80',
    warning: '#FBBF24',
    error: '#FF6B5E',
    info: '#60A5FA',
    bgLayout: '#0E1511',
    bgContainer: '#141D18',
    bgElevated: '#1A2620',
    bgMuted: '#19241E',
    bgHover: '#1D2B24',
    border: '#24312A',
    borderSubtle: '#1E2A23',
    text: '#E7EFE9',
    textSecondary: '#B6C5BC',
    textTertiary: '#8FA397',
    textPlaceholder: '#6B7F73',
    focusRing: '0 0 0 3px rgba(63, 174, 98, 0.22)',
} as const

/**
 * Cấu hình tập trung cho Ant Design v6 (<ConfigProvider theme={...}>).
 * Đồng bộ hoàn toàn với hệ biến CSS trong src/styles/variables.css.
 */
export function getAntdTheme(mode: ThemeMode = 'light'): ThemeConfig {
    const isDark = mode === 'dark'
    const p = isDark ? DARK_PALETTE : LIGHT_PALETTE

    return {
        algorithm: isDark ? theme.darkAlgorithm : theme.defaultAlgorithm,
        cssVar: {
            prefix: 'ant',
        },
        token: {
            // Màu thương hiệu & ngữ nghĩa y tế
            colorPrimary: p.primary,
            colorPrimaryHover: p.primaryHover,
            colorSuccess: p.success,
            colorWarning: p.warning,
            colorError: p.error,
            colorInfo: p.info,
            colorLink: p.primary,
            colorLinkHover: p.primaryHover,

            // Nền, bề mặt & viền
            colorBgLayout: p.bgLayout,
            colorBgContainer: p.bgContainer,
            colorBgElevated: p.bgElevated,
            colorFillAlter: p.bgMuted,
            colorBgTextHover: p.bgHover,
            colorBorder: p.border,
            colorBorderSecondary: p.borderSubtle,
            colorSplit: p.borderSubtle,

            // Chữ & tương phản
            colorText: p.text,
            colorTextHeading: p.text,
            colorTextSecondary: p.textSecondary,
            colorTextTertiary: p.textTertiary,
            colorTextDescription: p.textTertiary,
            colorTextPlaceholder: p.textPlaceholder,

            // Tương tác item
            controlItemBgHover: p.bgHover,
            controlItemBgActive: p.primarySoft,
            controlItemBgActiveHover: p.primarySoft,
            controlOutline: p.primarySoft,
            lineWidthFocus: 2,

            // Typography
            fontFamily: FONT_SANS,
            fontFamilyCode: FONT_MONO,
            fontSize: 14,
            fontSizeHeading1: 24,
            fontSizeHeading2: 20,
            fontSizeHeading3: 18,
            fontSizeHeading4: 16,
            fontSizeHeading5: 14,
            fontWeightStrong: 600,

            // Bo góc & kích thước chuẩn
            borderRadius: 8,
            borderRadiusLG: 12,
            borderRadiusSM: 6,
            borderRadiusXS: 4,
            controlHeight: 38,
            controlHeightLG: 42,
            controlHeightSM: 30,

            // Đổ bóng phẳng - sạch sẽ
            boxShadowSecondary: isDark
                ? '0 8px 24px rgba(0, 0, 0, 0.45)'
                : '0 8px 24px rgba(15, 31, 22, 0.08)',
        },
        components: {
            Button: {
                fontWeight: 600,
                borderRadius: 8,
                borderRadiusLG: 8,
                borderRadiusSM: 6,
                controlHeight: 38,
                controlHeightLG: 42,
                controlHeightSM: 30,
                paddingInline: 16,
                paddingInlineLG: 20,
                paddingInlineSM: 12,
                primaryShadow: 'none',
                defaultShadow: 'none',
                dangerShadow: 'none',
                defaultBorderColor: p.border,
                defaultColor: p.text,
                defaultBg: p.bgContainer,
                defaultHoverBg: p.bgHover,
                defaultHoverBorderColor: p.primary,
                defaultHoverColor: p.primary,
            },
            Card: {
                borderRadiusLG: 12,
                headerBg: 'transparent',
                headerFontSize: 16,
                headerFontSizeSM: 14,
                headerHeight: 52,
                headerHeightSM: 42,
                bodyPadding: 20,
                bodyPaddingSM: 14,
                headerPadding: 20,
                headerPaddingSM: 14,
                boxShadowTertiary: 'none',
            },
            Table: {
                headerBg: p.bgMuted,
                headerColor: p.textSecondary,
                headerSplitColor: 'transparent',
                headerBorderRadius: 8,
                rowHoverBg: p.bgHover,
                rowSelectedBg: p.primarySoft,
                rowSelectedHoverBg: p.primarySoft,
                borderColor: p.borderSubtle,
                footerBg: p.bgMuted,
                cellPaddingBlock: 12,
                cellPaddingInline: 16,
                cellPaddingBlockMD: 10,
                cellPaddingInlineMD: 12,
                cellPaddingBlockSM: 8,
                cellPaddingInlineSM: 10,
                cellFontSize: 13,
                cellFontSizeMD: 13,
                cellFontSizeSM: 12,
            },
            Tag: {
                borderRadiusSM: 6,
                fontSizeSM: 12,
                lineHeightSM: 1.5,
                defaultBg: p.bgMuted,
                defaultColor: p.textSecondary,
            },
            Input: {
                borderRadius: 8,
                controlHeight: 38,
                controlHeightLG: 42,
                controlHeightSM: 30,
                hoverBorderColor: p.primary,
                activeBorderColor: p.primary,
                activeShadow: p.focusRing,
                hoverBg: p.bgContainer,
                activeBg: p.bgContainer,
                addonBg: p.bgMuted,
            },
            Select: {
                borderRadius: 8,
                controlHeight: 38,
                controlHeightLG: 42,
                controlHeightSM: 30,
                optionSelectedBg: p.primarySoft,
                optionSelectedColor: p.primary,
                optionSelectedFontWeight: 600,
                optionActiveBg: p.bgHover,
                hoverBorderColor: p.primary,
                activeBorderColor: p.primary,
                activeOutlineColor: p.primarySoft,
            },
            Modal: {
                borderRadiusLG: 16,
                headerBg: p.bgContainer,
                contentBg: p.bgContainer,
                footerBg: p.bgContainer,
                titleFontSize: 18,
                titleLineHeight: 1.35,
                titleColor: p.text,
                paddingMD: 24,
                paddingLG: 24,
            },
            Drawer: {
                footerPaddingBlock: 14,
                footerPaddingInline: 24,
                paddingLG: 24,
            },
            Menu: {
                itemBorderRadius: 8,
                subMenuItemBorderRadius: 8,
                itemHeight: 40,
                itemMarginInline: 8,
                itemColor: p.textSecondary,
                itemHoverBg: p.bgHover,
                itemHoverColor: p.text,
                itemSelectedBg: p.primarySoft,
                itemSelectedColor: p.primary,
                groupTitleColor: p.textTertiary,
                groupTitleFontSize: 11,
                activeBarBorderWidth: 0,
            },
            Tabs: {
                itemColor: p.textSecondary,
                itemHoverColor: p.text,
                itemSelectedColor: p.primary,
                itemActiveColor: p.primaryHover,
                inkBarColor: p.primary,
                cardBg: p.bgMuted,
                titleFontSize: 14,
                horizontalItemPadding: '10px 4px',
            },
            Breadcrumb: {
                itemColor: p.textTertiary,
                linkColor: p.textTertiary,
                linkHoverColor: p.primary,
                lastItemColor: p.text,
                separatorColor: p.textPlaceholder,
                separatorMargin: 8,
            },
            Badge: {
                textFontWeight: 600,
                dotSize: 8,
                statusSize: 8,
            },
            Alert: {
                borderRadius: 12,
                defaultPadding: '12px 16px',
                withDescriptionPadding: '16px 20px',
            },
        },
    }
}
