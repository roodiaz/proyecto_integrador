namespace InvestLab.Models;

public static class MessageCodes
{
    // ── Generic ──────────────────────────────────────────────────────────────
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
    public const string SUCCESS = "SUCCESS";
    public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";

    // ── Auth ─────────────────────────────────────────────────────────────────
    public const string PASSWORDS_DO_NOT_MATCH = "PASSWORDS_DO_NOT_MATCH";
    public const string EMAIL_ALREADY_REGISTERED = "EMAIL_ALREADY_REGISTERED";
    public const string UNVERIFIED_ACCOUNT_EXISTS = "UNVERIFIED_ACCOUNT_EXISTS";
    public const string REGISTRATION_SUCCESS = "REGISTRATION_SUCCESS";
    public const string REGISTRATION_SUCCESS_EMAIL_FAILED = "REGISTRATION_SUCCESS_EMAIL_FAILED";
    public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";
    public const string ACCOUNT_NOT_VERIFIED = "ACCOUNT_NOT_VERIFIED";
    public const string LOGIN_SUCCESS = "LOGIN_SUCCESS";
    public const string INVALID_TOKEN = "INVALID_TOKEN";
    public const string TOKEN_EXPIRED = "TOKEN_EXPIRED";
    public const string LOGOUT_SUCCESS = "LOGOUT_SUCCESS";
    public const string ACCOUNT_ALREADY_VERIFIED = "ACCOUNT_ALREADY_VERIFIED";
    public const string ACCOUNT_VERIFIED = "ACCOUNT_VERIFIED";
    public const string CODE_RESENT = "CODE_RESENT";
    public const string CODE_RESEND_FAILED = "CODE_RESEND_FAILED";
    public const string RECOVERY_EMAIL_SENT = "RECOVERY_EMAIL_SENT";
    public const string PASSWORD_RESET_SUCCESS = "PASSWORD_RESET_SUCCESS";
    public const string ACCOUNT_LOCKED = "ACCOUNT_LOCKED";

    // ── Verification codes ───────────────────────────────────────────────────
    public const string VERIFICATION_CODE_NOT_FOUND = "VERIFICATION_CODE_NOT_FOUND";
    public const string VERIFICATION_CODE_EXPIRED = "VERIFICATION_CODE_EXPIRED";
    public const string VERIFICATION_CODE_ALREADY_USED = "VERIFICATION_CODE_ALREADY_USED";
    public const string VERIFICATION_CODE_INVALID = "VERIFICATION_CODE_INVALID";

    // ── User / Profile ───────────────────────────────────────────────────────
    public const string USER_NOT_FOUND = "USER_NOT_FOUND";
    public const string PROFILE_LOAD_SUCCESS = "PROFILE_LOAD_SUCCESS";
    public const string PROFILE_UPDATE_SUCCESS = "PROFILE_UPDATE_SUCCESS";
    public const string WRONG_CURRENT_PASSWORD = "WRONG_CURRENT_PASSWORD";
    public const string PASSWORD_CHANGE_SUCCESS = "PASSWORD_CHANGE_SUCCESS";
    public const string EMAIL_SAME_AS_CURRENT = "EMAIL_SAME_AS_CURRENT";
    public const string EMAIL_ALREADY_EXISTS = "EMAIL_ALREADY_EXISTS";
    public const string EMAIL_VERIFICATION_SENT = "EMAIL_VERIFICATION_SENT";
    public const string EMAIL_VERIFICATION_SEND_FAILED = "EMAIL_VERIFICATION_SEND_FAILED";
    public const string NO_PENDING_EMAIL_CHANGE = "NO_PENDING_EMAIL_CHANGE";
    public const string EMAIL_CHANGE_SUCCESS = "EMAIL_CHANGE_SUCCESS";
    public const string INVALID_FILE = "INVALID_FILE";
    public const string IMAGE_UPLOAD_SUCCESS = "IMAGE_UPLOAD_SUCCESS";
    public const string ACCOUNT_DELETED = "ACCOUNT_DELETED";
    public const string USER_SETTINGS_NOT_FOUND = "USER_SETTINGS_NOT_FOUND";

