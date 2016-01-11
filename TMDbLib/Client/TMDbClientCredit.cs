﻿using System.Threading.Tasks;
using TMDbLib.Objects.Credit;
using TMDbLib.Rest;

namespace TMDbLib.Client
{
    public partial class TMDbClient
    {
        public async Task<Credit> GetCredits(string id)
        {
            return await GetCredits(id, DefaultLanguage);
        }

        public async Task<Credit> GetCredits(string id, string language)
        {
            TmdbRestRequest req = _client2.Create("credit/{id}");

            if (!string.IsNullOrEmpty(language))
                req.AddParameter("language", language);

            req.AddUrlSegment("id", id);

            TmdbRestResponse<Credit> resp = await req.ExecuteGetTaskAsync<Credit>().ConfigureAwait(false);

            return resp;
        }
    }
}
