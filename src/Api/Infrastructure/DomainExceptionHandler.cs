using Core.Domain; // Тут лежить твій DomainException
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Infrastructure;

public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        // Якщо помилка не наша доменна — ігноруємо, хай обробляють інші
        if (exception is not DomainException domainException)
        {
            return false;
        }

        // Якщо наша — формуємо красиву відповідь 409 Conflict
        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Конфлікт бізнес-логіки",
            Detail = domainException.Message
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        
        // Кажемо системі, що ми успішно обробили помилку
        return true;
    }
}