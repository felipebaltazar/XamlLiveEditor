using Microsoft.AspNetCore.SignalR;
using System;

namespace LiveEditor.Api.Extensions
{
    internal static class HubCallerContextExtensions
    {
        private const string ID_QUERY = "connectionId";
        private const string EDITOR_QUERY = "iseditor";

        internal static string ToConnectionId(this HubCallerContext context) =>
            TryGetFromQuery(context, ID_QUERY);

        internal static bool CheckIsEditor(this HubCallerContext context)
        {
            if (context is null)
                return false;

            var value = TryGetFromQuery(context, EDITOR_QUERY);

            if (bool.TryParse(value, out var result))
                return result;

            return false;
        }

        private static string TryGetFromQuery(HubCallerContext context, string key)
        {
            try
            {
                var httpCtx = context.GetHttpContext();
                var query = httpCtx.Request.Query;
                return query[key];
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex);
            }

            return string.Empty;
        }
    }
}
