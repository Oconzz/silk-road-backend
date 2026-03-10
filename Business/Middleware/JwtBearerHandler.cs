using Business.Data;
using Business.Model;
using Business.Services;
using Business.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using ISystemClock = Microsoft.AspNetCore.Authentication.ISystemClock;


namespace Business.Middleware
{
    public class CustomJwtBearerHandler : JwtBearerHandler
    {
        private readonly HttpClient _httpClient;
        private readonly IJwtUtils _jwtUtils;
        private DBContext _ctx;
        private SessionService _session;
        //private Connection _conn;
        public CustomJwtBearerHandler(HttpClient httpClient,
                                    IOptionsMonitor<JwtBearerOptions> options,
                                    ILoggerFactory logger,
                                    UrlEncoder encoder,
                                    ISystemClock clock,
                                    IJwtUtils jwtUtils,
                                    DBContext ctx,
                                    SessionService _tk)//,
                                                       //Connection Conn)
            : base(options, logger, encoder, clock)
        {
            _httpClient = httpClient;
            _jwtUtils = jwtUtils;
            _ctx = ctx;
            _session = _tk;
            //_conn = Conn;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Get the token from the Authorization header
            if (!Context.Request.Headers.TryGetValue("Authorization", out var authorizationHeaderValues))
            {
                return AuthenticateResult.NoResult();
            }

            var authorizationHeader = authorizationHeaderValues.FirstOrDefault();
            if (string.IsNullOrEmpty(authorizationHeader))
            {
                return AuthenticateResult.NoResult();
            }

            string token = "";
            if (authorizationHeader.StartsWith("Bearer "))
            {
                token = authorizationHeader.Substring("Bearer ".Length).Trim();

            }
            else
            {
                token = authorizationHeader.Trim();
            }

            // Call the API to validate the token
            var _s = _session.GetSessionToken(token);
            if (_s != null)
            {
                return AuthenticateResult.Success(new AuthenticationTicket(_s.Principal, "CustomJwtBearer"));
            }
            var response = _jwtUtils.ValidateJwtToken(token);

            // Return an authentication failure if the response is not successful
            if (response == null)
            {
                return AuthenticateResult.Fail("Token validation failed.");
            }



            // Set the authentication result with the claims from the API response
            var _claims = GetIdentity(token);

            // Check if user is active; if not, return unauthorized
            var idClaim = _claims.FirstOrDefault(x => x.Type.Equals("id"));
            if (idClaim == null)
            {
                return AuthenticateResult.Fail("Invalid token: missing user id.");
            }
            if (!Guid.TryParse(idClaim.Value, out var userId))
            {
                return AuthenticateResult.Fail("Invalid token: bad user id.");
            }
            var userEntity = await _ctx.User.FirstOrDefaultAsync(u => u.id == userId);
            if (userEntity == null || !userEntity.active)
            {
                return AuthenticateResult.Fail("User is inactive.");
            }
            //var tenant = _claims.First(x=>x.Type.Equals("tenant"));//   .FirstOrDefault( "tenant")?.Value;
            //var tennantDB = _ctx.Tenant.First(a => a.id == new Guid(tenant.Value));
            //string dbName = tennantDB != null ? tennantDB.dbName : "";
            //string dbUser = tennantDB != null ? tennantDB.dbUser : "";
            //string dbPassword = tennantDB != null ? tennantDB.dbPassword : "";
            //string bucket = tennantDB != null ? tennantDB.s3BucketName : "";
            //string folder = tennantDB != null ? tennantDB.s3FolderName : "";
            //var dbManager = tennantDB.dbManager;
            //if (dbManager == null || dbManager < 1)
            //{
            //    //for setting plan we don't need the db manager, so to mitigate user waiting time we authorize the user
            //    var requestPath = Context.Request.Path.Value;
            //    if (requestPath.Contains("SetPlan"))
            //    {
            //        bool trial_ = !_ctx.Tenant.FirstOrDefault(x => x.id == tennantDB.id)?.SubscriptionActive ?? true;
            //        var bucketClaim_ = new Claim("bucket", bucket);
            //        var folderClaim_ = new Claim("folder", folder);
            //        var trialAccount_ = new Claim("trial", trial_.ToString());

            //        var c1_ = _claims.Append(bucketClaim_);
            //        var c2_ = c1_.Append(trialAccount_);
            //        var c3_ = c2_.Append(folderClaim_);


            //        var claimsIdentity_ = new ClaimsIdentity(c3_, "Token");
            //        var principal_ = new ClaimsPrincipal(claimsIdentity_);
            //        try
            //        {
            //            _session.AddSessionToken(token, principal_);
            //        }
            //        catch (Exception ex)
            //        {
            //            return AuthenticateResult.Fail("An error occurred during Session handling. Please contact Support.");
            //        }

            //    }
            //    return AuthenticateResult.Fail("Account has not been created yet. Please be patient.");
            //}
            //var dbServer = dbManager == 1 ? _conn.dbServer : _conn.dbServer + "-" + dbManager;
            //
            //Condition to know if the tenant is in trial mode
            //
            // bool trial = !_ctx.Tenant.FirstOrDefault(x => x.id == tennantDB.id)?.SubscriptionActive ?? true;


            //string connString = _conn.ConnectionString
            //    .Replace("{server}", dbServer)
            //    .Replace("{catalog}", dbName)
            //    .Replace("{dbUser}", dbUser)
            //    .Replace("{dbPass}", dbPassword);


            //string connString = "test1234567xxxxxxxxxxxxxxxxxxxxxxxxx";
            //var ConnId = new Claim("ConnId", connString);
            //var bucketClaim = new Claim("bucket", bucket);
            //var folderClaim = new Claim("folder", folder);
            //var trialAccount = new Claim("trial", trial.ToString());

            //var c1=_claims.Append(ConnId);
            //var c2= c1.Append(bucketClaim);
            //var c3= c2.Append(trialAccount);
            //var c4= c3.Append(folderClaim);


            var claimsIdentity = new ClaimsIdentity(_claims, "Token");
            var principal = new ClaimsPrincipal(claimsIdentity);
            try
            {
                //_session.AddSessionToken(token, principal);
            }
            catch (Exception ex)
            {
                return AuthenticateResult.Fail("An error occurred during Session handling. Please contact Support.");
            }


            return AuthenticateResult.Success(new AuthenticationTicket(principal, "CustomJwtBearer"));

        }


        private ClaimsPrincipal GetClaims(string Token)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadToken(Token) as JwtSecurityToken;

            var claimsIdentity = new ClaimsIdentity(token.Claims, "Token");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            return claimsPrincipal;
        }

        private IEnumerable<Claim> GetIdentity(string Token)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadToken(Token) as JwtSecurityToken;

            return token.Claims;
        }
    }
}
