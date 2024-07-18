using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ImpliciX.Harmony.Messages;
using ImpliciX.Language.Core;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;

namespace ImpliciX.Harmony.Infrastructure
{
    public interface IAzureIoTHubAdapter : IDisposable
    {
        public bool SendMessage(IHarmonyMessage message, IPublishingContext context);
    }

    public class AzureIotHubAdapter : IAzureIoTHubAdapter
    {
        private DeviceClient _iotHubDeviceClient;

        private AzureIotHubAdapter()
        {
        }

        public static Result<AzureIotHubAdapter> Create(string deviceId, IotHubSettings iotHubSettings)
        {
            var adapter = new AzureIotHubAdapter();
            var authenticationMethod =
                new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, iotHubSettings.SymmetricKey);
            adapter._iotHubDeviceClient =
                DeviceClient.Create(iotHubSettings.Uri, authenticationMethod, TransportType.Mqtt);
            adapter._iotHubDeviceClient.SetConnectionStatusChangesHandler(StatusChangesHandler);
            adapter._iotHubDeviceClient.SetRetryPolicy(new ExponentialBackoff(2, TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100)));
            try
            {
                adapter._iotHubDeviceClient.OpenAsync().Wait();
                return adapter;
            }
            catch (Exception)
            {
                return new Error(nameof(AzureIotHubAdapter),
                    $"Unable to connect device {deviceId} to IotHub {iotHubSettings.Uri}");
            }
        }

        public void Dispose()
        {
            _iotHubDeviceClient?.Dispose();
        }

        private static void StatusChangesHandler(ConnectionStatus connectionStatus,
            ConnectionStatusChangeReason reason)
        {
            Log.Debug("IoTHub connection status changed to {ConnectionStatus}, reason={Reason}", connectionStatus,
                reason);
        }

        public bool SendMessage(IHarmonyMessage message, IPublishingContext context)
        {
            string payload;
            Message mqttMessage;
            try
            {
                payload = message.Format(context);
                mqttMessage = new Message(Encoding.UTF8.GetBytes(payload));
                mqttMessage.Properties.Add("Type", message.GetMessageType());
                mqttMessage.Properties.Add("Compressed", "False");
            }
            catch (Exception e)
            {
                Log.Warning("Unable to format an Harmony '{MessageType}' message. Reason={ErrorMessage}", message.GetType(),
                    e.Message);
                return true;
            }
            try
            {
                _iotHubDeviceClient.SendEventAsync(mqttMessage).Wait();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static Result<string> RegisterWithDps(string deviceId, DpsSettings dpsSettings,
            IotHubSettings iotHubSettings)
        {
            Log.Information("Registering {DeviceId} on DPS {IdScope}", deviceId, dpsSettings.IdScope);
            try
            {
                using var security = new SecurityProviderSymmetricKey(deviceId, iotHubSettings.SymmetricKey, null);
                var provClient = ProvisioningDeviceClient.Create(dpsSettings.Uri, dpsSettings.IdScope, security,
                    new ProvisioningTransportHandlerMqtt());
                var registrationTask = provClient.RegisterAsync();
                if (!registrationTask.Wait(dpsSettings.RegistrationTimeout))
                {
                    var error = new Error(nameof(AzureIotHubAdapter),
                        $"Device {deviceId} DPS registration timeout");
                    return Result<string>.Create(error);
                }

                var result = registrationTask.Result;
                if (result.Status != ProvisioningRegistrationStatusType.Assigned)
                {
                    var error = new Error(nameof(AzureIotHubAdapter),
                        $"Device {deviceId} DPS registration failed with status='{result.Status}' - (error code={result.ErrorCode}) {result.ErrorMessage}");
                    return Result<string>.Create(error);
                }

                return result.AssignedHub;
            }
            catch (Exception exception)
            {
                var error = new Error(nameof(AzureIotHubAdapter),
                    $"Device {deviceId} DPS registration failed with exception={string.Join(", ", exception.AllMessages())}");
                return Result<string>.Create(error);
            }
        }

        static AzureIotHubAdapter()
        {
            InstallCaCert("ImpliciX.Harmony.Infrastructure.BaltimoreCyberTrustRoot.crt.pem");
            InstallCaCert("ImpliciX.Harmony.Infrastructure.DigiCertGlobalRootG2.crt.pem");
        }

        public static void InstallCaCert(string certFile)
        {
            using var certStream = new MemoryStream();
            Assembly.GetExecutingAssembly().GetManifestResourceStream(certFile)!.CopyTo(certStream);
            var certData = certStream.ToArray();
            var cert2 = new X509Certificate2(certData);
            using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert2);
            Log.Debug("Successfully added certificate from {CertName}", cert2.SubjectName.Format(false));
        }
    }
}