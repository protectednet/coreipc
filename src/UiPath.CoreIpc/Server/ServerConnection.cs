﻿using System.Net.Security;
namespace UiPath.Ipc;

public interface IClient
{
    TCallbackInterface GetCallback<TCallbackInterface>() where TCallbackInterface : class;
    void Impersonate(Action action);
}
abstract class ServerConnection : IClient, IDisposable
{
    private readonly ConcurrentDictionary<Type, object> _callbacks = new();
    protected readonly Listener _listener;
    private Connection _connection;
    private Task<Connection> _connectionAsTask;
    private Server _server;
    protected ServerConnection(Listener listener) => _listener = listener;
    public ILogger Logger => _listener.Logger;
    public ListenerSettings Settings => _listener.Settings;
    public abstract Task<Stream> AcceptClient(CancellationToken cancellationToken);
    public virtual void Impersonate(Action action) => action();
    TCallbackInterface IClient.GetCallback<TCallbackInterface>() where TCallbackInterface : class
    {
        return (TCallbackInterface)_callbacks.GetOrAdd(typeof(TCallbackInterface), CreateCallback);

        TCallbackInterface CreateCallback(Type callbackContract)
        {
            if (_listener.LogEnabled)
            {
                _listener.Log($"Create callback {callbackContract} {_listener.Name}");
            }
            _connectionAsTask ??= Task.FromResult(_connection);
            var serviceClient = new ServiceClient<TCallbackInterface>(_connection.Serializer, Settings.RequestTimeout, Logger, (_, _) => _connectionAsTask);
            return serviceClient.CreateProxy();
        }
    }
    public async Task Listen(IServiceProvider serviceProvider, Stream network, CancellationToken cancellationToken)
    {
        var stream = await AuthenticateAsServer();
        var serializer = serviceProvider.GetRequiredService<ISerializer>();
        _connection = new(stream, serializer, Logger, _listener.Name, _listener.MaxMessageSize);
        _server = new Server(
            new Router(serviceProvider, Settings.RouterConfig), 
            Settings, _connection, client: this);
        // close the connection when the service host closes
        using (cancellationToken.UnsafeRegister(state => ((Connection)state).Dispose(), _connection))
        {
            await _connection.Listen();
        }
        return;
        async Task<Stream> AuthenticateAsServer()
        {
            var certificate = Settings.Certificate;
            if (certificate == null)
            {
                return network;
            }
            var sslStream = new SslStream(network);
            try
            {
                await sslStream.AuthenticateAsServerAsync(certificate);
            }
            catch
            {
                sslStream.Dispose();
                throw;
            }
            Debug.Assert(sslStream.IsEncrypted && sslStream.IsSigned);
            return sslStream;
        }
    }
    protected virtual void Dispose(bool disposing){}
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}