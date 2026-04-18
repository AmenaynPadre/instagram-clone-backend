using InstagramClone.Common;

namespace InstagramClone.Helpers;

public static class SessionHelper
{
    public static SessionInfo GetSessionInfo(HttpContext context)
    {
        return new SessionInfo
        {
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            Device = context.Request.Headers["User-Agent"].ToString()
        };
    }
}