using System.Net;
using System.Net.Http.Json;
using Web.Contracts;

namespace Web.Services;

public sealed class ProductsApiClient(HttpClient http)
{
    public async Task<List<ProductModel>> ListAsync(CancellationToken ct = default)
    {
        var paged = await http.GetFromJsonAsync<PagedProductsResponse>("api/products", ct);
        return paged?.Items ?? [];
    }

    public async Task<ProductModel?> FindAsync(string id, CancellationToken ct = default)
    {
        using var response = await http.GetAsync($"api/products/{Uri.EscapeDataString(id)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductModel>(ct);
    }

    public Task<ApiResult> CreateAsync(ProductModel model, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, "api/products", model, ct);

    public Task<ApiResult> UpdateAsync(ProductModel model, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Put, $"api/products/{Uri.EscapeDataString(model.Id)}", model, ct);

    private async Task<ApiResult> SendAsync(
        HttpMethod method,
        string url,
        ProductModel model,
        CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url)
            {
                Content = JsonContent.Create(model)
            };
            using var response = await http.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
                return new ApiResult(true);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new ApiResult(false, "Запис не знайдено — можливо, його вже видалили.");

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var conflict = await TryReadProblemAsync(response, ct);
                return new ApiResult(
                    false,
                    conflict?.Detail ?? conflict?.Title ?? "Конфлікт даних (такий артикул уже існує).");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var problem = await TryReadProblemAsync(response, ct);
                return new ApiResult(
                    false,
                    problem?.Title ?? "Дані не пройшли перевірку.",
                    problem?.Errors);
            }

            return new ApiResult(false, $"Сервер повернув {(int)response.StatusCode}.");
        }
        catch (HttpRequestException)
        {
            return new ApiResult(false, "Api недоступний. Перевірте, чи запущено src/Api.");
        }
    }

    private static async Task<ValidationProblemResponse?> TryReadProblemAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ValidationProblemResponse>(ct);
        }
        catch
        {
            return null;
        }
    }
    public async Task<ApiResult> DeleteAsync(string id, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.DeleteAsync($"api/products/{Uri.EscapeDataString(id)}", ct);

            if (response.IsSuccessStatusCode)
                return new ApiResult(true);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new ApiResult(false, "Запис не знайдено — можливо, його вже видалили.");

            return new ApiResult(false, $"Сервер повернув {(int)response.StatusCode}.");
        }
        catch (HttpRequestException)
        {
            return new ApiResult(false, "Api недоступний. Перевірте, чи запущено src/Api.");
        }
    }
}