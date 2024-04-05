using Android.App;
using Android.Gms.Common;
using Android.Gms.Wearable;
using Android.Mtp;
using Android.OS;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using MobileCompanion.Shared;
using MobileCompanion.Shared.Enums;
using MobileCompanion.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static AndroidX.ConstraintLayout.Core.State.State;

namespace MobileCompanion.Phone.Platforms.Android;

[Service(Exported = true, Name = "com.companyname.mobilecompanion.PhoneWearableListenerService"),
    IntentFilter(
        [
            "com.google.android.gms.wearable.DATA_CHANGED",
            "com.google.android.gms.wearable.MESSAGE_RECEIVED",
            "com.google.android.gms.wearable.CAPABILITY_CHANGED",
            "com.google.android.gms.wearable.CHANNEL_EVENT",
            "com.google.android.gms.wearable.REQUEST_RECEIVED"
        ],
    DataHosts = ["*", "*"],
    DataSchemes = ["wear", "wear"],
    DataPathPrefixes = ["/file-transfer", "/message-data"])]
internal class WearableDevice : WearableListenerService, MessageClient.IOnMessageReceivedListener, DataClient.IOnDataChangedListener, CapabilityClient.IOnCapabilityChangedListener
{
    public delegate void WearableDeviceMessageReceivedDelegate(ICommunicationPacket commandObject);
    public event WearableDeviceMessageReceivedDelegate? MessageReceived;
    private readonly MessageClient _messageClient;
    private readonly DataClient _dataClient;
    private readonly CapabilityClient _capabilityClient;
    private readonly ILogger _logger;
    public const string MessagePath = "/message-data";
    public const string FileTransfer = "/file-transfer-phone";
    public const string CAPABILITY_WEAR = "mobilecompanion_watch_app";
    private INode? _primaryNode;

    public WearableDevice()
    {
        var logFactory = Ioc.Default.GetRequiredService<ILoggerFactory>();
        _logger = logFactory.CreateLogger(nameof(WearableDevice));
        _messageClient = WearableClass.GetMessageClient(Platform.AppContext);
        _dataClient = WearableClass.GetDataClient(Platform.AppContext);
        _capabilityClient = WearableClass.GetCapabilityClient(Platform.AppContext);
    }

    public async Task Connect()
    {
        await _messageClient.AddListenerAsync(this);
        await _dataClient.AddListenerAsync(this);
        await _capabilityClient.AddListenerAsync(this, CAPABILITY_WEAR);
        if (await VerifyCompanionApp() && _primaryNode != null)
        {
            _logger.LogInformation("Companion app found and paired to device {DeviceName} ({DeviceID})", _primaryNode.DisplayName, _primaryNode.Id);
        }
    }

    public async Task Disconnect()
    {
        await _messageClient.RemoveListenerAsync(this);
        await _dataClient.RemoveListenerAsync(this);
        await _capabilityClient.RemoveListenerAsync(this, CAPABILITY_WEAR);
    }

    public async Task<bool> VerifyCompanionApp()
    {

        var capabilityInfo = await _capabilityClient.GetCapabilityAsync(CAPABILITY_WEAR, CapabilityClient.FilterAll);
        if (capabilityInfo != null)
        {
            if (capabilityInfo.Nodes.Count == 0)
            {
                _logger.LogError("No devices found with companion app");
                return false;
            }
            _logger.LogInformation("Found {Count} devices with companion app", capabilityInfo.Nodes.Count);
            //_primaryDeviceId = capabilityInfo.Nodes.First().StepDataId;
            _primaryNode = capabilityInfo.Nodes.First();
            return true;
        }

        return false;
    }
    public bool GetIsReachable()
    {
        return _primaryNode != null;
    }

    private async Task GetDeviceId()
    {
        try
        {
            var existingDevice = string.Empty;
            if (string.IsNullOrEmpty(existingDevice))
            {
                var nodes = await WearableClass.GetNodeClient(Platform.AppContext).GetConnectedNodesAsync();

                foreach (var node in nodes)
                {
                    if (node is { IsNearby: true })
                    {
                        existingDevice = node.Id;
                        _primaryNode = node;
                        _logger.LogInformation("Set paired device {DeviceName} ({DeviceID})", node.DisplayName, existingDevice);
                    }
                    break;
                }
                Preferences.Default.Set("PrimaryDeviceId", existingDevice);
            }
            //_primaryDeviceId = existingDevice;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unable to set device ID");
        }
    }
    public async void SendData(string command, string? stringData = null, double? numberData = null, string[]? arrayData = null)
    {
        if (_primaryNode == null)
        {
            await VerifyCompanionApp();
        }
        var cmdObj = new CommandObject
        {
            Command = command,
            StringData = stringData,
            NumberData = numberData,
            TimeStamp = DateTime.Now,
            ArrayData = arrayData
        };
        var fileName = $"{DateTime.Now:yyyy-MM-dd}.{DateTime.Now:HHmmss}.{command}";
        //var tempPath = Path.Combine(FileSystem.Current.CacheDirectory, fileName);
        //await File.WriteAllBytesAsync(tempPath, cmdObj.GetPayload());
        //var dataRequest = PutDataRequest.Create(FileTransfer);
        var dataRequest = PutDataMapRequest.Create(FileTransfer);
        dataRequest.DataMap.PutString("SOURCE", "PHONE");
        dataRequest.DataMap.PutString("COMMAND", cmdObj.GetPayload());
        dataRequest.DataMap.PutString("STAMP", fileName);
        //Asset asset = Asset.CreateFromBytes(cmdObj.GetPayload());
        //dataRequest.PutAsset(fileName, asset);
        dataRequest.SetUrgent();

        await _dataClient.PutDataItemAsync(dataRequest.AsPutDataRequest());
    }

