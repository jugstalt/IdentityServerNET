using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.Security
{
    public class LoginBotDetection : ILoginBotDetection
    {
        private IDistributedCache _cache;
        
        public LoginBotDetection(IDistributedCache cache)
        {
            _cache = cache;
        }

        #region ILoginBotDetection

        async public Task<bool> IsSuspiciousUserAsync(string username)
        {
            if (String.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username required");

            var suspiciousUser = SuspiciousUser.FromString(await _cache.GetStringAsync(username));

            if (suspiciousUser != null)
            {
                var lastSet = suspiciousUser.TimeStamp;
                if ((DateTime.UtcNow - lastSet).TotalHours >= 1.0)
                {
                    await RemoveSuspiciousUserAsync(username);
                    return false;
                }

                return suspiciousUser.CountFailes >= 3;
            }

            return false;
        }

        async public Task AddSuspiciousUserAsync(string username)
        {
            var suspiciousUser = SuspiciousUser.FromStringOrDefault(await _cache.GetStringAsync(username), username);
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

        private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        async public Task<string> AddSuspicousUserAndGenerateCaptchaCodeAsync(string username)
        {
            var suspiciousUser = SuspiciousUser.FromStringOrDefault(await _cache.GetStringAsync(username), username);
            suspiciousUser.CountFailes++;

            Random rand = new Random();
            int maxRand = Letters.Length - 1;

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < 4; i++)
            {
                int index = rand.Next(maxRand);
                sb.Append(Letters[index]);
            }

            string code = sb.ToString();

            suspiciousUser.CaptchaCode = code;
            await _cache.SetStringAsync(username, suspiciousUser.ToString());

            return code;
        }

        async public Task<bool> VerifyCaptchaCodeAsync(string username, string code)
        {
            var suspiciousUser = SuspiciousUser.FromStringOrDefault(await _cache.GetStringAsync(username), username);

            return code != null && code.Equals(suspiciousUser.CaptchaCode, StringComparison.InvariantCultureIgnoreCase);
        }

        private static ConcurrentDictionary<string, DateTime> _suspiciousUserBlocks = new ConcurrentDictionary<string, DateTime>();

        async public Task BlockSuspicousUser(string username, int milliseconds = 10000)
        {
            if (_suspiciousUserBlocks.ContainsKey(username) && (DateTime.UtcNow - _suspiciousUserBlocks[username]).TotalMilliseconds < milliseconds)
                throw new Exception("Suspicous bot request detected");

            _suspiciousUserBlocks.TryAdd(username, DateTime.UtcNow);
            await Task.Delay(milliseconds);
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
