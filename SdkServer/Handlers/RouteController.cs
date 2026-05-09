using Microsoft.AspNetCore.Mvc;
using MikuSB.Configuration;
using MikuSB.SdkServer.Models;
using MikuSB.Util;
using System.Text;
using System.Text.Json;

namespace MikuSB.SdkServer.Handlers;

[ApiController]
public class RouteController : ControllerBase
{
    public static ConfigContainer Config = ConfigManager.Config;
        public static JsonSerializerOptions JsonOption = new()
		{
            PropertyNamingPolicy = null,  // no snake_case
        };

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

        // return Ok(rsp);
        var json = JsonSerializer.Serialize(rsp, JsonOption);
        return Content(json, "application/json");
    }

    [HttpGet("/seasun/config")]
    [HttpPost("/seasun/config")]
    public IActionResult GetSeasunConfig()
    {
        object rsp = new
        {
            code = 0,
            msg = "操作成功",
            data = new
            {
                platformPrivacyAgreement = "", // https url
                payType = new[]
                {
                    "mycard",
                },
                loginType = new[]
                {
                    "mail",
                    "google",
                    "twitter",
                    "guest",
                    "steam",
                },
                closeGeetest = false,
                userAgreement = "", // https url
                privacyAgreement = "", // https url
                initPrivacyUpdateTime = 0,
                platformUserAgreement = "", // https url
                // accountPublicKey = "", // pem string?
                payChannel = Array.Empty<string>(),
                registerPrivacyUrl = "", // https url
                loginPrivacyUrl = "", // https url
                // agreementUpdateTime = "1728552600000",
                // appDownLoadUrl = "",
                // enableReportDataToDouyin = false,
                // openActivationCode = false,
                // qqGroup = (string?)null,
                // privacyUpdateTime = "1728552600000",
                // realNameAuth = false
            },
        };

        // return Ok(rsp);
        var json = JsonSerializer.Serialize(rsp, JsonOption);
        return Content(json, "application/json");
    }


    public class LoginByTokenReq
    {
        public string? uid { get; set; }
        public string? token { get; set; }
    }

    [HttpGet("/seasun/loginByToken")]
    [HttpPost("/seasun/loginByToken")]
    public IActionResult LoginByToken(
        // [FromQuery] string? uid,
        // [FromQuery] string? token,
        [FromForm] string? form_uid,
        [FromForm] string? form_token,
        [FromBody] LoginByTokenReq? body
    )
    {
        string finalUid = body?.uid ?? form_uid ?? "10001";
        string finalToken = body?.token ?? form_token ?? Guid.NewGuid().ToString("N");

        object rsp = new
        {
            code = 0,
            data = new
            {
            //     associatedAccounts = new[]
            // {
            //     new { bindStatus = false, nickname = "", thirdPartyType = "mail" },
            //     new { bindStatus = true, nickname = Config.GameServer.GameServerName, thirdPartyType = "google" },
            //     new { bindStatus = false, nickname = "", thirdPartyType = "twitter" },
            //     new { bindStatus = false, nickname = "", thirdPartyType = "guest" },
            //     new { bindStatus = false, nickname = "", thirdPartyType = "steam" }
            // },
                associatedAccounts = Array.Empty<string>(),
                isFirstLogin = false,
                isNeedKoreaSciAuth = false,
                ksOpenId = $"ks_{finalUid}",
                nickname = Config.GameServer.GameServerName,
                passportId = finalUid.Length > 10 ? finalUid[^10..] : finalUid,
                playerFillAgeUrl = "",
                status = 0,
                thirdPartyUid = "",
                token = finalToken,
                type = "guest", // google
                uid = long.Parse(finalUid),
            },
            msg = "操作成功"
        };

        // return Ok(rsp);
        var json = JsonSerializer.Serialize(rsp, JsonOption);
        return Content(json, "application/json");
    }

    [HttpGet("/seasun/getAccountInfoForGame")]
    [HttpPost("/seasun/getAccountInfoForGame")]
    public IActionResult GetAccountInfoForGame(
        [FromQuery] string? uid,
        [FromForm] string? form_uid
    )
    {
        string uidString = uid ?? form_uid ?? "10001";
        var finalUid = int.TryParse(uidString, out int parsedUid) ? parsedUid : 10001;

        object rsp = new
        {
            code = 0,
            data = new
            {
                bindAccountTypes = new[] { "google" },
                channelUid = uidString,
                loginAccountType = "google",
                nickName = Config.GameServer.GameServerName,
                passportId = uidString.Length > 10 ? uidString[^10..] : uidString,
                uid = $"seasun__{uid}"
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
        var uid = ExtractUid(authInfo) ?? "10001";

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