    // ── Alerts ───────────────────────────────────────────────────────────────
    public const string MAX_ALERTS_REACHED = "MAX_ALERTS_REACHED";
    public const string ASSET_NOT_FOUND = "ASSET_NOT_FOUND";
    public const string ALERT_CREATED = "ALERT_CREATED";
    public const string ALERT_NOT_FOUND = "ALERT_NOT_FOUND";
    public const string ALERT_DELETED = "ALERT_DELETED";
    public const string ALERT_TOGGLED = "ALERT_TOGGLED";
    public const string ALERT_UPDATED = "ALERT_UPDATED";
    public const string ALERT_DUPLICATE = "ALERT_DUPLICATE";
    public const string ALERT_INVALID_DATA = "ALERT_INVALID_DATA";

    // ── Favorites ────────────────────────────────────────────────────────────
    public const string MAX_FAVORITES_REACHED = "MAX_FAVORITES_REACHED";
    public const string FAVORITE_ALREADY_EXISTS = "FAVORITE_ALREADY_EXISTS";
    public const string FAVORITE_NOT_FOUND = "FAVORITE_NOT_FOUND";
    public const string INVALID_SYMBOL = "INVALID_SYMBOL";

    // ── Portfolio / Transactions ─────────────────────────────────────────────
    public const string SETTINGS_NOT_FOUND = "SETTINGS_NOT_FOUND";
    public const string MAX_DAILY_OPERATIONS_REACHED = "MAX_DAILY_OPERATIONS_REACHED";
    public const string INSUFFICIENT_BALANCE = "INSUFFICIENT_BALANCE";
    public const string PRICE_NOT_AVAILABLE = "PRICE_NOT_AVAILABLE";
    public const string BUY_SUCCESS = "BUY_SUCCESS";
    public const string INSUFFICIENT_SHARES = "INSUFFICIENT_SHARES";
    public const string POSITION_NOT_FOUND = "POSITION_NOT_FOUND";
    public const string SELL_SUCCESS = "SELL_SUCCESS";
    public const string PORTFOLIO_RESET_SUCCESS = "PORTFOLIO_RESET_SUCCESS";
    public const string PORTFOLIO_SETUP_SUCCESS = "PORTFOLIO_SETUP_SUCCESS";
    public const string MAX_PORTFOLIOS_REACHED = "MAX_PORTFOLIOS_REACHED";
    public const string CANNOT_DELETE_LAST_PORTFOLIO = "CANNOT_DELETE_LAST_PORTFOLIO";
    public const string PORTFOLIO_NOT_FOUND = "PORTFOLIO_NOT_FOUND";
    public const string PORTFOLIO_CREATED_SUCCESS = "PORTFOLIO_CREATED_SUCCESS";
    public const string PORTFOLIO_DELETED_SUCCESS = "PORTFOLIO_DELETED_SUCCESS";
    public const string PORTFOLIO_ACTIVATED_SUCCESS = "PORTFOLIO_ACTIVATED_SUCCESS";

    // ── Market ───────────────────────────────────────────────────────────────
    public const string INVALID_SYMBOL_FORMAT = "INVALID_SYMBOL_FORMAT";
    public const string INVALID_RANGE = "INVALID_RANGE";
    public const string MARKET_INDICES_UNAVAILABLE = "MARKET_INDICES_UNAVAILABLE";
    public const string MARKET_OVERVIEW_SUCCESS = "MARKET_OVERVIEW_SUCCESS";
    public const string MARKET_OVERVIEW_ERROR = "MARKET_OVERVIEW_ERROR";

    // ── Contact ──────────────────────────────────────────────────────────────
    public const string CONTACT_MESSAGE_SENT = "CONTACT_MESSAGE_SENT";
    public const string CONTACT_MESSAGE_FAILED = "CONTACT_MESSAGE_FAILED";
}
