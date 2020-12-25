﻿////  __  _  __    __   ___ __  ___ ___
//// |  \| |/__\ /' _/ / _//__\| _ \ __|
//// | | ' | \/ |`._`.| \_| \/ | v / _|
//// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//// 
//// Copyright (C) 2019 - NosCore
//// 
//// NosCore is a free software: you can redistribute it and/or modify
//// it under the terms of the GNU General Public License as published by
//// the Free Software Foundation, either version 3 of the License, or any later version.
//// 
//// This program is distributed in the hope that it will be useful,
//// but WITHOUT ANY WARRANTY; without even the implied warranty of
//// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//// GNU General Public License for more details.
//// 
//// You should have received a copy of the GNU General Public License
//// along with this program.  If not, see <http://www.gnu.org/licenses/>.

//using System;
//using System.Globalization;
//using System.IdentityModel.Tokens.Jwt;
//using System.Linq;
//using System.Security.Claims;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Options;
//using Microsoft.IdentityModel.Tokens;
//using NosCore.Core.I18N;
//using NosCore.Core.Networking;
//using NosCore.Dao.Interfaces;
//using NosCore.Data.Dto;
//using NosCore.Data.Enumerations.I18N;
//using NosCore.Data.WebApi;
//using NosCore.Shared.Authentication;
//using NosCore.Shared.Configuration;
//using NosCore.Shared.Enumerations;
//using NosCore.WebApi.Models;
//using Serilog;
//using TwoFactorAuthNet;

//namespace NosCore.WebApi.Controller
//{
//    [ApiController]
//    [Route("api/v1/auth/thin")]
//    [Route("api/[controller]")]
//    public class AuthController : Microsoft.AspNetCore.Mvc.Controller
//    {
//        private readonly IDao<AccountDto, long> _accountDao;
//        private readonly IOptions<WebApiConfiguration> _apiConfiguration;
//        private readonly ILogger _logger;
//        private readonly IHasher _encryption;

//        public AuthController(IOptions<WebApiConfiguration> apiConfiguration, IDao<AccountDto, long> accountDao, ILogger logger, IHasher encryption)
//        {
//            _apiConfiguration = apiConfiguration;
//            _accountDao = accountDao;
//            _logger = logger;
//            _encryption = encryption;
//        }

//        [AllowAnonymous]
//        [HttpPost("sessions")]
//        public async Task<IActionResult> ConnectUserAsync(ApiSession session)
//        {
//            if (!ModelState.IsValid)
//            {
//                return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_ERROR));
//            }

//            var account = await _accountDao.FirstOrDefaultAsync(s => s.Name == session.Identity).ConfigureAwait(false);
//            if (account == null)
//            {
//                return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_ERROR));
//            }
//            var tfa = new TwoFactorAuth();
//            if (!string.IsNullOrEmpty(account.MfaSecret) && !tfa.VerifyCode(account.MfaSecret, session.Mfa))
//            {
//                return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MFA_INCORRECT));
//            }

//            if (account.NewAuthPassword != _encryption.Hash(Encoding.Default
//                    .GetString(Convert.FromBase64String(account!.NewAuthPassword!)), account.NewAuthSalt!
//            ))
//            {
//                return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_INCORRECT));
//            }


//            account.Language = Enum.Parse<RegionType>(session.GfLang?.ToUpper(CultureInfo.CurrentCulture) ?? "");

//            account = await _accountDao.TryInsertOrUpdateAsync(account).ConfigureAwait(false);
//            var platformGameAccountId = Guid.NewGuid();
//            var claims = new ClaimsIdentity(new[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, session.Identity),
//                new Claim(ClaimTypes.Sid, platformGameAccountId.ToString()),
//                new Claim(ClaimTypes.Role, account.Authority.ToString())
//            });

//            var password = _encryption.Hash(_apiConfiguration.Value.Password ?? "", _apiConfiguration.Value.Salt);

//            var keyByteArray = Encoding.Default.GetBytes(password);
//            var signinKey = new SymmetricSecurityKey(keyByteArray);
//            var handler = new JwtSecurityTokenHandler();
//            var securityToken = handler.CreateToken(new SecurityTokenDescriptor
//            {
//                Subject = claims,
//                Issuer = "Issuer",
//                Audience = "Audience",
//                SigningCredentials = new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256Signature)
//            });
//            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_API_SUCCESS),
//                session.Identity, platformGameAccountId, session.Locale);
//            return Ok(new
//            {
//                token = handler.WriteToken(securityToken),
//                platformGameAccountId
//            });
//        }

//        [HttpPost("codes")]
//        public IActionResult GetAuthCode(ApiPlatformGameAccount platformGameAccount)
//        {
//            var identity = (ClaimsIdentity?)User.Identity;
//            if (identity?.Claims.Any(s =>
//                (s.Type == ClaimTypes.Sid) && (s.Value == platformGameAccount.PlatformGameAccountId)) != true)
//            {
//                return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_INCORRECT));
//            }

//            var authCode = Guid.NewGuid();
//            SessionFactory.Instance.AuthCodes[authCode.ToString()] =
//                identity.Claims.First(s => s.Type == ClaimTypes.NameIdentifier).Value;

//            return Ok(new { code = authCode });
//        }

//        [HttpGet("mfaEnabled")]
//        [AllowAnonymous]
//        public async Task<IActionResult> HasMfaEnabled(string? username)
//        {
//            var account = await _accountDao.FirstOrDefaultAsync(s => s.Name == username).ConfigureAwait(false);
//            if (account == null || account.MfaSecret == null)
//            {
//                return Ok(false);
//            }

//            return Ok(true);
//        }
//    }
//}