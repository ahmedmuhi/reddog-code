using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dapr.Client;

namespace RedDog.Shared;

/// <summary>
/// Helper class for Dapr service-to-service invocation that ensures compliance with Dapr 1.9+ requirements.
/// Specifically handles the explicit Content-Type: application/json header requirement for POST/PUT requests.
/// </summary>
public class DaprInvocationHelper
{
    private readonly DaprClient _daprClient;

    public DaprInvocationHelper(DaprClient daprClient)
    {
        _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
    }

    /// <summary>
    /// Invokes a GET method on a remote service via Dapr.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="appId">The application ID of the target service.</param>
    /// <param name="methodName">The method/endpoint to invoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the service.</returns>
    public async Task<TResponse> InvokeMethodAsync<TResponse>(
        string appId,
        string methodName,
        CancellationToken cancellationToken = default)
    {
        return await _daprClient.InvokeMethodAsync<TResponse>(
            HttpMethod.Get,
            appId,
            methodName,
            cancellationToken);
    }

    /// <summary>
    /// Invokes a POST method on a remote service via Dapr with explicit Content-Type header.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="appId">The application ID of the target service.</param>
    /// <param name="methodName">The method/endpoint to invoke.</param>
    /// <param name="request">The request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the service.</returns>
    public async Task<TResponse> InvokeMethodAsync<TRequest, TResponse>(
        string appId,
        string methodName,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var httpRequest = _daprClient.CreateInvokeMethodRequest(
            HttpMethod.Post,
            appId,
            methodName);

        // Serialize request to JSON and set Content-Type header for Dapr 1.9+ compliance
        var json = JsonSerializer.Serialize(request);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        return await _daprClient.InvokeMethodAsync<TResponse>(httpRequest, cancellationToken);
    }

    /// <summary>
    /// Invokes a POST method on a remote service via Dapr without expecting a response.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <param name="appId">The application ID of the target service.</param>
    /// <param name="methodName">The method/endpoint to invoke.</param>
    /// <param name="request">The request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InvokeMethodAsync<TRequest>(
        string appId,
        string methodName,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var httpRequest = _daprClient.CreateInvokeMethodRequest(
            HttpMethod.Post,
            appId,
            methodName);

        // Serialize request to JSON and set Content-Type header for Dapr 1.9+ compliance
        var json = JsonSerializer.Serialize(request);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        await _daprClient.InvokeMethodAsync(httpRequest, cancellationToken);
    }

    /// <summary>
    /// Invokes a DELETE method on a remote service via Dapr.
    /// </summary>
    /// <param name="appId">The application ID of the target service.</param>
    /// <param name="methodName">The method/endpoint to invoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InvokeMethodDeleteAsync(
        string appId,
        string methodName,
        CancellationToken cancellationToken = default)
    {
        await _daprClient.InvokeMethodAsync(
            HttpMethod.Delete,
            appId,
            methodName,
            cancellationToken);
    }

    /// <summary>
    /// Invokes a DELETE method on a remote service via Dapr with a request body.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <param name="appId">The application ID of the target service.</param>
    /// <param name="methodName">The method/endpoint to invoke.</param>
    /// <param name="request">The request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InvokeMethodDeleteAsync<TRequest>(
        string appId,
        string methodName,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var httpRequest = _daprClient.CreateInvokeMethodRequest(
            HttpMethod.Delete,
            appId,
            methodName);

        // Serialize request to JSON and set Content-Type header for Dapr 1.9+ compliance
        var json = JsonSerializer.Serialize(request);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        await _daprClient.InvokeMethodAsync(httpRequest, cancellationToken);
    }

    /// <summary>
    /// Invokes a PUT method on a remote service via Dapr with explicit Content-Type header.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <param name="appId">The application ID of the target service.</param>
    /// <param name="methodName">The method/endpoint to invoke.</param>
    /// <param name="request">The request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InvokeMethodPutAsync<TRequest>(
        string appId,
        string methodName,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var httpRequest = _daprClient.CreateInvokeMethodRequest(
            HttpMethod.Put,
            appId,
            methodName);

        // Serialize request to JSON and set Content-Type header for Dapr 1.9+ compliance
        var json = JsonSerializer.Serialize(request);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        await _daprClient.InvokeMethodAsync(httpRequest, cancellationToken);
    }

    /// <summary>
    /// Invokes a PUT method on a remote service via Dapr with explicit Content-Type header and expects a response.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="appId">The application ID of the target service.</param>
    /// <param name="methodName">The method/endpoint to invoke.</param>
    /// <param name="request">The request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the service.</returns>
    public async Task<TResponse> InvokeMethodPutAsync<TRequest, TResponse>(
        string appId,
        string methodName,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var httpRequest = _daprClient.CreateInvokeMethodRequest(
            HttpMethod.Put,
            appId,
            methodName);

        // Serialize request to JSON and set Content-Type header for Dapr 1.9+ compliance
        var json = JsonSerializer.Serialize(request);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        return await _daprClient.InvokeMethodAsync<TResponse>(httpRequest, cancellationToken);
    }
}
