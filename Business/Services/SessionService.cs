using System.Security.Claims;

namespace Business.Services
{
    public class SessionService
    {
        private Dictionary<string, SessionToken> Tokens { get; set; } = new Dictionary<string, SessionToken>();

        public void AddSessionToken(string token, ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            var _st = new SessionToken
            {
                Principal = principal,
                Expiration = DateTime.UtcNow.AddDays(7)
            };

            if (!Tokens.ContainsKey(token))
            {
                Tokens.Add(token, _st);
            }
        }
        public SessionToken GetSessionToken(string token)
        {
            var _now = DateTime.UtcNow;
            var toDelete = Tokens
                .Where(x => x.Value.Expiration < _now)
                .Select(x => x.Key)
                .ToList();
            foreach (var key in toDelete)
            {
                Tokens.Remove(key);
            }
            if (Tokens.ContainsKey(token))
            {
                var _tk = Tokens[token];
                if (_tk.Expiration > _now)
                {
                    return _tk;
                }
                Tokens.Remove(token);
                return null;

            }
            return null;

        }

        public void RevokeUserSessions(Guid userId)
        {
            var keysToRemove = Tokens
                .Where(kvp => kvp.Value.Principal.FindFirst("id")?.Value == userId.ToString())
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                Tokens.Remove(key);
            }
        }
    }
    public class SessionToken
    {
        //public string Token { get; set; }
        public ClaimsPrincipal Principal { get; set; }
        public DateTime Expiration { get; set; }
    }
}
