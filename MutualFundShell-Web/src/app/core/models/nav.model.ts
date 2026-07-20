export interface NavHistoryDto {
    date: string;
    nav: number;
    percentage: string;
    isTradingHoliday: boolean;
    isGrowth: boolean;
}

export interface SchemeComparisonDto {
    fundName: string;
    schemeCode: string;
    schemeName: string;
    history: NavHistoryDto[];
    rank: number;
}

export interface NavComparisonResponseDto {
    startDate: string;
    endDate: string;
    message: string;
    schemes: SchemeComparisonDto[];
}