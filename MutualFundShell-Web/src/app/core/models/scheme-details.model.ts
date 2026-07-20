export interface PeriodReturnDto {
    label: string;
    startNAV: number;
    endNAV: number;
    startDate: string;
    returnPercent: number;
    returnPoints: number;
    isPositive: boolean;
    hasData: boolean;
}

export interface NavPointDto {
    date: string;
    nav: number;
    dateText: string;
}

export interface SchemeDetailsDto {
    // Identity
    schemeCode: string;
    schemeName: string;
    fundCode: string;
    fundName: string;
    isApproved: boolean;

    // Current NAV
    currentNAV: number;
    currentNavDate: string;
    currentNavDateText: string;

    // Previous NAV
    previousNAV: number;
    previousNavDate: string;
    previousNavDateText: string;

    // Daily change
    dailyChange: number;
    dailyChangePercent: number;
    isDailyUp: boolean;

    // This week
    weekStartNAV: number | null;
    weekStartDate: string | null;
    weekStartDateText: string;
    weekReturn: number | null;
    weekReturnPoints: number | null;
    isWeekUp: boolean;

    // Period returns
    oneMonth: PeriodReturnDto | null;
    threeMonth: PeriodReturnDto | null;
    sixMonth: PeriodReturnDto | null;
    oneYear: PeriodReturnDto | null;
    threeYear: PeriodReturnDto | null;

    // Sparkline
    navHistory: NavPointDto[];
}