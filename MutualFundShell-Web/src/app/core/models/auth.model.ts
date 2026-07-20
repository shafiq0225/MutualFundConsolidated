export interface LoginDto {
  email: string;
  password: string;
}

export interface TokenResponseDto {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  tokenType: string;
}

export interface DecodedTokenClaims {
  sub: string;      // PAN / UserId
  role: string;
  firstName?: string;
  lastName?: string;
  exp: number;
  permissions?: string | string[];
}
