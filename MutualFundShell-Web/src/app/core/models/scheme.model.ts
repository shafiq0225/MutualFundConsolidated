export interface SchemeEnrollmentDto {
    id: number;
    schemeCode: string;
    schemeName: string;
    fundName?: string;
    isApproved: boolean;
    createdAt: string;
    updatedAt: string | null;
}

export interface CreateSchemeEnrollmentDto {
    schemeCode: string;
    schemeName: string;
    isApproved: boolean;
}

export interface UpdateSchemeEnrollmentDto {
    schemeName: string;
    isApproved: boolean;
}