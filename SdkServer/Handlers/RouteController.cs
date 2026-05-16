using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MikuSB.Configuration;
using MikuSB.Database.Account;
using MikuSB.SdkServer.Models;
using MikuSB.Util;
using System.Text;
using System.Text.Json;

namespace MikuSB.SdkServer.Handlers;

[ApiController]
public class RouteController : ControllerBase
{
    public static ConfigContainer Config = ConfigManager.Config;

    public static object BuildServerList(string version = "")
    {
        return new
        {
            code = 0,
            ret = 0,
            msg = "ok",
            message = "ok",
            version,
            server_time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            servers = new[]
            {
                new
                {
                    id = 1,
                    server_id = 1,
                    name = Config.GameServer.GameServerName,
                    title = Config.GameServer.GameServerName,
                    host = Config.GameServer.PublicAddress,
                    ip = Config.GameServer.PublicAddress,
                    port = Config.GameServer.Port,
                    status = 1,
                    state = 1,
                    is_open = true,
                    open = true,
                    recommend = true
                }
            },
            game_server = new
            {
                host = Config.GameServer.PublicAddress,
                ip = Config.GameServer.PublicAddress,
                port = Config.GameServer.Port
            },
            http_server = new
            {
                host = Config.HttpServer.PublicAddress,
                port = Config.HttpServer.Port
            }
        };
    }

