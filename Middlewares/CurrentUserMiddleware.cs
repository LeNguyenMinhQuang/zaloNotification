// public class CurrentUserMiddleware
// {
//     private readonly RequestDelegate _next;

//     public CurrentUserMiddleware(RequestDelegate next)
//     {
//         _next = next;
//     }

//     public async Task Invoke(HttpContext context, CheckAccessTokenService checkService)
//     {
//         var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

//         if (token != null)
//         {
//             var (user, status) = checkService.CheckToken(token);
//             if (status == "OK" && user != null)
//             {
//                 context.Items["CurrentUser"] = user;
//             }
//             else
//             {
//                 context.Response.StatusCode = StatusCodes.Status401Unauthorized;
//                 await context.Response.WriteAsync("Invalid Token");
//                 return;
//             }
//         }

//         await _next(context);
//     }
// }