    public async void SendMessage(string command, string? stringData = null, double? numberData = null, string[]? arrayData = null)
    {
        if (_primaryNode == null)
        {
            await GetDeviceId();
        }
        var cmdObj = new CommandObject
        {
            Command = command,
            StringData = stringData,
            NumberData = numberData,
            TimeStamp = DateTime.Now,
            ArrayData = arrayData
        };
        try
        {
            _logger.LogInformation("Sending command {Command} to Node ID {NodeId}", command, _primaryNode!.Id);
            await _messageClient.SendMessageAsync(_primaryNode.Id, MessagePath, cmdObj.GetBytes());
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error sending message to watch");
        }
    }
    private void ProcessDataItem(DataMap item)
    {
        try
        {
            var source = item.GetString(Keys.Source);
            if (source == Constants.WatchSource)
            {
                var cmdBytes = item.GetString(Keys.Command);
                var cmdObj = JsonSerializer.Deserialize<CommandObject>(cmdBytes, Constants.JsonSerializerOptions) ?? new() { Command = "" };
                _logger.LogInformation("Received Data Event from Watch: [{Timestamp}] {Command}", cmdObj.TimeStamp, cmdObj.Command);
                if (cmdObj.Command == Commands.Watch.TransferFile)
                {
                    var fileName = item.GetString(Keys.FileName);
                    var rawDataPath = Path.Combine(FileSystem.CacheDirectory, fileName);
                    var asset = item.GetAsset(Keys.FileData);
                    if (asset != null)
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                var fd = await WearableClass.DataApi.GetFdForAssetAsync(_dataClient.AsGoogleApiClient(), asset);
                                var assetStream = new MemoryStream();
                                await fd.InputStream.CopyToAsync(assetStream);
                            var rawBytes = assetStream.ToArray();
                                File.WriteAllBytes(rawDataPath, rawBytes);
                                _logger.LogInformation("Wrote data to {Path} ({Size})", rawDataPath, rawBytes.LongLength);

                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Unable to parse asset");
                            }
                        });
                    }
                    var packet = new CommunicationPacket
                    {
                        Command = CommandType.FileTransfer,
                        CommandData = cmdObj,
                        ErrorMessage = string.Empty
                    };
                    MessageReceived?.Invoke(packet);
                }
                else
                {

                    var packet = new CommunicationPacket
                    {
                        Command = CommandType.Message,
                        CommandData = cmdObj,
                        ErrorMessage = string.Empty
                    };
                    MessageReceived?.Invoke(packet);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data Item not a valid command object");
        }
    }

    public override void OnDataChanged(DataEventBuffer dataEvents)
    {
        try
        {
            foreach (var dataEvent in dataEvents)
            {
                _logger.LogDebug(dataEvent.DataItem.Uri.Path);
                var item = DataMapItem.FromDataItem(dataEvent.DataItem);
                ProcessDataItem(item.DataMap);
            }
        }
        catch (ObjectDisposedException) { } // Java likes to throw this even when properly disposing of the object
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling changed data events");
        }
    }

    public override void OnMessageReceived(IMessageEvent messageEvent)
    {
        try
        {
            switch (messageEvent.Path)
            {
                case MessagePath:
                    var messageBytes = messageEvent.GetData();
                    var messageString = Encoding.UTF8.GetString(messageBytes);
                    var messageData = JsonSerializer.Deserialize<CommandObject>(messageString, Constants.JsonSerializerOptions) ?? new() { Command = "" };
                    var packet = new CommunicationPacket
                    {
                        Command = CommandType.Message,
                        CommandData = messageData,
                        ErrorMessage = string.Empty
                    };
                    MessageReceived?.Invoke(packet);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling message from Watch");
        }
    }

    public override void OnCapabilityChanged(ICapabilityInfo capabilityInfo)
    {
        _logger?.LogInformation("OnCapabilityChanged");
    }

    public async void OnConnected(Bundle connectionHint)
    {
        await _messageClient.AddListenerAsync(this);
        await _dataClient.AddListenerAsync(this);
        await _capabilityClient.AddListenerAsync(this, CAPABILITY_WEAR);
    }

    public void OnConnectionFailed(ConnectionResult connectionResult)
    {

    }

    public void OnConnectionSuspended(int cause) { }
}
