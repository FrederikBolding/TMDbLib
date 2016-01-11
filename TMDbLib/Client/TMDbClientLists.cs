﻿using System;
using System.Threading.Tasks;
using TMDbLib.Objects.Authentication;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Lists;
using TMDbLib.Rest;

namespace TMDbLib.Client
{
    public partial class TMDbClient
    {
        /// <summary>
        /// Retrieve a list by it's id
        /// </summary>
        /// <param name="listId">The id of the list you want to retrieve</param>
        public async Task<List> GetList(string listId)
        {
            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentNullException("listId");

            TmdbRestRequest req = _client2.Create("list/{listId}");
            req.AddUrlSegment("listId", listId);

            TmdbRestResponse<List> resp = await req.ExecuteGetTaskAsync<List>().ConfigureAwait(false);

            return resp;
        }

        /// <summary>
        /// Will check if the provided movie id is present in the specified list
        /// </summary>
        /// <param name="listId">Id of the list to check in</param>
        /// <param name="movieId">Id of the movie to check for in the list</param>
        public async Task<bool> GetListIsMoviePresent(string listId, int movieId)
        {
            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentNullException("listId");

            if (movieId <= 0)
                throw new ArgumentOutOfRangeException("movieId");

            TmdbRestRequest req = _client2.Create("list/{listId}/item_status");
            req.AddUrlSegment("listId", listId);
            req.AddParameter("movie_id", movieId.ToString());

            TmdbRestResponse<ListStatus> response = await req.ExecuteGetTaskAsync<ListStatus>().ConfigureAwait(false);

            return (await response.GetDataObject()).ItemPresent;
        }

        /// <summary>
        /// Creates a new list for the user associated with the current session
        /// </summary>
        /// <param name="name">The name of the new list</param>
        /// <param name="description">Optional description for the list</param>
        /// <param name="language">Optional language that might indicate the language of the content in the list</param>
        /// <remarks>Requires a valid user session</remarks>
        /// <exception cref="UserSessionRequiredException">Thrown when the current client object doens't have a user session assigned.</exception>
        public async Task<string> ListCreate(string name, string description = "", string language = null)
        {
            RequireSessionId(SessionType.UserSession);

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name");

            // Description is expected by the API and can not be null
            if (string.IsNullOrWhiteSpace(description))
                description = "";

            TmdbRestRequest req = _client2.Create("list");
            AddSessionId(req, SessionType.UserSession);

            language = language ?? DefaultLanguage;
            if (!String.IsNullOrWhiteSpace(language))
            {
                req.SetBody(new { name = name, description = description, language = language });

            }
            else
            {
                req.SetBody(new { name = name, description = description });
            }

            TmdbRestResponse<ListCreateReply> response = await req.ExecutePost<ListCreateReply>().ConfigureAwait(false);

            return (await response.GetDataObject()).ListId;
        }

        /// <summary>
        /// Deletes the specified list that is owned by the user
        /// </summary>
        /// <param name="listId">A list id that is owned by the user associated with the current session id</param>
        /// <remarks>Requires a valid user session</remarks>
        /// <exception cref="UserSessionRequiredException">Thrown when the current client object doens't have a user session assigned.</exception>
        public async Task<bool> ListDelete(string listId)
        {
            RequireSessionId(SessionType.UserSession);

            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentNullException("listId");

            TmdbRestRequest req = _client2.Create("list/{listId}");
            req.AddUrlSegment("listId", listId);
            AddSessionId(req, SessionType.UserSession);

            TmdbRestResponse<PostReply> response = await req.ExecuteDelete<PostReply>().ConfigureAwait(false);

            // Status code 13 = success
            PostReply item = await response.GetDataObject();

            // TODO: Previous code checked for item=null
            return item.StatusCode == 13;
        }

        /// <summary>
        /// Adds a movie to a specified list
        /// </summary>
        /// <param name="listId">The id of the list to add the movie to</param>
        /// <param name="movieId">The id of the movie to add</param>
        /// <returns>True if the method was able to add the movie to the list, will retrun false in case of an issue or when the movie was already added to the list</returns>
        /// <remarks>Requires a valid user session</remarks>
        /// <exception cref="UserSessionRequiredException">Thrown when the current client object doens't have a user session assigned.</exception>
        public async Task<bool> ListAddMovie(string listId, int movieId)
        {
            return await ManipulateMediaList(listId, movieId, "add_item");
        }

        /// <summary>
        /// Removes a movie from the specified list
        /// </summary>
        /// <param name="listId">The id of the list to add the movie to</param>
        /// <param name="movieId">The id of the movie to add</param>
        /// <returns>True if the method was able to remove the movie from the list, will retrun false in case of an issue or when the movie was not present in the list</returns>
        /// <remarks>Requires a valid user session</remarks>
        /// <exception cref="UserSessionRequiredException">Thrown when the current client object doens't have a user session assigned.</exception>
        public async Task<bool> ListRemoveMovie(string listId, int movieId)
        {
            return await ManipulateMediaList(listId, movieId, "remove_item");
        }

        /// <summary>
        /// Clears a list, without confirmation.
        /// </summary>
        /// <param name="listId">The id of the list to clear</param>
        /// <returns>True if the method was able to remove the movie from the list, will retrun false in case of an issue or when the movie was not present in the list</returns>
        /// <remarks>Requires a valid user session</remarks>
        /// <exception cref="UserSessionRequiredException">Thrown when the current client object doens't have a user session assigned.</exception>
        public async Task<bool> ListClear(string listId)
        {
            RequireSessionId(SessionType.UserSession);

            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentNullException("listId");

            TmdbRestRequest request = _client2.Create("list/{listId}/clear");
            request.AddUrlSegment("listId", listId);
            request.AddParameter("confirm", "true");
            AddSessionId(request, SessionType.UserSession);

            TmdbRestResponse<PostReply> response = await request.ExecutePost<PostReply>().ConfigureAwait(false);

            // Status code 12 = "The item/record was updated successfully"
            PostReply item = await response.GetDataObject();

            // TODO: Previous code checked for item=null
            return item.StatusCode == 12;
        }

        private async Task<bool> ManipulateMediaList(string listId, int movieId, string method)
        {
            RequireSessionId(SessionType.UserSession);

            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentNullException("listId");

            // Movie Id is expected by the API and can not be null
            if (movieId <= 0)
                throw new ArgumentOutOfRangeException("movieId");

            TmdbRestRequest req = _client2.Create("list/{listId}/{method}");
            req.AddUrlSegment("listId", listId);
            req.AddUrlSegment("method", method);
            AddSessionId(req, SessionType.UserSession);

            req.SetBody(new { media_id = movieId });

            TmdbRestResponse<PostReply> response = await req.ExecutePost<PostReply>().ConfigureAwait(false);

            // Status code 12 = "The item/record was updated successfully"
            // Status code 13 = "The item/record was deleted successfully"
            PostReply item = await response.GetDataObject();

            // TODO: Previous code checked for item=null
            return item.StatusCode == 12 || item.StatusCode == 13;
        }
    }
}