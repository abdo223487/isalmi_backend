namespace IslamiApi.DTOs;

public record RegisterRequest(string Email, string Password, string Name);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
public record ChangePasswordRequest(string OldPassword, string NewPassword);

public record AuthResponse(string AccessToken, string RefreshToken, string Role);

public record UserInfoResponse(int Id, string Email, string Name, string Role, string CreatedAt);
