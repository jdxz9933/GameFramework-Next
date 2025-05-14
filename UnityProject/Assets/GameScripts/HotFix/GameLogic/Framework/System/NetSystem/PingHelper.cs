// 文件: PingHelper.cs

using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityGameFramework.Runtime;

public class PingHelper {
    public string Host {
        get => host;
    }

    private string host;

    private CancellationTokenSource cancellationTokenSource;

    private const int MaxTimeout = 5000;

    public PingHelper(string host) {
        this.host = host;
    }

    /// <summary>
    /// 异步 ping 指定主机并返回往返时延（毫秒）。
    /// </summary>
    public async UniTask<long> PingHostAsync(string host, CancellationToken cancellationToken) {
        using var ping = new Ping();
        var reply = await ping.SendPingAsync(host, MaxTimeout);
        if (cancellationToken.IsCancellationRequested) {
            throw new OperationCanceledException(cancellationToken);
        }
        if (reply.Status == IPStatus.Success)
            return reply.RoundtripTime;
        throw new InvalidOperationException($"Ping 失败：{reply.Status}");
    }

    public async UniTask<bool> Run(CancellationToken cancellationToken) {
        // string host = "192.168.50.170";
        // string host = "yoyatime-server.yoyaworld.com";
        bool isResult;
        try {
            long rtt = await PingHostAsync(host, cancellationToken);
            Log.Warning($"Ping {host} 成功，延迟：{rtt} ms");
            isResult = true;
        } catch (Exception ex) {
            Log.Error($"Ping {host} 失败：{ex.Message}");
            isResult = false;
        }
        return isResult;
    }

    public async UniTask<PingHelper> RunRepeat(int count) {
        cancellationTokenSource = new CancellationTokenSource();
        int successCount = 0;
        while (successCount < count) {
            bool isResult = await Run(cancellationTokenSource.Token);
            if (cancellationTokenSource.IsCancellationRequested) {
                break;
            }
            if (isResult) {
                successCount++;
            }
        }
        return this;
    }

    public void Cancel() {
        cancellationTokenSource?.Cancel();
    }
}