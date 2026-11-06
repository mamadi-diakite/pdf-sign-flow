export interface CurrentUser {
  readonly id: string;
  readonly email: string;
  readonly displayName: string;
}

export interface LoginRequest {
  readonly email: string;
  readonly password: string;
}

export interface RegisterRequest {
  readonly displayName: string;
  readonly email: string;
  readonly password: string;
  readonly confirmPassword: string;
}

export interface ChangePasswordRequest {
  readonly currentPassword: string;
  readonly newPassword: string;
  readonly confirmPassword: string;
}

export interface LoginResponse {
  readonly accessToken: string;
  readonly expiresAtUtc: string;
  readonly user: CurrentUser;
}
