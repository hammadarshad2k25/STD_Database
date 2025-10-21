using Serilog;
using System.Net;
using System.Text.Json;

namespace STD_Database.Middleware
{
    public class LogMiddleware
    {
        private readonly RequestDelegate _next;
        public LogMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unhandled Exception Occur!");
                await ExceptionHandler(ex, context);
            }
        }
        private Task ExceptionHandler(Exception ex,HttpContext context)
        {
            HttpStatusCode status;
            string message;
            switch(ex)
            {
                case KeyNotFoundException:
                    status = HttpStatusCode.NotFound;
                    message = "The Requested Resource Was Not Found!";
                    break;
                case UnauthorizedAccessException: 
                    status = HttpStatusCode.Unauthorized;
                    message = "Unaouthorized Access!";
                    break;
                case ArgumentException:
                    status = HttpStatusCode.BadRequest; 
                    message = ex.Message;
                    break;
                default:
                    status = HttpStatusCode.InternalServerError;
                    message = "An Unexected Error Occurred! Plesase Try Again Later";
                    break;
            }
            context.Response.ContentType = "application/json"; 
            context.Response.StatusCode = (int)status;
            var Response = new
            {
                Statuscode = context.Response.StatusCode,
                Message = message
            };
            return context.Response.WriteAsync(JsonSerializer.Serialize(Response)); 
        }
    }
}
