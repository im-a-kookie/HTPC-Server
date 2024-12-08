using System.Net;

namespace CookieCrumbs.TCP
{
    /// <summary>
    /// Provides boilerplate HTTP responses for some common errors/etc.
    /// </summary>
    internal partial class ResponseSender
    {

        /// <summary>
        /// Completes this action as a NotFound response (404)
        /// </summary>
        /// <param name="api"></param>
        public void NotFound(bool api = true)
        {
            if (_writtenHeader) return;
            _writtenHeader = true;
            Headers.Clear();

            // Differentiate based on api or html
            string file = api ? "NotFound.json" : "NotFound.html";
            string mime = api ? "application/json" : "text/html";

            // Configure the request
            Result = HttpStatusCode.NotFound;
            AddHeader("Content-Type", $"{mime}; charset=UTF-8");

            // And output
            WriteHeaders();
            SubmitResource(file);
            ActionCompleted = true;
        }

        /// <summary>
        /// Completes this action as a BadRequest response (400)
        /// </summary>
        /// <param name="api"></param>
        public void BadRequest(bool api = true)
        {
            if (_writtenHeader) return;
            _writtenHeader = true;
            Headers.Clear();

            // Differentiate based on api or html
            string file = api ? "BadRequest.json" : "BadRequest.html";
            string mime = api ? "application/json" : "text/html";

            // Configure the request
            Result = HttpStatusCode.BadRequest;
            AddHeader("Content-Type", $"{mime}; charset=UTF-8");

            // And output
            WriteHeaders();
            SubmitResource(file);
            ActionCompleted = true;
        }

        /// <summary>
        /// Completes this action as an Unauthorized access response (401)
        /// </summary>
        /// <param name="api"></param>
        public void NotAuthorized(bool api = true)
        {
            if (_writtenHeader) return;
            _writtenHeader = true;
            Headers.Clear();

            // Differentiate based on api or html
            string file = api ? "NotAuthorized.json" : "NotAuthorized.html";
            string mime = api ? "application/json" : "text/html";

            // Configure the request
            Result = HttpStatusCode.Unauthorized;
            AddHeader("Content-Type", $"{mime}; charset=UTF-8");

            // And output
            WriteHeaders();
            SubmitResource(file);
            ActionCompleted = true;
        }

        /// <summary>
        /// Completes this action as a redirection to a new target URL (302)
        /// </summary>
        /// <param name="target">The new URL to target</param>
        public void Redirect(string target)
        {
            // This is whatever
            if (_writtenHeader) return;
            _writtenHeader = true;

            Headers.Clear();

            // Configure the request
            Result = HttpStatusCode.Redirect;
            AddHeader("Location", target);
            AddHeader("Content-Type", "text/html; charset=UTF-8");

            // now write it all off and boink
            WriteHeaders();
            SubmitResource("Redirect.html");
            ActionCompleted = true;
        }
    }
}
