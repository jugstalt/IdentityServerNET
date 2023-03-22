using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.Security
{
    public class LoginBotDetection : ILoginBotDetection
    {
        private readonly IDistributedCache _cache;
        private readonly LoginBotDetectionOptions _options;

        public LoginBotDetection(IDistributedCache cache, IOptionsMonitor<LoginBotDetectionOptions> options)
        {
            _cache = cache;
            _options = options.CurrentValue ?? new LoginBotDetectionOptions();
        }

        #region ILoginBotDetection

        async public Task<bool> IsSuspiciousUserAsync(string username)
        {
            if (String.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username required");
            }

            var suspiciousUser = SuspiciousUser.FromString(await _cache.GetStringAsync(username));

            if (suspiciousUser != null)
            {
                var lastSet = suspiciousUser.TimeStamp.ToUniversalTime();
                if ((DateTime.UtcNow - lastSet).TotalMinutes >= _options.RembemberSuspiciousUserTotalMinutes)
                {
                    await RemoveSuspiciousUserAsync(username);
                    return false;
                }

                return suspiciousUser.CountFailes >= _options.MaxFailCount;
            }

            return false;
        }

        async public Task AddSuspiciousUserAsync(string username)
        {
            var suspiciousUser = SuspiciousUser.FromStringOrDefault(await _cache.GetStringAsync(username), username);

            suspiciousUser.TimeStamp = DateTime.Now;
            suspiciousUser.CountFailes++;

            await _cache.SetStringAsync(username, suspiciousUser.ToString());
        }

        async public Task RemoveSuspiciousUserAsync(string username)
        {
            var suspiciousUser = SuspiciousUser.FromString(await _cache.GetStringAsync(username));

            if (suspiciousUser != null)
            {
                await _cache.RemoveAsync(username);
            }
        }

        async public Task<string> AddSuspicousUserAndGenerateCaptchaCodeAsync(string username)
        {
            var suspiciousUser = SuspiciousUser.FromStringOrDefault(await _cache.GetStringAsync(username), username);
            suspiciousUser.CountFailes++;

            Random rand = new Random();
            int maxRand = _options.CaptchaCodeLetters.Length - 1;

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < _options.CaptchaCodeLength; i++)
            {
                int index = rand.Next(maxRand);
                sb.Append(_options.CaptchaCodeLetters[index]);
            }

            string code = sb.ToString();

            suspiciousUser.CaptchaCode = code;
            suspiciousUser.TimeStamp = DateTime.Now;

            await _cache.SetStringAsync(username, suspiciousUser.ToString());

            return code;
        }

        async public Task<bool> VerifyCaptchaCodeAsync(string username, string code)
        {
            var suspiciousUser = SuspiciousUser.FromStringOrDefault(await _cache.GetStringAsync(username), username);

            return code != null && code.Equals(suspiciousUser.CaptchaCode, StringComparison.InvariantCultureIgnoreCase);
        }

        private static ConcurrentDictionary<string, DateTime> _suspiciousUserBlocks = new ConcurrentDictionary<string, DateTime>();

        async public Task BlockSuspicousUser(string username)
        {
            if (_suspiciousUserBlocks.ContainsKey(username) && (DateTime.UtcNow - _suspiciousUserBlocks[username]).TotalSeconds < _options.BlockSuspiciousUserSeconds)
            {
                throw new Exception("Suspicous bot request detected");
            }

            _suspiciousUserBlocks.TryAdd(username, DateTime.UtcNow);
            await Task.Delay(_options.BlockSuspiciousUserSeconds * 1000);
            _suspiciousUserBlocks.TryRemove(username, out DateTime timeStamp);
        }

        #endregion

        #region Classes

        private class SuspiciousUser
        {
            public SuspiciousUser()
            {
            }

            public SuspiciousUser(string username)
            {
                this.Username = username;
                this.TimeStamp = DateTime.Now;

            }

            [JsonProperty("un")]
            public string Username { get; set; }
            [JsonProperty("ts")]
            public DateTime TimeStamp { get; set; }
            [JsonProperty("cc")]
            public string CaptchaCode { get; set; }
            [JsonProperty("cf")]
            public int CountFailes { get; set; }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }

            static public SuspiciousUser FromString(string json)
            {
                if (String.IsNullOrEmpty(json))
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<SuspiciousUser>(json);
            }

            static public SuspiciousUser FromStringOrDefault(string json, string username)
            {
                if (String.IsNullOrEmpty(json))
                {
                    return new SuspiciousUser(username);
                }

                return JsonConvert.DeserializeObject<SuspiciousUser>(json);
            }
        }

        #endregion
    }
}