    private static string? ExtractUid(string? authInfo)
    {
        if (string.IsNullOrWhiteSpace(authInfo))
            return null;

        try
        {
            var normalized = Uri.UnescapeDataString(authInfo).Trim();
            var padding = normalized.Length % 4;
            if (padding > 0)
                normalized = normalized.PadRight(normalized.Length + (4 - padding), '=');

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("uid", out var uid) ? uid.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    [HttpGet("/getGameConfig")]
    [HttpPost("/getGameConfig")]
    public IActionResult GetGameConfig()
    {
        object rsp = new
        {
            code = "0",
            data = new
            {
                agreementUpdateTime = "1728552600000",
                appDownLoadUrl = "",
                enableReportDataToDouyin = false,
                loginType = new[] { "channel" },
                openActivationCode = false,
                qqGroup = (string?)null
            },
            msg = "success"
        };

        return Ok(rsp);
    }

    [HttpGet("/seasun/config")]
    [HttpPost("/seasun/config")]
    public IActionResult GetSeasunConfig()
    {
        object rsp = new
        {
            code = 0,
            data = new
            {
                platformPrivacyAgreement = "https://www.amazingseasun.com/privacy.html?lang=zh-Hant&gamecode=200001086",
                payType = new[] { "mycard" },
                loginType = new[] { "mail", "google", "twitter", "guest", "steam" },
                closeGeetest = false,
                userAgreement = "https://www.amazingseasun.com/user.html?lang=zh-Hant&gamecode=111111680",
                privacyAgreement = "https://www.amazingseasun.com/privacy.html?lang=zh-Hant&gamecode=111111680",
                initPrivacyUpdateTime = 0,
                platformUserAgreement = "https://www.amazingseasun.com/user.html?lang=zh-Hant&gamecode=200001086",
                accountPublicKey = "",
                payChannel = (string[]?)null,
                registerPrivacyUrl = "https://xgsdk.xoyo.games:13443/seasun/privacy-agreement/200001086/register/privacy.html?language=zh-Hant",
                loginPrivacyUrl = "https://xgsdk.xoyo.games:13443/seasun/privacy-agreement/111111680/login/privacy.html?language=zh-Hant"
            },
            msg = "操作成功"
        };

        return Ok(rsp);
    }

    private static AccountData? ResolveAccountByUid(string? uid)
    {
        if (int.TryParse(uid, out var parsedUid))
            return AccountData.GetAccountByUid(parsedUid);

        return null;
    }

    private static AccountData? ResolveAccountForSdkLogin(string? email, string? uid, string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            var accountByComboToken = AccountData.GetAccountByComboToken(token);
            if (accountByComboToken != null)
                return accountByComboToken;

            var accountByDispatchToken = AccountData.GetAccountByDispatchToken(token);
            if (accountByDispatchToken != null)
                return accountByDispatchToken;
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var accountByEmail = AccountData.GetAccountByEmail(email);
            if (accountByEmail != null)
                return accountByEmail;
        }

        return ResolveAccountByUid(uid);
    }

    private async Task<string?> GetJsonBodyValue(string propertyName)
    {
        if (!Request.HasJsonContentType())
            return null;

        Request.EnableBuffering();
        Request.Body.Position = 0;

        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            return document.RootElement.TryGetProperty(propertyName, out var value)
                ? value.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static AccountData? ResolveAutoLoginAccount()
        => AccountData.GetFirstAccount();

    private IActionResult BuildLoginFailedResponse(string message)
    {
        object rsp = new
        {
            code = 1001,
            data = (object?)null,
            msg = message
        };

        return Ok(rsp);
    }

    private IActionResult BuildNotFoundResponse(string message)
    {
        object rsp = new
        {
            code = 1001,
            data = (object?)null,
            msg = message
        };

        return Ok(rsp);
    }

    [HttpGet("/seasun/loginByToken")]
    [HttpPost("/seasun/loginByToken")]
    public async Task<IActionResult> LoginByToken(
        [FromQuery] string? uid,
        [FromQuery] string? token,
        [FromForm] string? form_uid,
        [FromForm] string? form_token)
    {
        var bodyUid = await GetJsonBodyValue("uid");
        var bodyToken = await GetJsonBodyValue("token");
        var account = ResolveAccountForSdkLogin(null, uid ?? form_uid ?? bodyUid, token ?? form_token ?? bodyToken)
                      ?? ResolveAutoLoginAccount();
        if (account == null)
            return BuildLoginFailedResponse("Account not found.");

        var responseUid = account.Uid.ToString();
        var responseToken = account.GenerateComboToken();

        object rsp = new
        {
            code = 0,
            data = new
            {
                associatedAccounts = Array.Empty<string>(),
                isFirstLogin = false,
                isNeedKoreaSciAuth = false,
                ksOpenId = $"ks_{responseUid}",
                nickname = account.Username,
                passportId = responseUid,
                playerFillAgeUrl = "",
                status = 0,
                thirdPartyUid = "",
                token = responseToken,
                type = "guest",
                uid = account.Uid
            },
            msg = "操作成功"
        };

        return Ok(rsp);
    }

    [HttpGet("/seasun/login")]
    [HttpPost("/seasun/login")]
    public async Task<IActionResult> Login(
        [FromQuery] string? uid,
        [FromQuery] string? token,
        [FromQuery] string? email,
        [FromForm] string? form_uid,
        [FromForm] string? form_token,
        [FromForm] string? form_email)
    {
        var bodyEmail = await GetJsonBodyValue("email");
        var finalEmail = email ?? form_email ?? bodyEmail;
        if (!string.IsNullOrWhiteSpace(finalEmail))
        {
            var normalizedEmail = finalEmail.Trim();
            var accountData = AccountData.GetAccountByEmail(normalizedEmail);
            if (accountData == null)
            {
                if (!ConfigManager.Config.ServerOption.AutoCreateUser) return BuildLoginFailedResponse("Account not found.");
                AccountData.CreateAccount(normalizedEmail, 0, "123456");
                accountData = AccountData.GetAccountByEmail(normalizedEmail)!;
            }

            var finalUidValue = accountData.Uid.ToString();
            var finalTokenValue = accountData.GenerateComboToken();

            object emailLoginRsp = new
            {
                code = 0,
                data = new
                {
                    associatedAccounts = Array.Empty<string>(),
                    isFirstLogin = false,
                    isNeedKoreaSciAuth = false,
                    ksOpenId = $"ks_{finalUidValue}",
                    nickname = accountData.Username,
                    passportId = finalUidValue,
                    playerFillAgeUrl = "",
                    status = 0,
                    thirdPartyUid = "",
                    token = finalTokenValue,
                    type = "guest",
                    uid = accountData.Uid
                },
                msg = "操作成功"
            };

            return Ok(emailLoginRsp);
        }

        var bodyUid = await GetJsonBodyValue("uid");
        var bodyToken = await GetJsonBodyValue("token");
        var account = ResolveAccountForSdkLogin(finalEmail, uid ?? form_uid ?? bodyUid, token ?? form_token ?? bodyToken)
                      ?? ResolveAutoLoginAccount();
        if (account == null)
            return BuildLoginFailedResponse("Account not found.");

        var responseUid = account.Uid.ToString();
        var responseToken = account.GenerateComboToken();

        object rsp = new
        {
            code = 0,
            data = new
            {
                associatedAccounts = Array.Empty<string>(),
                isFirstLogin = false,
                isNeedKoreaSciAuth = false,
                ksOpenId = $"ks_{responseUid}",
                nickname = account.Username,
                passportId = responseUid,
                playerFillAgeUrl = "",
                status = 0,
                thirdPartyUid = "",
                token = responseToken,
                type = "guest",
                uid = account.Uid
            },
            msg = "操作成功"
        };

        return Ok(rsp);
    }

    [HttpGet("/seasun/getAccountInfoForGame")]
    [HttpPost("/seasun/getAccountInfoForGame")]
    public IActionResult GetAccountInfoForGame(
        [FromQuery] string? uid,
        [FromForm] string? form_uid
    )
    {
        var account = ResolveAccountByUid(uid ?? form_uid);
        if (account == null)
            return BuildNotFoundResponse("Account not found.");

        var uidString = account.Uid.ToString();

        object rsp = new
        {
            code = 0,
            data = new
            {
                bindAccountTypes = new[] { "google" },
                channelUid = uidString,
                loginAccountType = "google",
                nickName = account.Username,
                passportId = uidString,
                uid = $"seasun__{uidString}"
            },
            msg = "操作成功"
        };

        return Ok(rsp);
    }

    [HttpPost("/bisdk/batchpush")]
    public IActionResult GetBatchPush()
    {
        object rsp = new
        {
            code = 0,
            ret = 0,
            msg = "ok",
            message = "ok"
        };

        return Ok(rsp);
    }

    [HttpGet("/query")]
    public IActionResult GetQuery([FromQuery] string? version, [FromQuery] string? platform)
    {
        var servers = new[]
        {
            new
            {
                id = 1,
                server_id = 1,
                name = Config.GameServer.GameServerName,
                title = Config.GameServer.GameServerName,
                host = Config.GameServer.PublicAddress,
                ip = Config.GameServer.PublicAddress,
                port = Config.GameServer.Port,
                status = 1,
                state = 1,
                is_open = true,
                open = true,
                recommend = true
            }
        };
        return Ok(servers);
    }

    [HttpGet("/query_version={version}")]
    public IActionResult GetQueryVersionV1(string version)
    {
        return Ok(BuildServerList(version));
    }

    [HttpGet("/query_version")]
    public IActionResult GetQueryVersionV2([FromQuery] string version)
    {
        return Ok(BuildServerList(version));
    }

    [HttpGet("/api/serverlist")]
    public IActionResult GetServerList()
    {
        return Ok(BuildServerList());
    }

    [HttpGet("/account/query-uid/{appId}")]
    public IActionResult QueryUid(string appId, [FromQuery] string authInfo)
    {
        var account = ResolveAccountByUid(ExtractUid(authInfo));
        if (account == null)
            return BuildNotFoundResponse("Account not found.");

        var uid = account.Uid.ToString();

        object rsp = new
        {
            code = "0",
            msg = "success",
            data = new
            {
                uid = $"seasun__{uid}"
            }
        };

        return Ok(rsp);
    }

    [HttpGet("/health")]
    public IActionResult HealthCheck()
    {
        object rsp = new
        {
            status = "ok",
            service = Config.GameServer.GameServerName
        };

        return Ok(rsp);
    }

    [HttpPost("/api/auth/guest")]
    public IActionResult AuthGuest([FromQuery] string? Token)
    {
        object rsp = new
        {
            Provider = "Guest",
            Token = Token,
            Account = "Account",
            Pid = "123813131321312"
        };

        return Ok(rsp);
    }
}
