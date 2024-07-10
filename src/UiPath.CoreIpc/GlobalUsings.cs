﻿global using UiPath.Ipc.Extensibility;
global using BeforeCallHandler = System.Func<UiPath.Ipc.CallInfo, System.Threading.CancellationToken, System.Threading.Tasks.Task>;
global using ConnectionFactory = System.Func<UiPath.Ipc.Connection?, System.Threading.CancellationToken, System.Threading.Tasks.Task<UiPath.Ipc.Connection>>;
global using InvokeDelegate = System.Func<UiPath.Ipc.IServiceClient, System.Reflection.MethodInfo, object?[], object?>;
global using Accept = System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task<System.Net.WebSockets.WebSocket>>;
